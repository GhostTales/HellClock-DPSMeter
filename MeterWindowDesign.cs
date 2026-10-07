using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DPSMeter
{
    internal class MeterWindowDesign
    {
        internal static bool IsMouseOver;
        readonly FigmaWindow _window;
        FigmaFrame root;

        public MeterWindowDesign(FigmaWindow window)
        {
            _window = window;
            root = new FigmaFrame(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height))
            {
                Fill = Gui.Bg,
                Border = Gui.Border,
                BorderWidth = 1f,
                CornerRadius = 20f,
                // Root grid: 1 column, header row = 0.25fr, body rows = 0.9fr each, footer row = 0.25fr.
                Layout = FigmaLayout.Grid,
                ColumnTracks = new[] { FigmaTrack.Fr(1f) },
                RowTracks = new[] { FigmaTrack.Fr(0.25f), FigmaTrack.Fr(0.9f), FigmaTrack.Fr(0.9f), FigmaTrack.Fr(0.25f) },
                RowGap = 10f
            };
            //root.Padding = new Vector4(20f, 10f, 20f, 10f);

            // Header: 1x2 grid (1fr | 1fr) that fills the 0.25fr cell.
            var header = root.Add(new FigmaFrame(default)
            {
                Layout = FigmaLayout.Grid,
                ColumnTracks = new[] { FigmaTrack.Fr(1f), FigmaTrack.Fr(1f)},
                ColumnGap = 10f,
                Border = Gui.Border,
                Fill = Gui.Text,
                BorderWidths = new Vector4(0f, 0f, 0f, 1f),
                CornerRadii = new Vector4(20f, 20f, 0f, 0f),
                Padding = new Vector4(10f, 10f, 10f, 10f),
            }.Size(FigmaSize.Fill, FigmaSize.Fill));

            // Header title
            var title = header.Add(new FigmaText(default, "DPS METER", 18, Gui.Primary){
                CellAlignY = FigmaAlign.Center, 
                CellAlignX = FigmaAlign.Start 
                }.Size(FigmaSize.Hug, FigmaSize.Hug));

            // Button row
            var buttonRow = new FigmaFrame(default).Size(FigmaSize.Fill, FigmaSize.Fill);

            buttonRow.Layout = FigmaLayout.Horizontal;
            buttonRow.ColumnGap = 4f;
            buttonRow.RowGap = 4f;
            buttonRow.Padding = new Vector4(2f, 2f, 5f, 5f);
            buttonRow.CellAlignY = FigmaAlign.Center;
            buttonRow.CellAlignX = FigmaAlign.End;

            // Debug button toggles the wireframe mode on and off
            var debugButton = new FigmaButton(default, FigmaFrame.Wireframe ? "Debug: On" : "Debug: Off",
                FigmaFrame.Wireframe ? Gui.ButtonKind.Primary : Gui.ButtonKind.Secondary, 14, ToggleWireframe).Size(FigmaSize.Hug, FigmaSize.Hug);
            buttonRow.Add(debugButton);

            // config gear button goes to config page
            var configButton = new FigmaButton(default, "Config", Gui.ButtonKind.Primary, 14, () => Plugin.Logger.LogInfo("Config clicked")).Size(FigmaSize.Hug, FigmaSize.Hug);
            configButton.CellAlignY = FigmaAlign.Center;
            configButton.CellAlignX = FigmaAlign.End;
            buttonRow.Add(configButton);

            header.Add(buttonRow);
            //header.Add(new FigmaText(default, Tracker.Dps.ToString("N0") + " DPS", HeaderStyle(TextAnchor.MiddleCenter, Gui.Text)).Size(FigmaSize.Fill, FigmaSize.Fill));
            //var reset = header.Add(new FigmaButton(default, "Reset", Gui.ButtonKind.Secondary, 14, Tracker.Reset).Size(FigmaSize.Hug, FigmaSize.Hug));
            //reset.CellAlignY = FigmaAlign.Center;

            var infoBoxTop = new FigmaFrame(default).Size(FigmaSize.Fill, FigmaSize.Fill);
            infoBoxTop.Layout = FigmaLayout.Horizontal;
            infoBoxTop.ColumnGap = 4f;
            infoBoxTop.RowGap = 4f;
            infoBoxTop.Padding = new Vector4(2f, 2f, 5f, 5f);
            infoBoxTop.MainAlign = FigmaAlign.Start;
            infoBoxTop.Fill = Gui.Text;
            root.Add(infoBoxTop);

            var infoBoxMiddle = new FigmaFrame(default).Size(FigmaSize.Fill, FigmaSize.Fill);
            infoBoxMiddle.Layout = FigmaLayout.Horizontal;
            infoBoxMiddle.ColumnGap = 4f;
            infoBoxMiddle.RowGap = 4f;
            infoBoxMiddle.Padding = new Vector4(2f, 2f, 5f, 5f);
            infoBoxMiddle.MainAlign = FigmaAlign.Start;
            infoBoxMiddle.Fill = Gui.Primary;
            root.Add(infoBoxMiddle);

            var infoBoxBottom = new FigmaFrame(default).Size(FigmaSize.Fill, FigmaSize.Fill);
            infoBoxBottom.Layout = FigmaLayout.Horizontal;
            infoBoxBottom.ColumnGap = 4f;
            infoBoxBottom.RowGap = 4f;
            infoBoxBottom.Padding = new Vector4(2f, 2f, 5f, 5f);
            infoBoxBottom.MainAlign = FigmaAlign.Start;
            infoBoxBottom.Fill = Gui.Border;
            root.Add(infoBoxBottom);
        }

        static void ToggleWireframe()
        {
            FigmaFrame.Wireframe = !FigmaFrame.Wireframe;
            Plugin.DebugWireframe.Value = FigmaFrame.Wireframe;
        }
        
        public void Render()
        {
            root.Render(new Rect(0f, 0f, _window.Rect.width, _window.Rect.height));
        }

    }
}