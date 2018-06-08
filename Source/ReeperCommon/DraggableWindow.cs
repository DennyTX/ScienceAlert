using UnityEngine;
using ClickThroughFix;

namespace ReeperCommon
{
    public delegate void WindowClosedDelegate();
    public delegate void WindowDelegate(bool tf);

    public abstract class DraggableWindow : MonoBehaviour
    {
        protected Rect windowRect = default(Rect);
        protected Rect lastRect = default(Rect);
        private GUISkin skin;
        private int winId = Random.Range(2444, 2147483647);
        private static Vector2 offset = new Vector2(4f, 4f);
        private static GUIStyle buttonStyle;
        private bool draggable = true;
        private bool visible = true;
        private static Texture2D hoverBackground;
        private static GUISkin defaultSkin;
        public event WindowDelegate OnVisibilityChange = delegate{};

        public event WindowDelegate OnDraggabilityChange = delegate{};

        public event WindowClosedDelegate OnClosed = delegate{};

        public bool Draggable
        {
            get
            {
                return draggable;
            }
            protected set
            {
                if (draggable != value)
                {
                    OnDraggabilityChange(value);
                }
                draggable = value;
            }
        }

        public bool ShrinkHeightToFit
        {
            get;
            set;
        }

        public bool Visible
        {
            get
            {
                return visible;
            }
            set
            {
                if (value != visible)
                {
                    OnVisibilityChange(value);
                }
                visible = value;
                if (gameObject.activeInHierarchy != visible && !visible)
                {
                    OnClosed();
                }
                gameObject.SetActive(visible);
            }
        }

        public int WindowID
        {
            get
            {
                return winId;
            }
            private set
            {
                winId = value;
            }
        }

        public string Title
        {
            get;
            set;
        }

        public GUISkin Skin
        {
            get
            {
                return skin ?? DefaultSkin;
            }
            set
            {
                skin = value ?? DefaultSkin;
            }
        }

        public Rect WindowRect
        {
            get
            {
                return lastRect;
            }
        }

        public bool ClampToScreen
        {
            get;
            set;
        }

        public static Texture2D LockTexture
        {
            get;
            set;
        }

        public static Texture2D UnlockTexture
        {
            get;
            set;
        }

        public static Texture2D CloseTexture
        {
            get;
            set;
        }

        public static Texture2D ButtonHoverBackground
        {
            get
            {
                return hoverBackground ?? ResourceUtil.GenerateRandom(16, 16);
            }
            set
            {
                hoverBackground = value;
                if (buttonStyle != null)
                {
                    buttonStyle.hover.background = value;
                }
            }
        }

        public static string ButtonSound
        {
            get;
            set;
        }

        public static GUISkin DefaultSkin
        {
            get
            {
                return defaultSkin ?? HighLogic.Skin;
            }
            set
            {
                defaultSkin = value;
            }
        }

        protected void Awake()
        {
            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUIStyle.none);
                if (hoverBackground != null)
                {
                    buttonStyle.hover.background = hoverBackground;
                }
            }
            Draggable = true;
            Visible = true;
            ClampToScreen = true;
            Title = "Draggable Window";
            windowRect = Setup();
            lastRect = new Rect(windowRect);
            GameEvents.onHideUI.Add(OnHideUI);
            GameEvents.onShowUI.Add(OnShowUI);
        }

        private void Start()
        {
            Log.Debug("ALERT:DraggableWindow {0} Start", Title);
        }

        protected virtual void OnDestroy()
        {
            Log.Debug("ALERT:DraggableWindow.OnDestroy");
            GameEvents.onHideUI.Remove(OnHideUI);
            GameEvents.onShowUI.Remove(OnShowUI);
        }

        protected void OnEnable()
        {
            OnVisibilityChange(true);
        }

        protected void OnDisable()
        {
            OnVisibilityChange(false);
        }

        public void Show(bool tf)
        {
            Visible = tf;
        }

        protected void Update()
        {
            if (ShrinkHeightToFit)
            {
                windowRect.height = 1f;
            }
        }

        protected void OnGUI()
        {
            GUI.skin = Skin;
            windowRect = ClickThruBlocker.GUILayoutWindow(winId, windowRect, _InternalDraw, Title);
            if (ClampToScreen)
                windowRect = KSPUtil.ClampRectToScreen(windowRect);
        }

        private void _InternalDraw(int winid)
        {
            DrawUI();
            lastRect.x = windowRect.x;
            lastRect.y = windowRect.y;
            GUILayout.BeginArea(new Rect(0f, offset.y, lastRect.width, lastRect.height));
            lastRect = new Rect(windowRect);
            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(false));
            GUILayout.FlexibleSpace();
            if (LockTexture != null && UnlockTexture != null)
            {
                if (GUILayout.Button(Draggable ? UnlockTexture : LockTexture, buttonStyle))
                {
                    Draggable = !Draggable;
                    if (!string.IsNullOrEmpty(ButtonSound))
                    {
                        AudioPlayer.Audio.PlayUI(ButtonSound);
                    }
                    Log.Debug("ALERT:DraggableWindow {0}", Draggable ? "unlocked" : "locked");
                }
                if (CloseTexture != null)
                {
                    GUILayout.Space(offset.x * 0.5f);
                }
            }
            if (CloseTexture != null && GUILayout.Button(CloseTexture, buttonStyle))
            {
                if (!string.IsNullOrEmpty(ButtonSound))
                {
                    AudioPlayer.Audio.PlayUI(ButtonSound);
                }
                OnCloseClick();
            }
            GUILayout.Space(offset.x);
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            if (Draggable)
            {
                GUI.DragWindow();
            }
        }

        protected abstract Rect Setup();

        protected abstract void DrawUI();

        protected abstract void OnCloseClick();

        private void OnHideUI()
        {
            gameObject.SetActive(false);
        }

        private void OnShowUI()
        {
            gameObject.SetActive(Visible);
        }

        public void SaveInto(ConfigNode node)
        {
            if (node != null)
            {
                node.Set("WindowX", windowRect.x);
                node.Set("WindowY", windowRect.y);
                node.Set("Draggable", Draggable);
                node.Set("Visible", Visible);
                Log.Debug("ALERT:DraggableWindow.SaveInto: Saved window {0} as ConfigNode {1}", Title, node.ToString());
                return;
            }
            Log.Warning("GuiUtil.DraggableWindow: Can't save into null ConfigNode");
        }

        public bool LoadFrom(ConfigNode node)
        {
            if (node != null)
            {
                windowRect.x = node.Parse("WindowX", (float)Screen.width * 0.5f - windowRect.width * 0.5f);
                windowRect.y = node.Parse("WindowY", (float)Screen.height * 0.5f - windowRect.height * 0.5f);
                Draggable = node.Parse("Draggable", true);
                Visible = node.Parse("Visible", false);
                return node.HasValue("WindowX") && node.HasValue("WindowY");
            }
            Log.Warning("GuiUtil.DraggableWindow: Can't load from null ConfigNode");
            return false;
        }
    }
}
