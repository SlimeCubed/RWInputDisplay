using BepInEx;
using BepInEx.Logging;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Text;
using UnityEngine;
using Inputs = Player.InputPackage;

[assembly: AssemblyVersion(RWInputDisplay.RWInputDisplay.MOD_VERSION)]
[assembly: AssemblyFileVersion(RWInputDisplay.RWInputDisplay.MOD_VERSION)]
[assembly: AssemblyCompany("Slime_Cubed")]

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace RWInputDisplay;

[BepInPlugin(MOD_ID, "Input Display", MOD_VERSION)]
public class RWInputDisplay : BaseUnityPlugin
{
    public const string MOD_ID = "slime-cubed.inputdisplay";
    public const string MOD_VERSION = "2.3.0";

    public static InputGraphic[] inputGraphics = new InputGraphic[1];
    public static Configurable<bool> enableInterpolation;
    public static Configurable<bool> showRTIndicators;
    public static Configurable<bool> outlineLabels;
    public static Configurable<bool> showTimeStacker;
    public static Configurable<bool> highPerformance;
    public static Configurable<int> layoutMode;
    public static Configurable<string> customLayout;
    public static Configurable<float> alpha;
    public static Configurable<Color> backColor;
    public static Configurable<Color> onColor;
    public static Configurable<Color> offColor;
    public static Configurable<float> scale;
    public static Configurable<float> originX;
    public static Configurable<float> originY;
    public static Configurable<KeyCode> positionKey;
    public static float Scale => scale.Value * 2f;
    public static new ManualLogSource Logger { get; private set; }

    public static string fullLayoutJson;
    public static Layout defaultLayout;
    public static Layout classicLayout;

    public static LayoutMode LayoutMode
    {
        get => (LayoutMode)layoutMode.Value;
        set => layoutMode.Value = (int)value;
    }

    public static Vector2 Origin
    {
        get => new Vector2(originX.Value, originY.Value);
        set
        {
            if (originX.Value != value.x || originY.Value != value.y)
            {
                originDirty = true;
                originX.Value = value.x;
                originY.Value = value.y;
            }
        }
    }

    private static bool originDirty;
    private static RWIDOptions options;
    private bool initialized;

    public void Awake()
    {
        Logger = base.Logger;

        On.RainWorld.OnModsInit += (orig, self) =>
        {
            orig(self);

            try
            {
                defaultLayout = new Layout();
                classicLayout = new Layout();

                fullLayoutJson = File.ReadAllText(AssetManager.ResolveFilePath("rwid-layouts/full.json"));
                defaultLayout.FromJson(File.ReadAllText(AssetManager.ResolveFilePath("rwid-layouts/default.json")));
                classicLayout.FromJson(File.ReadAllText(AssetManager.ResolveFilePath("rwid-layouts/classic.json")));

                if (options == null || MachineConnector.GetRegisteredOI(MOD_ID) != options)
                {
                    MachineConnector.SetRegisteredOI(MOD_ID, options = new RWIDOptions());
                }

                if (initialized) return;
                initialized = true;

                Futile.atlasManager.LoadImage("atlases/inputdisplay/analogcircle");

                On.RoomCamera.ctor += RoomCamera_ctor;
                On.RoomCamera.ClearAllSprites += RoomCamera_ClearAllSprites;
                On.RainWorldGame.GrafUpdate += RainWorldGame_GrafUpdate;
                Application.quitting += Application_quitting;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        };
    }

    public static Layout GetCurrentLayout()
    {
        switch (LayoutMode)
        {
            default:
            case LayoutMode.Default: return defaultLayout;
            case LayoutMode.Classic: return classicLayout;
            case LayoutMode.Custom:
                var custom = new Layout();
                try
                {
                    custom.FromJson(Encoding.UTF8.GetString(Convert.FromBase64String(customLayout.Value)));
                    return custom;
                }
                catch (Exception e)
                {
                    Logger.LogError("Failed to load custom layout!");
                    Logger.LogError(e);
                    return defaultLayout;
                }
        }
    }

    private void Application_quitting()
    {
        if (originDirty)
        {
            MachineConnector.SaveConfig(options);
        }
    }

    private void RoomCamera_ClearAllSprites(On.RoomCamera.orig_ClearAllSprites orig, RoomCamera self)
    {
        if (inputGraphics[self.cameraNumber]?.cam == self)
        {
            inputGraphics[self.cameraNumber]?.Remove();
            inputGraphics[self.cameraNumber] = null;
        }
        orig(self);
    }

    private void RainWorldGame_GrafUpdate(On.RainWorldGame.orig_GrafUpdate orig, RainWorldGame self, float timeStacker)
    {
        if (!enableInterpolation.Value) timeStacker = 1f;
        orig(self, timeStacker);
        foreach (InputGraphic display in inputGraphics)
            display?.Update(timeStacker);
    }

    private void RoomCamera_ctor(On.RoomCamera.orig_ctor orig, RoomCamera self, RainWorldGame game, int cameraNumber)
    {
        orig(self, game, cameraNumber);

        if (inputGraphics.Length <= cameraNumber) Array.Resize(ref inputGraphics, cameraNumber + 1);
        inputGraphics[self.cameraNumber]?.Remove();
        InputGraphic ig = new InputGraphic(self, GetCurrentLayout());
        inputGraphics[cameraNumber] = ig;
        ig.Move();
    }

    public class InputGraphic
    {
        public RoomCamera cam;
        public List<InputButton> buttons;
        public InputAnalog analog;
        public LerpBar lerpBar;
        public Inputs rtInput;

        public bool IsMouseOver
        {
            get
            {
                foreach (InputButton button in buttons) if (button.IsMouseOver) return true;
                return analog != null && analog.IsMouseOver;
            }
        }
        private bool _dragging;
        private Vector2 _dragOffset;

        private Camera _rtCam;
        private Rect _rtBounds;
        private RenderTexture _rt;
        private Layout _layout;
        private int _index;

        public FSprite displaySprite;
        public FContainer buttonContainer;

        public Inputs CurrentInput
        {
            get
            {
                if (cam == null)
                    return RWInput.PlayerUIInput(0);
                else if (cam.game.Players.Count > 0 && cam.game.Players[0].realizedCreature is Player ply)
                    return ply.input[0];
                else
                    return new Inputs();
            }
        }

        private InputGraphic(RoomCamera cam, int index, Layout layout)
        {
            this.cam = cam;
            _index = index;
            _layout = layout;

            if (!highPerformance.Value && alpha.Value < 1f)
            {
                GameObject go = new GameObject("Input Display Camera");
                _rtCam = go.AddComponent<Camera>();
                _rtCam.depth = -100;
                _rtCam.orthographic = true;
                _rtCam.farClipPlane = 20f;
                _rtCam.nearClipPlane = 0.1f;
                _rtCam.clearFlags = CameraClearFlags.SolidColor;
            }

            buttonContainer = new FContainer();
            Futile.stage.AddChild(buttonContainer);

            InitSprites();
        }

        public InputGraphic(RoomCamera cam, Layout layout) : this(cam, cam.cameraNumber, layout)
        {
        }

        public InputGraphic(int index, Layout layout) : this(null, index, layout)
        {

        }

        public void InitSprites()
        {
            float spacing = InputButton.Size + Mathf.Floor(InputButton.Size / 6f);
            buttons = new List<InputButton>();

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

            // Create button sprites based on the current layout
            foreach (var key in _layout.keys.Values)
            {
                var button = new InputButton(this, key.pos * spacing, key);
                buttons.Add(button);
                ExpandBounds(button.Bounds);
            }

            // Analogue display
            if (_layout.analog != null)
            {
                analog = new InputAnalog(this, _layout.analog.pos * spacing, _layout.analog);
                ExpandBounds(analog.Bounds);
            }

            // timeStacker display
            if (showTimeStacker.Value)
            {
                lerpBar = new LerpBar(this, new Vector2(bounds.xMin, bounds.yMin - 8f), bounds.width);
                ExpandBounds(lerpBar.Bounds);
            }

            _rtBounds = Rect.MinMaxRect(
                Mathf.Floor(bounds.xMin) - 4f,
                Mathf.Floor(bounds.yMin) - 4f,
                Mathf.Ceil(bounds.xMax) + 4f,
                Mathf.Ceil(bounds.yMax) + 4f
            );
            _rtBounds.width = Mathf.Ceil(_rtBounds.width / 2f) * 2f;
            _rtBounds.height = Mathf.Ceil(_rtBounds.height / 2f) * 2f;

            Move();
        }

        public void Remove()
        {
            buttonContainer.RemoveFromContainer();

            if (_rtCam)
            {
                Futile.atlasManager.UnloadAtlas("InputDisplay_" + _index);
                displaySprite.RemoveFromContainer();
                Destroy(_rtCam.gameObject);
            }
        }

        public void Update(float timeStacker)
        {
            // Move the input display when left bracket is pressed
            if (Input.GetKey(positionKey.Value))
            {
                Origin = Input.mousePosition;
                Move();
            }

            // Allow dragging the input display
            if (_dragging)
            {
                if (!Input.GetMouseButton(0))
                    _dragging = false;
                else
                {
                    Origin = (Vector2)Input.mousePosition + _dragOffset;
                    Move();
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0) && IsMouseOver)
                {
                    _dragging = true;
                    _dragOffset = Origin - (Vector2)Input.mousePosition;
                }
            }

            // Change the lerp bar to display the current timeStacker
            lerpBar?.Update(timeStacker);

            // Cache the inputs at the start of the frame. It is not going to change while the buttons are updating
            rtInput = RWInput.PlayerInput(0);
            foreach (InputButton button in buttons)
                button.Update();

            // Update the analog input
            analog?.Update();
            

            var container = cam?.ReturnFContainer("HUD2") ?? Futile.stage;
            if (_rtCam)
            {
                if (displaySprite.container != container)
                    container.AddChild(displaySprite);
                displaySprite.MoveToFront();
            }
            else
            {
                if (buttonContainer.container != container)
                    container.AddChild(buttonContainer);
            }

            buttonContainer.MoveToFront();
        }

        private Vector2 OffscreenOrigin => new Vector2(-70000f, -70000f - _index * 1000f);

        public void Move()
        {
            // Update RT and camera
            int rtW = Mathf.RoundToInt(_rtBounds.width);
            int rtH = Mathf.RoundToInt(_rtBounds.height);

            if (_rtCam)
            {
                if (_rt == null)
                {
                    _rt = new RenderTexture(rtW, rtH, 16);
                    _rt.filterMode = FilterMode.Point;

                    if (displaySprite != null)
                    {
                        Futile.atlasManager.UnloadAtlas("InputDisplay_" + _index);
                        displaySprite?.RemoveFromContainer();
                    }

                    FAtlasElement element = Futile.atlasManager.LoadAtlasFromTexture("InputDisplay_" + _index, _rt, false).elements[0];
                    displaySprite = new FSprite(element) { anchorX = 0f, anchorY = 0f, alpha = alpha.Value };
                    _rtCam.targetTexture = _rt;
                }

                if (_rt.width != rtW || _rt.height != rtH)
                {
                    _rt.width = rtW;
                    _rt.height = rtH;
                }
            }

            // Update display sprite
            if (_rtCam)
            {
                displaySprite.SetPosition(Origin + _rtBounds.min - Vector2.one * 0.5f);
                buttonContainer.SetPosition(OffscreenOrigin - Vector2.one * 0.5f);
                buttonContainer.alpha = 1f;
            }
            else
            {
                buttonContainer.SetPosition(Origin - Vector2.one * 0.5f);
                buttonContainer.alpha = alpha.Value;
            }

            if (_rtCam)
            {
                _rtCam.transform.position = (Vector3)(OffscreenOrigin + _rtBounds.center + new Vector2(0.5f, 0.5f)) + Vector3.forward * -10f;
                _rtCam.orthographicSize = _rtBounds.height / 2f;
            }
        }
    }

    public class InputButton
    {
        public static float Size => Mathf.Floor(24f * Scale) * 2f;

        public InputGraphic parent;
        public Vector2 relPos;
        public Rect Bounds => new Rect(relPos, Vector2.one * Size);

        private FSprite _back;
        private FSprite _front;
        private FSprite _rtIndicator;
        private FLabel _key;
        private FSprite _keySprite;
        private Func<Inputs, bool> _inputGetter;

        public InputButton(InputGraphic parent, Vector2 pos, Layout.Key key)
        {
            this.parent = parent;
            _back = new FSprite("pixel") { anchorX = 0f, anchorY = 0f, scale = Size, color = backColor.Value };
            _front = new FSprite("pixel") { anchorX = 0f, anchorY = 0f, scale = Size - 2f };
            _rtIndicator = new FSprite("deerEyeB") { anchorX = 0f, anchorY = 0f };
            _inputGetter = key.inputGetter;
            relPos = pos;

            // Text
            if (!string.IsNullOrEmpty(key.text))
            {
                _key = new FLabel(Custom.GetFont(), key.text);
                if (key.abbreviate && Scale < 0.75f)
                {
                    _key.text = key.text.Substring(0, 1);
                }
            }
            
            // Sprite
            if (key.sprite != null)
            {
                _keySprite = new FSprite(key.sprite) { rotation = key.spriteAngle };
            }

            Move();
            AddToContainer();
        }

        public bool IsMouseOver
        {
            get
            {
                Vector2 mp = Input.mousePosition;
                mp.x -= Origin.x + relPos.x;
                mp.y -= Origin.y + relPos.y;
                if (mp.x < 0f || mp.y < 0f) return false;
                if (mp.x > Size || mp.y > Size) return false;
                return true;
            }
        }

        private void AddToContainer()
        {
            FContainer c = parent.buttonContainer;
            c.AddChild(_back);
            c.AddChild(_front);
            if (_key != null) c.AddChild(_key);
            if (_keySprite != null) c.AddChild(_keySprite);
            c.AddChild(_rtIndicator);
        }

        private void Move()
        {
            Vector2 pos = relPos + Vector2.one * 0.01f;
            _back.SetPosition(pos);
            _front.x = pos.x + 1f;
            _front.y = pos.y + 1f;
            float rtIndOffset = 1f + Mathf.Floor(5f * Mathf.Min(Scale, 1f));
            _rtIndicator.x = pos.x + rtIndOffset;
            _rtIndicator.y = pos.y + rtIndOffset;
            if (_key != null)
            {
                _key.x = pos.x + Size / 2f;
                _key.y = pos.y + Size / 2f;
            }
            if (_keySprite != null)
            {
                _keySprite.x = pos.x + Size / 2f;
                _keySprite.y = pos.y + Size / 2f;
            }
        }

        public void Update()
        {
            bool isDown = _inputGetter(parent.CurrentInput);
            bool rtIsDown = _inputGetter(parent.rtInput);

            _front.color = isDown ? onColor.Value : offColor.Value;
            _rtIndicator.color = rtIsDown ? onColor.Value : offColor.Value;
            _rtIndicator.isVisible = showRTIndicators.Value;
            if (_key != null) _key.color = outlineLabels.Value ? backColor.Value : (isDown ? offColor.Value : onColor.Value);
            if (_keySprite != null) _keySprite.color = outlineLabels.Value ? backColor.Value : (isDown ? offColor.Value : onColor.Value);
        }
    }

    public class InputAnalog
    {
        public static float Diameter => InputButton.Size;
        public static float Radius => InputButton.Size / 2f;

        public InputGraphic parent;
        public Vector2 relPos;
        public Rect Bounds => new Rect(relPos, Vector2.one * Diameter);

        private FSprite _analogBack;
        private FSprite _analogFront;
        private FSprite _analogIndicator;
        private FSprite _analogRTIndicator;

        public InputAnalog(InputGraphic parent, Vector2 pos, Layout.Analog analog)
        {
            this.parent = parent;
            relPos = pos;

            _analogBack = new FSprite("atlases/inputdisplay/analogcircle") { anchorX = 0f, anchorY = 0f, scale = Diameter / 256f, color = backColor.Value };
            _analogFront = new FSprite("atlases/inputdisplay/analogcircle") { anchorX = 0f, anchorY = 0f, scale = (Diameter - 2f) / 256f, color = offColor.Value };
            _analogIndicator = new FSprite("mouseEyeA1") { color = outlineLabels.Value ? backColor.Value : onColor.Value };
            _analogRTIndicator = new FSprite("mouseEyeA1") { color = outlineLabels.Value ? backColor.Value : onColor.Value, alpha = 0.5f };

            _analogBack.SetPosition(relPos);
            _analogFront.SetPosition(relPos + Vector2.one);

            FContainer c = parent.buttonContainer;
            c.AddChild(_analogBack);
            c.AddChild(_analogFront);
            c.AddChild(_analogIndicator);
            c.AddChild(_analogRTIndicator);
        }

        public bool IsMouseOver
        {
            get
            {
                Vector2 mp = Input.mousePosition;
                mp.x -= Origin.x + relPos.x + Radius;
                mp.y -= Origin.y + relPos.y + Radius;
                return mp.magnitude <= Radius;
            }
        }

        public void Update()
        {
            var center = relPos + Vector2.one * Radius;
            float maxOffset = Diameter * 0.5f - 4f;
            _analogIndicator.SetPosition(center + parent.CurrentInput.analogueDir * maxOffset);
            _analogRTIndicator.SetPosition(center + parent.rtInput.analogueDir * maxOffset);
        }
    }

    public class LerpBar
    {
        public InputGraphic parent;
        public Vector2 relPos;
        public Rect Bounds => new Rect(relPos, new Vector2(_lerpBarWidth, 2f));

        private FSprite _lerpBarBack;
        private FSprite _lerpBar;
        private float _lerpBarWidth;

        public LerpBar(InputGraphic parent, Vector2 pos, float width)
        {
            this.parent = parent;
            relPos = pos;

            _lerpBarWidth = width;
            _lerpBarBack = new FSprite("pixel") { anchorX = 0f, anchorY = 1f, scaleX = _lerpBarWidth, scaleY = 2f, color = offColor.Value };
            _lerpBar = new FSprite("pixel") { anchorX = 0f, anchorY = 1f, scaleX = _lerpBarWidth, scaleY = 2f, color = onColor.Value };

            _lerpBarBack.SetPosition(relPos);
            _lerpBar.SetPosition(relPos);

            FContainer c = parent.buttonContainer;
            c.AddChild(_lerpBarBack);
            c.AddChild(_lerpBar);
        }

        public void Update(float timeStacker)
        {
            _lerpBar.scaleX = timeStacker * _lerpBarWidth;
        }
    }
}

public enum LayoutMode
{
    Default,
    Classic,
    Custom
}
