using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HellClock.RelicInventory;
using HellClock.RelicInventory.Affixes;
using HellClock.RelicInventory.Affixes.Special;
using HellClock.RelicInventory.UniqueRelic;
using HellClock.Skill.Effect;
using UnityEngine;

namespace DPSMeter
{
    // Damage added by relic affixes is reported under the skill's name (or the name of an effect area),
    // never the relic's. This attributes it back to the relic, or to the affix when it is not part of
    // a unique relic, in two ways:
    //  - by effect: the damage was dealt while one of the affix's skill effects was being triggered
    //  - by name: the area that dealt it is named "Skill - Relic - Effect"
    internal static class RelicSources
    {
        internal class Info
        {
            public string Name;
            public Sprite Icon;
        }

        static Dictionary<SkillEffectData, Info> _byEffect;
        static Dictionary<string, Info> _byAreaName;
        static Dictionary<string, Info> _byRelicName;
        static readonly Dictionary<string, Info> Resolved = new Dictionary<string, Info>();
        static readonly HashSet<string> Reported = new HashSet<string>();

        public static Info FromEffect(SkillEffectData effect)
        {
            if (effect == null) return null;
            Build();
            return _byEffect != null && _byEffect.TryGetValue(effect, out var info) ? info : null;
        }

        public static Info FromAreaName(string areaName)
        {
            if (string.IsNullOrEmpty(areaName)) return null;
            Build();
            if (_byAreaName == null) return null;
            if (_byAreaName.TryGetValue(areaName, out var info)) return info;
            if (Resolved.TryGetValue(areaName, out info)) return info;

            info = FromNamePattern(areaName);
            Resolved[areaName] = info;
            if (info == null && areaName.Contains(" - ") && Reported.Add(areaName))
                Plugin.Logger.LogInfo($"[Relics] no relic found for '{areaName}'");
            return info;
        }

        static string Normalize(string s) => Regex.Replace(s ?? "", "[^A-Za-z0-9]", "").ToLowerInvariant();

        static string Middle(string name)
        {
            var parts = name.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 3 ? parts[1].Trim() : null;
        }

        // "Shadow  Dash -  Infested  Skull -  Plague  Explosion": the middle part is the relic
        static Info FromNamePattern(string areaName)
        {
            var parts = areaName.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return null;
            for (int i = 1; i < parts.Length - 1; i++)
                if (_byRelicName.TryGetValue(Normalize(parts[i]), out var info)) return info;
            return null;
        }

        static void Build()
        {
            if (_byEffect != null) return;
            try
            {
                var collection = HellClock.Constants.Collections.RELIC_BASE_DEFINITION_COLLECTION.Reference;
                if (collection == null || collection.Count == 0) return; // not loaded yet, retry on the next hit

                var effects = new Dictionary<SkillEffectData, Info>();
                var areas = new Dictionary<string, Info>();
                var names = new Dictionary<string, Info>();

                foreach (RelicBaseDefinition relicBase in collection)
                {
                    if (!(relicBase is UniqueRelicBaseDefinition relic)) continue;
                    string middle = Middle(relic.name);
                    var info = new Info { Name = Localized(relic.NameLocalizationKey, middle ?? relic.name), Icon = relic.Sprite };
                    AddName(names, info.Name, info);
                    AddName(names, middle, info);
                    if (relic.IntrinsicAffixes == null) continue;
                    foreach (var affix in relic.IntrinsicAffixes)
                    {
                        if (affix == null) continue;
                        AddName(names, Middle(affix.name), info);
                        Register(effects, areas, affix, info);
                    }
                }

                // Affixes outside unique relics (rare/common ones such as Flaming) are named after the affix itself
                var affixes = HellClock.Constants.Collections.RELIC_AFFIX_DEFINITION_COLLECTION.Reference;
                if (affixes != null)
                {
                    foreach (RelicAffixDefinition affix in affixes)
                    {
                        if (!(affix is SkillBehaviorAffixDefinition)) continue;
                        var info = new Info { Name = Localized(affix.NameLocalizationKey, affix.name), Icon = affix.Icon };
                        AddName(names, info.Name, info);
                        Register(effects, areas, affix, info);
                    }
                }

                _byRelicName = names;
                _byAreaName = areas;
                _byEffect = effects;
                Plugin.Logger.LogInfo($"[Relics] mapped {effects.Count} effects, {areas.Count} effect areas and {names.Count} names");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError($"[Relics] lookup build failed: {ex}");
                _byRelicName = new Dictionary<string, Info>();
                _byAreaName = new Dictionary<string, Info>();
                _byEffect = new Dictionary<SkillEffectData, Info>();
            }
        }

        static void AddName(Dictionary<string, Info> names, string name, Info info)
        {
            string key = Normalize(name);
            if (key.Length > 0 && !names.ContainsKey(key)) names[key] = info;
        }

        static void Register(Dictionary<SkillEffectData, Info> effects, Dictionary<string, Info> areas, RelicAffixDefinition affix, Info info)
        {
            try
            {
                var all = (affix as SkillBehaviorAffixDefinition)?.BehaviorData?.AllEffects;
                if (all == null) return;
                foreach (var effect in all)
                {
                    if (effect == null) continue;
                    if (!effects.ContainsKey(effect)) effects[effect] = info;
                    if (!(effect is IEffectAreaEffectData areaEffect)) continue;
                    string areaName = areaEffect.EffectAreaDefinition?.DebugName;
                    if (!string.IsNullOrEmpty(areaName) && !areas.ContainsKey(areaName)) areas[areaName] = info;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"[Relics] could not read '{affix.name}': {ex.Message}");
            }
        }

        static string Localized(string key, string fallback)
        {
            string name = null;
            try { name = RosettaStone.Interpret(key); } catch { }
            return string.IsNullOrWhiteSpace(name) ? fallback : name;
        }
    }
}
