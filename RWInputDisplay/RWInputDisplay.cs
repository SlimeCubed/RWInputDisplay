using System;
using UnityEngine;
using BepInEx;
using System.Security;
using System.Security.Permissions;
using Inputs = Player.InputPackage;
using RWCustom;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning restore CS0618 // Type or member is obsolete

namespace RWInputDisplay
{
    [BepInPlugin(MOD_ID, "Input Display", "2.1.0")]
    public partial class RWInputDisplay : BaseUnityPlugin
    {
        public const string MOD_ID = "slime-cubed.inputdisplay";

        public static InputGraphic[] inputGraphics = new InputGraphic[1];
        public static Configurable<bool> enableInterpolation;
        public static Configurable<bool> showRTIndicators;
        public static Configurable<bool> outlineLabels;
        public static Configurable<bool> showTimeStacker;
        public static Configurable<bool> highPerformance;
        public static Configurable<float> alpha;
        public static Configurable<Color> backColor;
        public static Configurable<Color> onColor;
        public static Configurable<Color> offColor;
        public static Configurable<float> scale;
        public static float Scale => scale.Value * 2f;

        public static Vector2 origin = new Vector2(64f, 64f);

        private bool initialized;

        public void Awake()
        {
            On.RainWorld.OnModsInit += (orig, self) =>
            {
                orig(self);

                if (initialized) return;
                initialized = true;

                Futile.atlasManager.LoadImage("atlases/inputdisplay/analogcircle");
                MachineConnector.SetRegisteredOI(MOD_ID, new RWIDOptions());

                On.RoomCamera.ctor += RoomCamera_ctor;
                On.RoomCamera.ClearAllSprites += RoomCamera_ClearAllSprites;
                On.RainWorldGame.GrafUpdate += RainWorldGame_GrafUpdate;
            };
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
            InputGraphic ig = new InputGraphic(self);
            inputGraphics[cameraNumber] = ig;
            ig.Move();
        }

        public class InputGraphic
        {
            public RoomCamera cam;
            public InputButton[] buttons;
            public Inputs rtInput;
            
            public bool IsMouseOver
            {
                get
                {
                    float rad = _analogBoxSize * 0.5f;
                    if (rad * rad > Vector2.SqrMagnitude((Vector2)Input.mousePosition - (origin + _analogRelPos + Vector2.one * (_analogBoxSize * 0.5f)))) return true;
                    foreach (InputButton button in buttons) if (button.IsMouseOver) return true;
                    return false;
                }
            }
            private bool _dragging;
            private Vector2 _dragOffset;

            private Camera _rtCam;
            private Rect _rtBounds;
            private RenderTexture _rt;

            public FSprite displaySprite;
            public FContainer buttonContainer;

            private FSprite _lerpBarBack;
            private FSprite _lerpBar;
            private float _lerpBarWidth;

            private FSprite _analogBack;
            private FSprite _analogFront;
            private FSprite _analogIndicator;
            private FSprite _analogRTIndicator;
            private float _analogBoxSize;
            private Vector2 _analogRelPos;

            public Inputs CurrentInput
            {
                get
                {
                    if(cam.game.Players.Count > 0)
                        if (cam.game.Players[0].realizedCreature is Player ply) return ply.input[0];
                    return new Inputs();
                }
            }

            public InputGraphic(RoomCamera cam)
            {
                this.cam = cam;

                if (!highPerformance.Value)
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

            public void InitSprites()
            {
                float spacing = InputButton.Size + Mathf.Floor(InputButton.Size / 6f);
                buttons = new InputButton[]
                {
                    // Buttons
                    new InputButton(this, new Vector2(0f, 0f) * spacing, "Grab" , i => i.pckp   ),
                    new InputButton(this, new Vector2(0f, 1f) * spacing, "Throw", i => i.thrw   ),
                    new InputButton(this, new Vector2(1f, 1f) * spacing, "Jump" , i => i.jmp    ),
                    //new InputButton(this, new Vector2(3f, 1f) * spacing, "Map"  , i => i.mp     ),
                    // Axes
                    new InputButton(this, new Vector2(2f, 1f) * spacing, new FSprite("ShortcutArrow") { rotation =   0f }, i => i.y == 1 ),
                    new InputButton(this, new Vector2(2f, 0f) * spacing, new FSprite("ShortcutArrow") { rotation = 180f }, i => i.y == -1),
                    new InputButton(this, new Vector2(1f, 0f) * spacing, new FSprite("ShortcutArrow") { rotation = 270f }, i => i.x == -1),
                    new InputButton(this, new Vector2(3f, 0f) * spacing, new FSprite("ShortcutArrow") { rotation =  90f }, i => i.x == 1 ),
                };

                FContainer c = buttonContainer;

                // Analogue display
                _analogBoxSize = InputButton.Size;
                _analogRelPos = new Vector2(spacing * 3f + 0.5f, spacing + 0.5f);
                _analogBack = new FSprite("atlases/inputdisplay/analogcircle") { anchorX = 0f, anchorY = 0f, scale = _analogBoxSize / 256f, color = backColor.Value };
                _analogFront = new FSprite("atlases/inputdisplay/analogcircle") { anchorX = 0f, anchorY = 0f, scale = (_analogBoxSize - 2f) / 256f, color = offColor.Value };
                _analogIndicator = new FSprite("mouseEyeA1") { color = outlineLabels.Value ? backColor.Value : onColor.Value };
                _analogRTIndicator = new FSprite("mouseEyeA1") { color = outlineLabels.Value ? backColor.Value : onColor.Value, alpha = 0.5f };
                c.AddChild(_analogBack);
                c.AddChild(_analogFront);
                c.AddChild(_analogIndicator);
                c.AddChild(_analogRTIndicator);

                // timeStacker display
                _lerpBarWidth = spacing * 4f - 8f;
                _lerpBarBack = new FSprite("pixel") { anchorX = 0f, anchorY = 1f, scaleX = _lerpBarWidth, scaleY = 2f, color = offColor.Value };
                _lerpBar = new FSprite("pixel") { anchorX = 0f, anchorY = 1f, scaleX = _lerpBarWidth, scaleY = 2f, color = onColor.Value };
                _lerpBar.isVisible = showTimeStacker.Value;
                _lerpBarBack.isVisible = showTimeStacker.Value;
                c.AddChild(_lerpBarBack);
                c.AddChild(_lerpBar);

                _rtBounds = new Rect(-10f, -10f, spacing * 4f - 8f + 20f, spacing * 2f - 8f + 20f);

                Move();
            }

            public void Remove()
            {
                buttonContainer.RemoveFromContainer();

                if (_rtCam)
                {
                    Futile.atlasManager.UnloadAtlas("InputDisplay_" + cam.cameraNumber);
                    displaySprite.RemoveFromContainer();
                    Destroy(_rtCam.gameObject);
                }
            }

            public void Update(float timeStacker)
            {
                // Move the input display when left bracket is pressed
                if (Input.GetKey(KeyCode.LeftBracket))
                {
                    origin = Input.mousePosition;
                    Move();
                }

                // Allow dragging the input display
                if(_dragging)
                {
                    if (!Input.GetMouseButton(0))
                        _dragging = false;
                    else
                    {
                        origin = (Vector2)Input.mousePosition + _dragOffset;
                        Move();
                    }
                } else
                {
                    if(Input.GetMouseButtonDown(0) && IsMouseOver)
                    {
                        _dragging = true;
                        _dragOffset = origin - (Vector2)Input.mousePosition;
                    }
                }

                // Change the lerp bar to display the current timeStacker
                _lerpBar.scaleX = timeStacker * _lerpBarWidth;

                // Cache the inputs at the start of the frame. It is not going to change while the buttons are updating
                rtInput = RWInput.PlayerInput(0, cam.game.rainWorld);
                foreach (InputButton button in buttons)
                    button.Update();

                // Update the analog input
                Vector2 aiCenter = new Vector2(_analogRelPos.x + _analogBoxSize * 0.5f - 0.5f, _analogRelPos.y + _analogBoxSize * 0.5f - 0.5f);
                float maxOffset = _analogBoxSize * 0.5f - 4f;
                _analogIndicator.SetPosition(aiCenter + CurrentInput.analogueDir * maxOffset);
                _analogRTIndicator.SetPosition(aiCenter + rtInput.analogueDir * maxOffset);
                _analogRTIndicator.isVisible = showRTIndicators.Value;

                if (_rtCam)
                {
                    if (displaySprite.container != cam.ReturnFContainer("HUD2"))
                        cam.ReturnFContainer("HUD2").AddChild(displaySprite);
                    displaySprite.MoveToFront();
                }

                buttonContainer.MoveToFront();
            }

            private Vector2 OffscreenOrigin => new Vector2(-70000f, -70000f - cam.cameraNumber * 1000f);

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
                            Futile.atlasManager.UnloadAtlas("InputDisplay_" + cam.cameraNumber);
                            displaySprite?.RemoveFromContainer();
                        }

                        FAtlasElement element = Futile.atlasManager.LoadAtlasFromTexture("InputDisplay_" + cam.cameraNumber, _rt, false).elements[0];
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
                    displaySprite.SetPosition(origin + _rtBounds.min - Vector2.one * 0.5f);
                    buttonContainer.SetPosition(OffscreenOrigin - Vector2.one * 0.5f);
                    buttonContainer.alpha = 1f;
                }
                else
                {
                    buttonContainer.SetPosition(origin - Vector2.one * 0.5f);
                    buttonContainer.alpha = alpha.Value;
                }

                // Update components
                _lerpBarBack.SetPosition(0f, -8f);
                _lerpBar.SetPosition(0f, -8f);

                foreach (InputButton button in buttons)
                    button.Move(new Vector2(0f, 0f));

                _analogBack.SetPosition(_analogRelPos);
                _analogFront.SetPosition(_analogRelPos + Vector2.one);

                if (_rtCam)
                {
                    _rtCam.transform.position = (Vector3)(OffscreenOrigin + _rtBounds.center) + Vector3.forward * -10f;
                    _rtCam.orthographicSize = _rtBounds.height / 2f;
                }
            }
        }

        public class InputButton
        {
            public static float Size => Mathf.Floor(24f * Scale) * 2f;

            public InputGraphic parent;
            public Vector2 relPos;

            private FSprite _back;
            private FSprite _front;
            private FSprite _rtIndicator;
            private FLabel _key;
            private FSprite _keySprite;
            private Func<Inputs, bool> _inputGetter;

            private InputButton(InputGraphic parent, Vector2 pos, Func<Inputs, bool> inputGetter)
            {
                this.parent = parent;
                _back = new FSprite("pixel") { anchorX = 0f, anchorY = 0f, scale = Size, color = backColor.Value };
                _front = new FSprite("pixel") { anchorX = 0f, anchorY = 0f, scale = Size - 2f };
                _rtIndicator = new FSprite("deerEyeB") { anchorX = 0f, anchorY = 0f };
                _inputGetter = inputGetter;
                relPos = pos;
            }

            public InputButton(InputGraphic parent, Vector2 pos, string keyName, Func<Inputs, bool> inputGetter) : this(parent, pos, inputGetter)
            {
                _key = new FLabel(Custom.GetFont(), keyName);
                Move(Vector2.zero);
                AddToContainer();
                if (Scale < 0.75f)
                {
                    _key.text = keyName.Substring(0, 1);
                }
            }

            public InputButton(InputGraphic parent, Vector2 pos, FSprite keySprite, Func<Inputs, bool> inputGetter) : this(parent, pos, inputGetter)
            {
                _keySprite = keySprite;
                Move(Vector2.zero);
                AddToContainer();
            }

            public bool IsMouseOver
            {
                get
                {
                    Vector2 mp = Input.mousePosition;
                    mp.x -= origin.x + relPos.x;
                    mp.y -= origin.y + relPos.y;
                    if (mp.x < 0f || mp.y < 0f) return false;
                    if (mp.x > Size || mp.y > Size) return false;
                    return true;
                }
            }

            public void AddToContainer()
            {
                FContainer c = parent.buttonContainer;
                c.AddChild(_back);
                c.AddChild(_front);
                if(_key != null) c.AddChild(_key);
                if(_keySprite != null) c.AddChild(_keySprite);
                c.AddChild(_rtIndicator);
            }
            
            public void RemoveFromContainer()
            {
                _back.RemoveFromContainer();
                _front.RemoveFromContainer();
                _rtIndicator.RemoveFromContainer();
                _key?.RemoveFromContainer();
                _keySprite?.RemoveFromContainer();
            }

            public void Move(Vector2 origin)
            {
                Vector2 pos = origin + relPos + Vector2.one * 0.01f;
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
    }
}
