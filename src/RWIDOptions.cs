using UnityEngine;
using Menu.Remix.MixedUI;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI.ValueTypes;
using System;
using System.Text;

namespace RWInputDisplay;

using static RWInputDisplay;

public class RWIDOptions : OptionInterface
{
    private OpColorPicker _backCol;
    private OpColorPicker _onCol;
    private OpColorPicker _offCol;
    private OpRadioButtonGroup layoutModeGroup;
    private OpSimpleButton[] customLayoutButtons;
    private PreviewBox preview;
    private HiddenTextConfig customLayoutConfig;

    public override void Initialize()
    {
        base.Initialize();
        Tabs = new OpTab[] { new OpTab(this) };

        // Title
        Tabs[0].AddItems(new OpLabel(new Vector2(32f, 536f), new Vector2(256f, 32f), "Input Display Options", FLabelAlignment.Left, true));

        const float boolSpacing = 35f;

        // Interpolation
        string lerpDesc = "Enable interpolation between frames (True by default)";
        Tabs[0].AddItems(new OpCheckBox(enableInterpolation, new Vector2(71f, 491f)) { description = lerpDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(100f, 491f - 3f), new Vector2(150f, 32f), "Interpolation", FLabelAlignment.Left) { description = lerpDesc });

        // RT indicators
        string rtiDesc = "Show real-time input indicators at the bottom left of each button (True by default)";
        Tabs[0].AddItems(new OpCheckBox(showRTIndicators, new Vector2(71f, 491f - boolSpacing * 1f)) { description = rtiDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(100f, 491f - 3f - boolSpacing * 1f), new Vector2(150f, 32f), "Show Realtime Input", FLabelAlignment.Left) { description = rtiDesc });

        // Use outline for label colors
        string oclDesc = "Set button labels as the outline color instead of the opposite on or off color (False by default)";
        Tabs[0].AddItems(new OpCheckBox(outlineLabels, new Vector2(71f, 491f - boolSpacing * 2f)) { description = oclDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(100f, 491f - 3f - boolSpacing * 2f), new Vector2(150f, 32f), "Outline Color Labels", FLabelAlignment.Left) { description = oclDesc });

        // Show the time stacker
        string stsDesc = "A bar below the input display indicates the current timeStacker (False by default)";
        Tabs[0].AddItems(new OpCheckBox(showTimeStacker, new Vector2(71f, 491f - boolSpacing * 3f)) { description = stsDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(100f, 491f - 3f - boolSpacing * 3f), new Vector2(150f, 32f), "Show timeStacker", FLabelAlignment.Left) { description = stsDesc });

        // Show the time stacker
        string hpDesc = "Speed up rendering by applying transparency to sprites individually (False by default)";
        Tabs[0].AddItems(new OpCheckBox(highPerformance, new Vector2(71f, 491f - boolSpacing * 4f)) { description = hpDesc });
        Tabs[0].AddItems(new OpLabel(new Vector2(100f, 491f - 3f - boolSpacing * 4f), new Vector2(150f, 32f), "High Performance", FLabelAlignment.Left) { description = hpDesc });

        // Color pickers
        _backCol = new OpColorPicker(backColor, new Vector2(32f, 159f));
        Tabs[0].AddItems(_backCol, new OpLabel(new Vector2(32f, 317f), new Vector2(150f, 16f), "Outline Color"));
        _offCol = new OpColorPicker(offColor, new Vector2(225f, 159f));
        Tabs[0].AddItems(_offCol, new OpLabel(new Vector2(225f, 317f), new Vector2(150f, 16f), "Off Color"));
        _onCol = new OpColorPicker(onColor, new Vector2(418f, 159f));
        Tabs[0].AddItems(_onCol, new OpLabel(new Vector2(418f, 317f), new Vector2(150f, 16f), "On Color"));

        // Alpha slider
        string aDesc = "How opaque the display is (1.00 by default)";
        Tabs[0].AddItems(new OpLabel(new Vector2(278f, 500f), new Vector2(40f, 24f), "Alpha", FLabelAlignment.Right) { description = aDesc });
        Tabs[0].AddItems(new OpFloatSlider(alpha, new Vector2(278f + 48f, 500f - 3f), 200, 2) { description = aDesc });

        // Scale slider (30 more y on alpha compared to scale)
        string sclDesc = "The scale factor of the display (0.50 by default)";
        Tabs[0].AddItems(new OpLabel(new Vector2(278f, 470f), new Vector2(40f, 24f), "Scale", FLabelAlignment.Right) { description = sclDesc });
        Tabs[0].AddItems(new OpFloatSlider(scale, new Vector2(278f + 48f, 470f - 3f), 200, 2) { description = sclDesc });

        // Rebind moving all of the input display thingies
        string pkDesc = "The key which will move the input display";
        Tabs[0].AddItems(new OpLabel(new Vector2(315f, 434f), new Vector2(40f, 24f), "Position Key", FLabelAlignment.Right) { description = pkDesc });
        Tabs[0].AddItems(new OpKeyBinder(positionKey, new Vector2(315f + 48f, 430f), new Vector2(140f, 20f), false, OpKeyBinder.BindController.AnyController) { description = pkDesc });


        // Layout options
        string defaultDesc = "The default button layout: jump, throw, grab, special, and movement.";
        string classicDesc = "A slimmer layout: same as Default, but without the special button.";
        string customDesc = "A layout defined by JSON.";

        customLayoutConfig = new HiddenTextConfig(customLayout);
        Tabs[0].AddItems(customLayoutConfig);

        layoutModeGroup = new OpRadioButtonGroup(layoutMode);
        var buttons = new OpRadioButton[] {
            new OpRadioButton(71f, 80f - boolSpacing * 0f) { description = defaultDesc },
            new OpRadioButton(71f, 80f - boolSpacing * 1f) { description = classicDesc },
            new OpRadioButton(71f, 80f - boolSpacing * 2f) { description = customDesc }
        };
        layoutModeGroup.SetButtons(buttons);

        Tabs[0].AddItems(
            new OpLabel(76f, 114f, "Layout", true),
            new OpLabel(100f, 80f + 2f - boolSpacing * 0f, "Default") { description = defaultDesc },
            new OpLabel(100f, 80f + 2f - boolSpacing * 1f, "Classic") { description = classicDesc },
            new OpLabel(100f, 80f + 2f - boolSpacing * 2f, "Custom") { description = customDesc },
            layoutModeGroup
        );
        Tabs[0].AddItems(buttons);

        var copyCustomLayout = new OpSimpleButton(new Vector2(236f, 0f), new Vector2(100f, 24f))
        {
            text = "Copy Layout",
            description = "Copy the current custom layout to the clipboard as JSON."
        };
        var pasteCustomLayout = new OpSimpleButton(new Vector2(351f, 0f), new Vector2(100f, 24f))
        {
            text = "Paste Layout",
            description = "Replace the current custom layout with JSON from the clipboard."
        };
        var customLayoutInfo = new OpSimpleImageButton(new Vector2(466f, 0f), new Vector2(24f, 24f), "Menu_InfoI")
        {
            description = "View examples and guide for making custom layouts."
        };
        customLayoutButtons = new OpSimpleButton[] { copyCustomLayout, pasteCustomLayout, customLayoutInfo };
        Tabs[0].AddItems(customLayoutButtons);

        copyCustomLayout.OnClick += _ => CopyCustomLayout();
        pasteCustomLayout.OnClick += _ => PasteCustomLayout();
        customLayoutInfo.OnClick += _ =>
        {
            var manager = RWCustom.Custom.rainWorld.processManager;
            var dialog = new DialogConfirm("Show layouts folder in file explorer?", new Vector2(300f, 120f), manager, ShowCustomLayoutExamples, null);
            dialog.okButton.menuLabel.text = "Open Folder";
            manager.ShowDialog(dialog);
        };

        preview = new PreviewBox(new Vector2(200f, 36f), new Vector2(330f, 104f));
        Tabs[0].AddItems(preview);
        preview.Layout = GetCurrentLayout();

        layoutModeGroup.OnValueChanged += (_, _, _) => LayoutModeChanged(layoutModeGroup.GetValueInt());
        LayoutModeChanged(layoutMode.Value);
    }

    private Layout GetSelectedLayout()
    {
        switch ((LayoutMode)layoutModeGroup.GetValueInt())
        {
            default:
            case LayoutMode.Default: return defaultLayout;
            case LayoutMode.Classic: return classicLayout;
            case LayoutMode.Custom:
                var layout = new Layout();
                try
                {
                    layout.FromJson(customLayoutConfig.SafeValue);
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                    layout = defaultLayout;
                }
                return layout;
        }
    }

    private void LayoutModeChanged(int option)
    {
        bool isCustom = option == (int)LayoutMode.Custom;
        foreach (var button in customLayoutButtons)
        {
            if (isCustom) button.Show();
            else button.Hide();
        }

        preview.size = new Vector2(preview.size.x, isCustom ? 104f : 140f);
        preview.PosY = isCustom ? 36f : 0f;
        preview.Layout = GetSelectedLayout();
    }

    private void CopyCustomLayout()
    {
        UniClipboard.SetText(customLayoutConfig.SafeValue);
    }

    private void PasteCustomLayout()
    {
        var json = UniClipboard.GetText();

        // Make sure the layout can actually be loaded first
        try
        {
            var layout = new Layout();
            layout.FromJson(json);
        }
        catch(Exception e)
        {
            Logger.LogWarning("Failed to paste custom layout!");
            Logger.LogWarning(e);

            var manager = RWCustom.Custom.rainWorld.processManager;
            var notify = new DialogNotify("Failed to paste layout!\nMake sure your JSON is formatted correctly.", manager, null);
            manager.ShowDialog(notify);
            return;
        }

        customLayoutConfig.SafeValue = json;
        preview.Layout = GetSelectedLayout();
    }

    private static void ShowCustomLayoutExamples()
    {
        var path = AssetManager.ResolveDirectory("rwid-layouts");
        Application.OpenURL(new Uri(path).AbsoluteUri);
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
        positionKey = config.Bind("position_key", KeyCode.LeftBracket);
        layoutMode = config.Bind("layout_mode", (int)LayoutMode.Default);

        var defaultBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(fullLayoutJson));
        var info = new ConfigurableInfo("Base64-encoded JSON that defines a display layout.");
        customLayout = config.Bind("custom_layout", defaultBase64, info: info);

        originX = config.Bind("origin_x", 64f);
        originY = config.Bind("origin_y", 64f);
    }

    public override string ValidationString()
    {
        return base.ValidationString() + (highPerformance.Value ? " HP" : "");
    }

    private class PreviewBox : UIelement
    {
        public Layout Layout
        {
            get => layout;
            set
            {
                if (layout != value)
                {
                    layout = value;
                    Refresh();
                }
            }
        }
        private Layout layout;
        private FContainer layoutContainer;

        public PreviewBox(Vector2 pos, Vector2 size) : base(pos, size)
        {
            layoutContainer = new FContainer();
            myContainer.AddChild(layoutContainer);
        }

        public void Refresh()
        {
            var layoutRect = LayoutBounds;
            var uiRect = new Rect(Vector2.zero, size);

            // Scale layout to fit UI element bounds
            float scale;
            Vector2 offset;
            if (layoutRect.width / uiRect.width > layoutRect.height / uiRect.height)
            {
                // Center layout vertically
                scale = uiRect.width / layoutRect.width;
                offset = new Vector2(0f, (uiRect.height - layoutRect.height * scale) / 2f - layoutRect.y * scale);
            }
            else
            {
                // Center layout horizontally
                scale = uiRect.height / layoutRect.height;
                offset = new Vector2((uiRect.width - layoutRect.width * scale) / 2f - layoutRect.x * scale, 0f);
            }
            layoutContainer.SetPosition(offset);
            layoutContainer.scale = scale;

            // Create key sprites
            layoutContainer.RemoveAllChildren();
            Color color = Color.gray;

            if (layout != null)
            {
                const float buttonSize = 6f / 7f;
                foreach (var pair in layout.keys)
                {
                    var key = pair.Value;
                    var sprite = new FSprite("pixel") { anchorX = 0f, anchorY = 0f };
                    sprite.SetPosition(key.pos);
                    sprite.width = buttonSize;
                    sprite.height = buttonSize;
                    sprite.color = color;
                    layoutContainer.AddChild(sprite);

                    var text = new FLabel("font", pair.Key);
                    text.scale = 1f / scale;
                    text.SetAnchor(0.5f, 0.5f);
                    text.SetPosition(key.pos + buttonSize / 2f * Vector2.one);
                    layoutContainer.AddChild(text);
                }

                // Create analog circle
                if (layout.analog != null)
                {
                    var sprite = new FSprite("atlases/inputdisplay/analogcircle") { anchorX = 0f, anchorY = 0f };
                    sprite.SetPosition(layout.analog.pos);
                    sprite.width = buttonSize;
                    sprite.height = buttonSize;
                    sprite.color = color;
                    layoutContainer.AddChild(sprite);
                }
            }
        }

        private Rect LayoutBounds
        {
            get
            {
                if (layout == null)
                    return new Rect(0f, 0f, 1f, 1f);

                Rect bounds = default;
                void ExpandBounds(Rect add)
                {
                    if (bounds != default)
                    {
                        bounds = Rect.MinMaxRect(
                            Math.Min(bounds.xMin, add.xMin),
                            Math.Min(bounds.yMin, add.yMin),
                            Math.Max(bounds.xMax, add.xMax),
                            Math.Max(bounds.yMax, add.yMax)
                        );
                    }
                    else
                    {
                        bounds = add;
                    }
                }

                Vector2 buttonSize = 6f / 7f * Vector2.one;
                foreach (var key in layout.keys.Values)
                    ExpandBounds(new Rect(key.pos, buttonSize));

                if (layout.analog != null)
                    ExpandBounds(new Rect(layout.analog.pos, buttonSize));
                return bounds;
            }
        }
    }

    private class HiddenTextConfig : UIconfig
    {
        public HiddenTextConfig(ConfigurableBase config) : base(config, Vector2.zero, Vector2.zero)
        {
            Hide();
        }

        // Remix doesn't handle escaping properly in some circumstances
        // Convert text to base 64 to make sure escaping does nothing
        public string SafeValue
        {
            get
            {
                try
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(value));
                }
                catch
                {
                    return defaultValue;
                }
            }
            set => this.value = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }
    }
}
