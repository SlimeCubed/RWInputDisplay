using System;
using System.Collections.Generic;
using UnityEngine;
using JsonObj = System.Collections.Generic.Dictionary<string, object>;
using Inputs = Player.InputPackage;

namespace RWInputDisplay;

public class Layout
{
    private static readonly Dictionary<string, Key> defaultKeys = new()
    {
        { "fastroll", new(i => (i.analogueDir.y >= -0.5f && i.analogueDir.y < -0.05f)) { text = "FR", abbreviate = false } },
        { "special", new(i => i.spec) { text = "Spec" } },
        { "grab", new(i => i.pckp) { text = "Grab" }  },
        { "throw", new(i => i.thrw) { text = "Throw" }  },
        { "jump", new(i => i.jmp) { text = "Jump" }  },
        { "up", new(i => i.y == 1) { sprite = "ShortcutArrow", spriteAngle = 0f } },
        { "right", new(i => i.x == 1) { sprite = "ShortcutArrow", spriteAngle = 90f } },
        { "down", new(i => i.y == -1) { sprite = "ShortcutArrow", spriteAngle = 180f } },
        { "left", new(i => i.x == -1) { sprite = "ShortcutArrow", spriteAngle = 270f } },

        // Unused by default
        { "map", new(i => i.mp) { text = "Map" } },
    };
    private static readonly Analog defaultAnalog = new();

    public Dictionary<string, Key> keys = new();
    public Analog analog;

    public Layout()
    {
        Reset();
    }

    public void Reset()
    {
        keys.Clear();
        analog = null;
    }

    public string ToJson()
    {
        var dict = new JsonObj();

        foreach (var pair in keys)
            dict[pair.Key] = pair.Value.ToDictionary(defaultKeys[pair.Key]);

        if (analog != null)
            dict["analog"] = analog.ToDictionary(defaultAnalog);

        return Json.Serialize(dict);
    }

    public void FromJson(string json)
    {
        try
        {
            Reset();
            var dict = (JsonObj)Json.Deserialize(json);

            foreach (var key in defaultKeys.Keys)
            {
                if (dict.TryGetValue(key, out var keyJson))
                {
                    keys[key] = defaultKeys[key].Clone();
                    keys[key].ApplyDictionary((JsonObj)keyJson);
                }
            }

            if (dict.TryGetValue("analog", out var analogJson))
            {
                analog = defaultAnalog.Clone();
                analog.ApplyDictionary((JsonObj)analogJson);
            }

        }
        catch
        {
            Reset();
            throw;
        }
    }

    private static void Apply<T>(JsonObj json, string key, ref T value)
    {
        if (json.TryGetValue(key, out var rawValue))
        {
            if (rawValue is T valueT)
                value = valueT;
            else
                value = (T)Convert.ChangeType(rawValue, typeof(T));
        }
    }

    private static void Save<T>(JsonObj json, string key, T value, T baseValue)
    {
        if (!value.Equals(baseValue))
            json[key] = value;
    }

    public class Key
    {
        public Func<Inputs, bool> inputGetter;

        public bool abbreviate = true;
        public Vector2 pos;
        public string text;
        public string sprite;
        public float spriteAngle;

        public Key(Func<Inputs, bool> inputGetter)
        {
            this.inputGetter = inputGetter;
        }

        public JsonObj ToDictionary(Key parent)
        {
            var dict = new JsonObj();

            Save(dict, "abbreviate", abbreviate, parent.abbreviate);
            Save(dict, "x", pos.x, parent.pos.x);
            Save(dict, "y", pos.y, parent.pos.y);
            Save(dict, "text", text, parent.text);
            Save(dict, "sprite", sprite, parent.sprite);
            Save(dict, "sprite_angle", spriteAngle, parent.spriteAngle);

            return dict;
        }

        public void ApplyDictionary(JsonObj json)
        {
            Apply(json, "abbreviate", ref abbreviate);
            Apply(json, "x", ref pos.x);
            Apply(json, "y", ref pos.y);
            Apply(json, "text", ref text);
            Apply(json, "sprite", ref sprite);
            Apply(json, "sprite_angle", ref spriteAngle);
        }

        public Key Clone()
        {
            return new Key(inputGetter)
            {
                abbreviate = abbreviate,
                pos = pos,
                text = text,
                sprite = sprite,
                spriteAngle = spriteAngle
            };
        }
    }

    public class Analog
    {
        public Vector2 pos;

        public JsonObj ToDictionary(Analog parent)
        {
            var dict = new JsonObj();

            Save(dict, "x", pos.x, parent.pos.x);
            Save(dict, "y", pos.y, parent.pos.y);

            return dict;
        }

        public void ApplyDictionary(JsonObj json)
        {
            Apply(json, "x", ref pos.x);
            Apply(json, "y", ref pos.y);
        }

        public Analog Clone()
        {
            return new Analog()
            {
                pos = pos
            };
        }
    }
}
