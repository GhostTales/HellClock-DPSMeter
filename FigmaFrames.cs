using System;
using System.Collections.Generic;
using UnityEngine;

namespace DPSMeter
{
    internal enum FigmaLayout { None, Horizontal, Vertical, Grid }

    // Figma child sizing: Fixed = Bounds size, Hug = size of the content, Fill = fill the container
    // (stack: share the leftover main-axis space by FillWeight; grid: fill the cell).
    internal enum FigmaSize { Fixed, Hug, Fill }

    // Figma "Packing" (main axis) / "Alignment" (cross axis / cell alignment).
    internal enum FigmaAlign { Start, Center, End, SpaceBetween }

    internal enum FigmaTrackKind { Fixed, Hug, Fr }

    // Pointer state of a frame, resolved on every render.
    internal enum FigmaState { Idle, Hover, Pressed }

    // One grid row or column definition: Px(100), Hug, Fr(0.25).
    internal readonly struct FigmaTrack
    {
        internal readonly FigmaTrackKind Kind;
        internal readonly float Value;

        FigmaTrack(FigmaTrackKind kind, float value)
        {
            Kind = kind;
            Value = value;
        }

        internal static FigmaTrack Px(float value) => new FigmaTrack(FigmaTrackKind.Fixed, value);
        internal static FigmaTrack Fr(float value) => new FigmaTrack(FigmaTrackKind.Fr, value);
        internal static FigmaTrack Hug => new FigmaTrack(FigmaTrackKind.Hug, 0f);
    }

    internal static class FigmaExtensions
    {
        internal static T Size<T>(this T frame, FigmaSize width, FigmaSize height) where T : FigmaFrame
        {
            frame.WidthSize = width;
            frame.HeightSize = height;
            return frame;
        }
    }

    internal class FigmaFrame
    {
        internal Rect Bounds; // left, top, width, height
        internal Color? Fill;
        internal Color? Border;
        // Per-side colour overrides; a side left null uses Border.
        internal Color? BorderLeft, BorderTop, BorderRight, BorderBottom;
        internal Vector4 BorderWidths;       // left, top, right, bottom
        internal Vector4 CornerRadii;        // top-left, top-right, bottom-right, bottom-left

        // Uniform shorthands that set all four sides / corners.
        internal float BorderWidth { get => BorderWidths.x; set => BorderWidths = new Vector4(value, value, value, value); }
        internal float CornerRadius { get => CornerRadii.x; set => CornerRadii = new Vector4(value, value, value, value); }

        // Auto layout (mirrors Figma's panel). Layout.None keeps absolute Bounds positioning.
        internal FigmaLayout Layout;
        internal Vector4 Padding;            // left, top, right, bottom
        internal float Gap;                  // Horizontal / Vertical
        internal FigmaAlign MainAlign;       // Horizontal / Vertical: distribution along the direction
        internal FigmaAlign CrossAlign;      // Horizontal / Vertical: placement across (SpaceBetween = Start)

        // Grid. Tracks beyond the defined ones default to 1fr; if ColumnTracks is empty, Columns x 1fr is used.
        internal int Columns = 1;
        internal FigmaTrack[] ColumnTracks;
        internal FigmaTrack[] RowTracks;
        internal float ColumnGap;
        internal float RowGap;

        // Used when this frame is a child of an auto layout / grid frame.
        internal FigmaSize WidthSize;
        internal FigmaSize HeightSize;
        internal float FillWeight = 1f;      // stack: share among Fill siblings
        internal int ColumnSpan = 1;         // grid
        internal int RowSpan = 1;            // grid
        internal FigmaAlign CellAlignX;      // grid: position inside the cell when not Fill
        internal FigmaAlign CellAlignY;

        // Interaction. Any frame can be clickable / hoverable; the Hover*/Pressed* overrides fall back to the
        // previous state (Pressed -> Hover -> idle value). Tint multiplies the colours of this frame and all its
        // descendants, so a button can dim or brighten its whole content at once.
        internal Action Clicked;
        internal Action Hovered;             // called every repaint while the pointer is over the frame
        internal Color Tint = Color.white;
        internal Color? HoverTint, PressedTint;
        internal Color? HoverFill, PressedFill;
        internal Color? HoverBorder, PressedBorder;
        internal FigmaState State { get; private set; }

        // Debug view: every frame is drawn as an outline instead of its fill/border.
        internal static bool Wireframe;
        static readonly Color WireColor = new Color(0f, 1f, 1f, 0.9f);

        internal readonly List<FigmaFrame> Children = new List<FigmaFrame>();

        internal FigmaFrame(Rect bounds)
        {
            Bounds = bounds;
        }

        internal T Add<T>(T child) where T : FigmaFrame
        {
            Children.Add(child);
            return child;
        }

        internal void Render(Rect parent)
        {
            Render(parent, Color.white);
        }

        void Render(Rect parent, Color inherited)
        {
            RenderAt(new Rect(parent.x + Bounds.x, parent.y + Bounds.y, Bounds.width, Bounds.height), inherited);
        }

        void RenderAt(Rect rect, Color inherited)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            State = !hover ? FigmaState.Idle : Input.GetMouseButton(0) ? FigmaState.Pressed : FigmaState.Hover;
            var tint = inherited * StateValue<Color>(Tint, HoverTint, PressedTint).Value;

            Draw(rect, tint);
            if (Wireframe && Event.current.type == EventType.Repaint) Gui.Outline(rect, WireColor, 1f);
            switch (Layout)
            {
                case FigmaLayout.None:
                    for (int i = 0; i < Children.Count; i++) Children[i].Render(rect, tint);
                    break;
                case FigmaLayout.Grid:
                    var cells = LayoutGrid(rect);
                    for (int i = 0; i < Children.Count; i++) Children[i].RenderAt(cells[i], tint);
                    break;
                default:
                    var rects = LayoutStack(rect);
                    for (int i = 0; i < Children.Count; i++) Children[i].RenderAt(rects[i], tint);
                    break;
            }

            // Input is handled after the children so nested clickable frames take priority over their parent.
            if (Clicked != null && GUI.Button(rect, GUIContent.none, GUIStyle.none)) Clicked();
            if (hover && Hovered != null && Event.current.type == EventType.Repaint) Hovered();
        }

        // Picks the value for the current pointer state.
        T? StateValue<T>(T? idle, T? hover, T? pressed) where T : struct
        {
            switch (State)
            {
                case FigmaState.Pressed: return pressed ?? hover ?? idle;
                case FigmaState.Hover: return hover ?? idle;
                default: return idle;
            }
        }

        // Content size, used by Hug.
        protected virtual Vector2 Measure()
        {
            switch (Layout)
            {
                case FigmaLayout.None: return Bounds.size;
                case FigmaLayout.Grid: return MeasureGrid();
                default: return MeasureStack();
            }
        }

        float Intrinsic(bool horizontal)
        {
            if ((horizontal ? WidthSize : HeightSize) == FigmaSize.Fixed)
                return horizontal ? Bounds.width : Bounds.height;
            var size = Measure();
            return horizontal ? size.x : size.y;
        }

        float InnerWidth(Rect rect) => Mathf.Max(0f, rect.width - Padding.x - Padding.z);
        float InnerHeight(Rect rect) => Mathf.Max(0f, rect.height - Padding.y - Padding.w);

        // ---- Horizontal / Vertical ----

        Vector2 MeasureStack()
        {
            bool horizontal = Layout == FigmaLayout.Horizontal;
            float main = Gap * Mathf.Max(0, Children.Count - 1), cross = 0f;
            for (int i = 0; i < Children.Count; i++)
            {
                main += Children[i].Intrinsic(horizontal);
                cross = Mathf.Max(cross, Children[i].Intrinsic(!horizontal));
            }
            return horizontal
                ? new Vector2(main + Padding.x + Padding.z, cross + Padding.y + Padding.w)
                : new Vector2(cross + Padding.x + Padding.z, main + Padding.y + Padding.w);
        }

        Rect[] LayoutStack(Rect rect)
        {
            bool horizontal = Layout == FigmaLayout.Horizontal;
            float mainSize = horizontal ? InnerWidth(rect) : InnerHeight(rect);
            float crossSize = horizontal ? InnerHeight(rect) : InnerWidth(rect);

            int n = Children.Count;
            var main = new float[n];
            float used = 0f, weights = 0f;
            for (int i = 0; i < n; i++)
            {
                var c = Children[i];
                if ((horizontal ? c.WidthSize : c.HeightSize) == FigmaSize.Fill) weights += Mathf.Max(0f, c.FillWeight);
                else used += main[i] = c.Intrinsic(horizontal);
            }

            float free = Mathf.Max(0f, mainSize - used - Gap * Mathf.Max(0, n - 1));
            if (weights > 0f)
            {
                for (int i = 0; i < n; i++)
                {
                    var c = Children[i];
                    if ((horizontal ? c.WidthSize : c.HeightSize) == FigmaSize.Fill)
                        main[i] = free * Mathf.Max(0f, c.FillWeight) / weights;
                }
                free = 0f;
            }

            float gap = Gap, pos = 0f;
            switch (MainAlign)
            {
                case FigmaAlign.Center: pos = free * 0.5f; break;
                case FigmaAlign.End: pos = free; break;
                case FigmaAlign.SpaceBetween: if (n > 1) gap += free / (n - 1); break;
            }

            var rects = new Rect[n];
            for (int i = 0; i < n; i++)
            {
                var c = Children[i];
                bool crossFill = (horizontal ? c.HeightSize : c.WidthSize) == FigmaSize.Fill;
                float cross = crossFill ? crossSize : c.Intrinsic(!horizontal);
                float crossPos = 0f;
                if (CrossAlign == FigmaAlign.Center) crossPos = (crossSize - cross) * 0.5f;
                else if (CrossAlign == FigmaAlign.End) crossPos = crossSize - cross;

                float x = rect.x + Padding.x, y = rect.y + Padding.y;
                rects[i] = horizontal
                    ? new Rect(x + pos, y + crossPos, main[i], cross)
                    : new Rect(x + crossPos, y + pos, cross, main[i]);
                pos += main[i] + gap;
            }
            return rects;
        }

        // ---- Grid ----

        int ColumnCount => ColumnTracks != null && ColumnTracks.Length > 0 ? ColumnTracks.Length : Mathf.Max(1, Columns);

        // Row-major auto placement; returns per child (col, row, colSpan, rowSpan).
        Vector4[] PlaceCells(out int rowCount)
        {
            int cols = ColumnCount, n = Children.Count;
            var cells = new Vector4[n];
            var occupied = new List<bool[]>();
            int cursor = 0;
            rowCount = RowTracks != null ? RowTracks.Length : 0;
            for (int i = 0; i < n; i++)
            {
                int cs = Mathf.Clamp(Children[i].ColumnSpan, 1, cols);
                int rs = Mathf.Max(1, Children[i].RowSpan);
                while (cursor % cols + cs > cols || !IsFree(occupied, cursor % cols, cursor / cols, cs, rs)) cursor++;
                int c0 = cursor % cols, r0 = cursor / cols;
                for (int r = r0; r < r0 + rs; r++)
                {
                    while (occupied.Count <= r) occupied.Add(new bool[cols]);
                    for (int c = c0; c < c0 + cs; c++) occupied[r][c] = true;
                }
                cells[i] = new Vector4(c0, r0, cs, rs);
                rowCount = Mathf.Max(rowCount, r0 + rs);
                cursor += cs;
            }
            return cells;
        }

        static bool IsFree(List<bool[]> occupied, int col, int row, int cs, int rs)
        {
            for (int r = row; r < row + rs && r < occupied.Count; r++)
                for (int c = col; c < col + cs; c++)
                    if (occupied[r][c]) return false;
            return true;
        }

        // Fixed = px, Hug = widest/tallest single-span child, Fr = share of the leftover space.
        // available < 0 means "measuring": fr tracks collapse to their content.
        float[] ResolveTracks(bool horizontal, int count, Vector4[] cells, float available)
        {
            var defs = horizontal ? ColumnTracks : RowTracks;
            float gap = horizontal ? ColumnGap : RowGap;
            var content = new float[count];
            for (int i = 0; i < cells.Length; i++)
            {
                int start = (int)(horizontal ? cells[i].x : cells[i].y);
                float span = horizontal ? cells[i].z : cells[i].w;
                if (span == 1f) content[start] = Mathf.Max(content[start], Children[i].Intrinsic(horizontal));
            }

            var sizes = new float[count];
            float fixedTotal = 0f, frTotal = 0f;
            for (int t = 0; t < count; t++)
            {
                var def = defs != null && t < defs.Length ? defs[t] : FigmaTrack.Fr(1f);
                if (def.Kind == FigmaTrackKind.Fr) frTotal += Mathf.Max(0f, def.Value);
                else fixedTotal += sizes[t] = def.Kind == FigmaTrackKind.Fixed ? def.Value : content[t];
            }

            float free = available < 0f ? 0f : Mathf.Max(0f, available - gap * (count - 1) - fixedTotal);
            for (int t = 0; t < count; t++)
            {
                var def = defs != null && t < defs.Length ? defs[t] : FigmaTrack.Fr(1f);
                if (def.Kind != FigmaTrackKind.Fr) continue;
                sizes[t] = available < 0f ? content[t] : (frTotal > 0f ? free * Mathf.Max(0f, def.Value) / frTotal : 0f);
            }
            return sizes;
        }

        Vector2 MeasureGrid()
        {
            var cells = PlaceCells(out int rows);
            int cols = ColumnCount;
            float w = ColumnGap * (cols - 1) + Padding.x + Padding.z;
            float h = RowGap * Mathf.Max(0, rows - 1) + Padding.y + Padding.w;
            foreach (float v in ResolveTracks(true, cols, cells, -1f)) w += v;
            foreach (float v in ResolveTracks(false, rows, cells, -1f)) h += v;
            return new Vector2(w, h);
        }

        Rect[] LayoutGrid(Rect rect)
        {
            var cells = PlaceCells(out int rows);
            int cols = ColumnCount;
            var colW = ResolveTracks(true, cols, cells, InnerWidth(rect));
            var rowH = ResolveTracks(false, rows, cells, InnerHeight(rect));

            var colX = new float[cols];
            for (int c = 1; c < cols; c++) colX[c] = colX[c - 1] + colW[c - 1] + ColumnGap;
            var rowY = new float[rows];
            for (int r = 1; r < rows; r++) rowY[r] = rowY[r - 1] + rowH[r - 1] + RowGap;

            var rects = new Rect[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                int c0 = (int)cells[i].x, r0 = (int)cells[i].y, cs = (int)cells[i].z, rs = (int)cells[i].w;
                float cellW = colX[c0 + cs - 1] + colW[c0 + cs - 1] - colX[c0];
                float cellH = rowY[r0 + rs - 1] + rowH[r0 + rs - 1] - rowY[r0];

                var child = Children[i];
                float w = child.WidthSize == FigmaSize.Fill ? cellW : child.Intrinsic(true);
                float h = child.HeightSize == FigmaSize.Fill ? cellH : child.Intrinsic(false);
                rects[i] = new Rect(
                    rect.x + Padding.x + colX[c0] + Offset(child.CellAlignX, cellW - w),
                    rect.y + Padding.y + rowY[r0] + Offset(child.CellAlignY, cellH - h),
                    w,
                    h);
            }
            return rects;
        }

        static float Offset(FigmaAlign align, float free)
        {
            return align == FigmaAlign.Center ? free * 0.5f : align == FigmaAlign.End ? free : 0f;
        }

        protected virtual void Draw(Rect rect, Color tint)
        {
            if (Wireframe || Event.current.type != EventType.Repaint) return;
            bool rounded = CornerRadii != Vector4.zero;
            var fill = StateValue<Color>(Fill, HoverFill, PressedFill);
            var border = StateValue<Color>(Border, HoverBorder, PressedBorder);
            var left = BorderLeft ?? border;
            var top = BorderTop ?? border;
            var right = BorderRight ?? border;
            var bottom = BorderBottom ?? border;
            var baseColor = border ?? left ?? top ?? right ?? bottom;
            bool hasBorder = baseColor.HasValue;
            var b = BorderWidths;

            if (hasBorder && !fill.HasValue && !rounded)
            {
                // No fill to punch a hole into, so draw only the sides that have a colour and a width.
                if (top.HasValue) Gui.Round(new Rect(rect.x, rect.y, rect.width, b.y), top.Value * tint, 0f);
                if (bottom.HasValue) Gui.Round(new Rect(rect.x, rect.yMax - b.w, rect.width, b.w), bottom.Value * tint, 0f);
                if (left.HasValue) Gui.Round(new Rect(rect.x, rect.y + b.y, b.x, rect.height - b.y - b.w), left.Value * tint, 0f);
                if (right.HasValue) Gui.Round(new Rect(rect.xMax - b.z, rect.y + b.y, b.z, rect.height - b.y - b.w), right.Value * tint, 0f);
                return;
            }

            if (hasBorder)
            {
                Gui.Round(rect, baseColor.Value * tint, CornerRadii);
                // Sides with their own colour are painted over the base, stopping short of the rounded corners.
                var r = CornerRadii;
                if (BorderTop.HasValue) Gui.Round(new Rect(rect.x + r.x, rect.y, rect.width - r.x - r.y, b.y), BorderTop.Value * tint, 0f);
                if (BorderBottom.HasValue) Gui.Round(new Rect(rect.x + r.w, rect.yMax - b.w, rect.width - r.w - r.z, b.w), BorderBottom.Value * tint, 0f);
                if (BorderLeft.HasValue) Gui.Round(new Rect(rect.x, rect.y + r.x, b.x, rect.height - r.x - r.w), BorderLeft.Value * tint, 0f);
                if (BorderRight.HasValue) Gui.Round(new Rect(rect.xMax - b.z, rect.y + r.y, b.z, rect.height - r.y - r.z), BorderRight.Value * tint, 0f);
            }
            if (fill.HasValue)
            {
                var w = hasBorder ? BorderWidths : Vector4.zero;
                var fillRect = new Rect(rect.x + w.x, rect.y + w.y, rect.width - w.x - w.z, rect.height - w.y - w.w);
                if (fillRect.width > 0f && fillRect.height > 0f)
                {
                    // An inner corner shrinks by the thicker of its two adjoining borders.
                    var inner = new Vector4(
                        Mathf.Max(0f, CornerRadii.x - Mathf.Max(w.x, w.y)),
                        Mathf.Max(0f, CornerRadii.y - Mathf.Max(w.z, w.y)),
                        Mathf.Max(0f, CornerRadii.z - Mathf.Max(w.z, w.w)),
                        Mathf.Max(0f, CornerRadii.w - Mathf.Max(w.x, w.w)));
                    Gui.Round(fillRect, fill.Value * tint, inner);
                }
            }
        }
    }

    internal sealed class FigmaText : FigmaFrame
    {
        internal string Text;
        GUIStyle _style;

        internal int FontSize { get => _style.fontSize; set => _style.fontSize = value; }
        Color _color;
        internal Color Color { get => _color; set => _style.normal.textColor = Gui.TextColor(_color = value); }
        internal TextAnchor Align { get => _style.alignment; set => _style.alignment = value; }
        internal FontStyle FontStyle { get => _style.fontStyle; set => _style.fontStyle = value; }
        internal bool WordWrap { get => _style.wordWrap; set => _style.wordWrap = value; }
        // Enables <b>, <i> and Gui.Colored(...) spans inside Text.
        internal bool RichText { get => _style.richText; set => _style.richText = value; }

        // Owns a private copy of the style, so changing FontSize/Color/Align never affects other texts.
        internal FigmaText(Rect bounds, string text, GUIStyle style) : base(bounds)
        {
            Text = text;
            _style = new GUIStyle(style);
            Color = style.normal.textColor;
        }

        internal FigmaText(Rect bounds, string text, int fontSize, Color color, TextAnchor align = TextAnchor.UpperLeft) : base(bounds)
        {
            Text = text;
            _style = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = align, wordWrap = false };
            Color = color;
        }

        protected override Vector2 Measure() => _style.CalcSize(new GUIContent(Text));

        protected override void Draw(Rect rect, Color tint)
        {
            base.Draw(rect, tint);
            _style.normal.textColor = Gui.TextColor(_color) * tint;
            GUI.Label(rect, Text, _style);
        }
    }

    // A frame that draws a texture (icon) on top of its own fill / border.
    internal sealed class FigmaImage : FigmaFrame
    {
        internal Texture2D Texture;
        internal Color ImageTint = Color.white;      // multiplied with the PNG's colours
        internal ScaleMode Scale = ScaleMode.ScaleToFit;

        internal FigmaImage(Rect bounds, Texture2D texture) : base(bounds)
        {
            Texture = texture;
        }

        protected override void Draw(Rect rect, Color tint)
        {
            base.Draw(rect, tint);
            if (Texture == null || Wireframe || Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(rect, Texture, Scale, true, 0f, ImageTint * tint, 0f, 0f);
        }
    }


    internal sealed class FigmaWindow
    {
        readonly int _id;
        readonly GUI.WindowFunction _draw;
        readonly Vector2 _minimumSize;
        int _resizeCorner = -1;
        Vector2 _resizeStartMouse;
        Rect _resizeStartRect;

        internal Rect Rect { get; private set; }
        internal bool Resizable { get; set; }
        internal float ResizeGrip { get; set; } = 14f;
        internal float ScreenMargin { get; set; } = 40f;

        internal FigmaWindow(int id, Rect initialRect, Vector2 minimumSize, GUI.WindowFunction draw)
        {
            _id = id;
            Rect = initialRect;
            _minimumSize = minimumSize;
            _draw = draw;
        }

        internal void SetSize(float width, float height)
        {
            Rect = new Rect(Rect.x, Rect.y, Mathf.Max(_minimumSize.x, width), Mathf.Max(_minimumSize.y, height));
        }

        internal void UpdateInput(bool active)
        {
            if (!active || !Resizable)
            {
                _resizeCorner = -1;
                return;
            }

            var mouse = Input.mousePosition;
            var position = new Vector2(mouse.x, Screen.height - mouse.y);
            if (_resizeCorner < 0 && Input.GetMouseButtonDown(0))
            {
                bool left = Mathf.Abs(position.x - Rect.xMin) <= ResizeGrip;
                bool right = Mathf.Abs(position.x - Rect.xMax) <= ResizeGrip;
                bool top = Mathf.Abs(position.y - Rect.yMin) <= ResizeGrip;
                bool bottom = Mathf.Abs(position.y - Rect.yMax) <= ResizeGrip;
                if ((left || right) && (top || bottom))
                {
                    _resizeCorner = (right ? 1 : 0) + (bottom ? 2 : 0);
                    _resizeStartMouse = position;
                    _resizeStartRect = Rect;
                }
            }

            if (_resizeCorner < 0) return;
            if (!Input.GetMouseButton(0))
            {
                _resizeCorner = -1;
                return;
            }

            var delta = position - _resizeStartMouse;
            float xMin = _resizeStartRect.xMin;
            float xMax = _resizeStartRect.xMax;
            float yMin = _resizeStartRect.yMin;
            float yMax = _resizeStartRect.yMax;
            if ((_resizeCorner & 1) == 0) xMin = Mathf.Min(_resizeStartRect.xMin + delta.x, xMax - _minimumSize.x);
            else xMax = Mathf.Max(_resizeStartRect.xMax + delta.x, xMin + _minimumSize.x);
            if ((_resizeCorner & 2) == 0) yMin = Mathf.Min(_resizeStartRect.yMin + delta.y, yMax - _minimumSize.y);
            else yMax = Mathf.Max(_resizeStartRect.yMax + delta.y, yMin + _minimumSize.y);
            Rect = UnityEngine.Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        internal void Render()
        {
            float maxWidth = Mathf.Max(_minimumSize.x, Screen.width);
            float maxHeight = Mathf.Max(_minimumSize.y, Screen.height);
            Rect = new Rect(
                Rect.x,
                Rect.y,
                Mathf.Clamp(Rect.width, Mathf.Min(_minimumSize.x, maxWidth), maxWidth),
                Mathf.Clamp(Rect.height, Mathf.Min(_minimumSize.y, maxHeight), maxHeight));
            Rect = GUI.Window(_id, Rect, _draw, "", GUIStyle.none);
            Rect = new Rect(
                Mathf.Clamp(Rect.x, 0f, Mathf.Max(0f, Screen.width - ScreenMargin)),
                Mathf.Clamp(Rect.y, 0f, Mathf.Max(0f, Screen.height - ScreenMargin)),
                Rect.width,
                Rect.height);
        }

        internal bool Contains(Vector2 position)
        {
            return Rect.Contains(position);
        }
    }
}
