using System;
using UnityEngine;
using RWCustom;
using Menu.Remix.MixedUI;

namespace RWInputDisplay
{
    using static RWInputDisplay;

    public class RWIDOptions : OptionInterface
    {
        private OpColorPicker _backCol;
        private OpColorPicker _onCol;
        private OpColorPicker _offCol;

        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[] { new OpTab(this) };
            
            // Title
            Tabs[0].AddItems(new OpLabel(new Vector2(32f, 536f), new Vector2(256f, 32f), "Input Display Options", FLabelAlignment.Left, true));

            const float boolSpacing = 35f;

            // Interpolation
            string lerpDesc = "Enable interpolation between frames (True by default)";
            Tabs[0].AddItems(new OpCheckBox(enableInterpolation, new Vector2(75f, 491f)) { description = lerpDesc });
            Tabs[0].AddItems(new OpLabel(new Vector2(100f, 491f - 3f), new Vector2(150f, 32f), "Interpolation", FLabelAlignment.Left) { description = lerpDesc });

            // RT indicators
            string rtiDesc = "Show real-time input indicators at the bottom left of each button (True by default)";
            Tabs[0].AddItems(new OpCheckBox(showRTIndicators, new Vector2(75f, 491f - boolSpacing * 1f)) { description = rtiDesc });
            Tabs[0].AddItems(new OpLabel(new Vector2(100f, 491f - 3f - boolSpacing * 1f), new Vector2(150f, 32f), "Show Realtime Input", FLabelAlignment.Left) { description = rtiDesc });

            // Use outline for label colors
            string oclDesc = "Set button labels as the outline color instead of the opposite on or off color (False by default)";
            Tabs[0].AddItems(new OpCheckBox(outlineLabels, new Vector2(75f, 491f - boolSpacing * 2f)) { description = oclDesc });
            Tabs[0].AddItems(new OpLabel(new Vector2(100f, 491f - 3f - boolSpacing * 2f), new Vector2(150f, 32f), "Outline Color Labels", FLabelAlignment.Left) { description = oclDesc });

            // Show the time stacker
            string stsDesc = "A bar below the input display indicates the current timeStacker (False by default)";
            Tabs[0].AddItems(new OpCheckBox(showTimeStacker, new Vector2(75f, 491f - boolSpacing * 3f)) { description = stsDesc });
            Tabs[0].AddItems(new OpLabel(new Vector2(100f, 491f - 3f - boolSpacing * 3f), new Vector2(150f, 32f), "Show timeStacker", FLabelAlignment.Left) { description = stsDesc });

            // Show the time stacker
            string hpDesc = "Speed up rendering by applying transparency to sprites individually (False by default)";
            Tabs[0].AddItems(new OpCheckBox(highPerformance, new Vector2(75f, 491f - boolSpacing * 4f)) { description = hpDesc });
            Tabs[0].AddItems(new OpLabel(new Vector2(100f, 491f - 3f - boolSpacing * 4f), new Vector2(150f, 32f), "High Performance", FLabelAlignment.Left) { description = hpDesc });

            // Color pickers
            _backCol = new OpColorPicker(backColor, new Vector2(32f, 159f));
            Tabs[0].AddItems(_backCol, new OpLabel(new Vector2(32f, 317f), new Vector2(150f, 16f), "Outline Color"));
            _offCol = new OpColorPicker(offColor, new Vector2(225f, 159f));
            Tabs[0].AddItems(_offCol, new OpLabel(new Vector2(225f, 317f), new Vector2(150f, 16f), "Off Color"));
            _onCol = new OpColorPicker(onColor, new Vector2(418f, 159f));
            Tabs[0].AddItems(_onCol, new OpLabel(new Vector2(418f, 317f), new Vector2(150f, 16f), "On Color"));

            // Alpha slider
            string aDesc = "How opaque the display is (0.75 by default)";
            Tabs[0].AddItems(new OpLabel(new Vector2(278f, 439f), new Vector2(40f, 24f), "Alpha", FLabelAlignment.Right) { description = aDesc });
            Tabs[0].AddItems(new OpFloatSlider(alpha, new Vector2(278f + 48f, 439f - 3f),  200, 2) { description = aDesc });

            // Scale slider
            string sclDesc = "The scale factor of the display (0.50 by default)";
            Tabs[0].AddItems(new OpLabel(new Vector2(278f, 409f), new Vector2(40f, 24f), "Scale", FLabelAlignment.Right) { description = sclDesc });
            Tabs[0].AddItems(new OpFloatSlider(scale, new Vector2(278f + 48f, 409f - 3f), 200, 2) { description = sclDesc });
        }

        public RWIDOptions()
        {
            enableInterpolation = config.Bind("enable_lerp", true);
            showRTIndicators = config.Bind("show_rt", true);
            outlineLabels = config.Bind("outline_color_labels", false);
            showTimeStacker = config.Bind("show_time_stacker", false);
            highPerformance = config.Bind("high_performance", false);
            alpha = config.Bind("alpha", 1f);
            scale = config.Bind("scale", 0.5f);
            backColor = config.Bind("back_color", Color.white);
            onColor = config.Bind("on_color", new Color(0.75f, 0.75f, 0.75f));
            offColor = config.Bind("off_color", new Color(0.1f, 0.1f, 0.1f));

            originX = config.Bind("origin_x", 64f);
            originY = config.Bind("origin_y", 64f);
        }

        public override string ValidationString()
        {
            return base.ValidationString() + (highPerformance.Value ? " HP" : "");
        }
    }
}
