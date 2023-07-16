using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System;
using Newtonsoft.Json;
using Plugin = RWInputDisplay.RWInputDisplay;

namespace RWInputDisplay;

// -FB
public static class SaveDataManager
{
    public class SaveMiscProgression
    {
        public float InputDisplayPosX { get; set; } = 64.0f;
        public float InputDisplayPosY { get; set; } = 64.0f;
    }


    public static SaveMiscProgression GetMiscProgression(this RainWorld rainWorld) => GetMiscProgression(rainWorld.progression.miscProgressionData);
    public static SaveMiscProgression GetMiscProgression(this RainWorldGame game) => GetMiscProgression(game.GetStorySession.saveState.progression.miscProgressionData);
    public static SaveMiscProgression GetMiscProgression(this PlayerProgression.MiscProgressionData data)
    {
        if (!data.GetSaveData().TryGet(Plugin.MOD_ID, out SaveMiscProgression save))
            data.GetSaveData().Set(Plugin.MOD_ID, save = new());

        return save;
    }


    // Following is adapted from SlugBase, code by Vigaro
    public static void ApplySaveDataHooks() => On.PlayerProgression.MiscProgressionData.ToString += MiscProgressionData_ToString; 

    private static string MiscProgressionData_ToString(On.PlayerProgression.MiscProgressionData.orig_ToString orig, PlayerProgression.MiscProgressionData self)
    {
        self.GetSaveData().SaveToStrings(self.unrecognizedSaveStrings);
        return orig(self);
    }

    public static SaveData GetSaveData(this PlayerProgression.MiscProgressionData data) => SaveData.ProgressionData.GetValue(data, mwsd => new(mwsd.unrecognizedSaveStrings));

    public class SaveData
    {
        internal const string SAVE_DATA_PREFIX = $"_{Plugin.MOD_ID}SaveData_";

        internal static readonly ConditionalWeakTable<PlayerProgression.MiscProgressionData, SaveData> ProgressionData = new();

        private readonly Dictionary<string, object> _data;
        private readonly List<string> _unrecognizedSaveStrings;

        internal SaveData(List<string> unrecognizedSaveStrings)
        {
            _data = new Dictionary<string, object>();
            _unrecognizedSaveStrings = unrecognizedSaveStrings;
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var obj) && obj is T castObj)
            {
                value = castObj;
                return true;
            }

            if (LoadStringFromUnrecognizedStrings(key, out var stringValue))
            {
                value = JsonConvert.DeserializeObject<T>(stringValue)!;
                _data[key] = value;
                return true;
            }

            value = default!;
            return false;
        }

        public void Set<T>(string key, T value)
        {
            _data[key] = value!;
        }

        internal void SaveToStrings(List<string> strings)
        {
            foreach (var pair in _data)
            {
                SavePairToStrings(strings, pair.Key, JsonConvert.SerializeObject(pair.Value));
            }
        }

        private static void SavePairToStrings(List<string> strings, string key, string value)
        {
            var prefix = key + SAVE_DATA_PREFIX;
            var dataToStore = prefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

            for (var i = 0; i < strings.Count; i++)
            {
                if (strings[i].StartsWith(prefix))
                {
                    strings[i] = dataToStore;
                    return;
                }
            }

            strings.Add(dataToStore);
        }

        internal bool LoadStringFromUnrecognizedStrings(string key, out string value)
        {
            var prefix = key + SAVE_DATA_PREFIX;

            foreach (var s in _unrecognizedSaveStrings)
            {
                if (s.StartsWith(prefix))
                {
                    value = Encoding.UTF8.GetString(Convert.FromBase64String(s.Substring(prefix.Length)));
                    return true;
                }
            }

            value = default!;
            return false;
        }
    }
}