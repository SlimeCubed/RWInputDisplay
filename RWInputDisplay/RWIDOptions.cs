using System;
using UnityEngine;
using OptionalUI;
using RWCustom;

namespace RWInputDisplay
{
    public class RWIDOptions : OptionInterface
    {
        public RWIDOptions(Partiality.Modloader.PartialityMod mod) : base(mod: mod) {}

        public override bool Configuable() => true;

        private OpColorPicker _backCol;
        private OpColorPicker _onCol;
        private OpColorPicker _offCol;

        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[] { new OpTab("Config") };
            
            // Title
            Tabs[0].AddItems(new OpLabel(new Vector2(32f, 536f), new Vector2(256f, 32f), "Input Display Options", FLabelAlignment.Left, true));

            const float boolOffset = 150f;

            // Interpolation
            string lerpDesc = "Enable interpolation between frames (True by default)";
            Tabs[0].AddItems(new OpCheckBox(new Vector2(8f, 504f), "rwid_interpolation", true) { description = lerpDesc });
            Tabs[0].AddItems(new OpLabel(new Vector2(34f, 504f - 3f), new Vector2(boolOffset - 32f, 32f), "Interpolation", FLabelAlignment.Left) { description = lerpDesc });

            // RT indicators
            string rtiDesc = "Show real-time input indicators at the bottom left of each button (True by default)";
            Tabs[0].AddItems(new OpCheckBox(new Vector2(8f + boolOffset, 504f), "rwid_rt_indicators", true) { description = rtiDesc });
            Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset, 504f - 3f), new Vector2(boolOffset - 32f, 32f), "Show Realtime Input", FLabelAlignment.Left) { description = rtiDesc });

            // Use outline for label colors
            string oclDesc = "Set button labels as the outline color instead of the opposite on or off color (False by default)";
            Tabs[0].AddItems(new OpCheckBox(new Vector2(8f + boolOffset * 2f, 504f), "rwid_outline_labels", false) { description = oclDesc });
            Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * 2f, 504f - 3f), new Vector2(boolOffset - 32f, 32f), "Outline Color Labels", FLabelAlignment.Left) { description = oclDesc });

            // Show the time stacker
            string stsDesc = "A bar below the input display indicates the current timeStacker (False by default)";
            Tabs[0].AddItems(new OpCheckBox(new Vector2(8f + boolOffset * 3f, 504f), "rwid_show_timestacker", false) { description = stsDesc });
            Tabs[0].AddItems(new OpLabel(new Vector2(34f + boolOffset * 3f, 504f - 3f), new Vector2(boolOffset - 32f, 32f), "Show timeStacker", FLabelAlignment.Left) { description = stsDesc });

            // Color pickers
            _backCol = new OpColorPicker(new Vector2(32f, 314f), "rwid_color_back", "FFFFFF");
            Tabs[0].AddItems(_backCol, new OpLabel(new Vector2(32f, 472f), new Vector2(150f, 16f), "Outline Color"));
            _offCol = new OpColorPicker(new Vector2(225f, 314f), "rwid_color_off", "000000");
            Tabs[0].AddItems(_offCol, new OpLabel(new Vector2(225f, 472f), new Vector2(150f, 16f), "Off Color"));
            _onCol = new OpColorPicker(new Vector2(418f, 314f), "rwid_color_on", "BFBFBF");
            Tabs[0].AddItems(_onCol, new OpLabel(new Vector2(418f, 472f), new Vector2(150f, 16f), "On Color"));

            // Alpha slider
            string aDesc = "How opaque the display is (75 by default)";
            Tabs[0].AddItems(new OpLabel(new Vector2(8f, 280f), new Vector2(40f, 24f), "Alpha", FLabelAlignment.Right) { description = aDesc });
            Tabs[0].AddItems(new OpSlider(new Vector2(8f + 48f, 280f - 3f), "rwid_alpha", new IntVector2(0, 100), length: 200, defaultValue: 75) { description = aDesc });

            // Scale slider
            string sclDesc = "The scale factor of the display (10 by default)";
            Tabs[0].AddItems(new OpLabel(new Vector2(8f, 250f), new Vector2(40f, 24f), "Scale", FLabelAlignment.Right) { description = sclDesc });
            Tabs[0].AddItems(new OpSlider(new Vector2(8f + 48f, 250f - 3f), "rwid_scale", new IntVector2(5, 20), length: 200, defaultValue: 10) { description = sclDesc });
        }

        public override void ConfigOnChange()
        {
            base.ConfigOnChange();
            RWInputDisplay.disableInterpolation = config["rwid_interpolation"] == "false";
            RWInputDisplay.hideRTIndicators = config["rwid_rt_indicators"] == "false";
            RWInputDisplay.outlineLabels = config["rwid_outline_labels"] == "true";
            RWInputDisplay.showTimeStacker = config["rwid_show_timestacker"] == "true";
            RWInputDisplay.alpha = int.TryParse(config["rwid_alpha"], out int res) ? res / 100f : 1f;
            RWInputDisplay.scale = float.TryParse(config["rwid_scale"], out float scl) ? scl / 10f : 1f;
            RWInputDisplay.backColor = OpColorPicker.HexToColor(config["rwid_color_back"]);
            RWInputDisplay.onColor   = OpColorPicker.HexToColor(config["rwid_color_on"  ]);
            RWInputDisplay.offColor  = OpColorPicker.HexToColor(config["rwid_color_off" ]);
        }
    }

    public partial class RWInputDisplay
    {
        public OptionInterface LoadOI() => new RWIDOptions(this);
    }
}
