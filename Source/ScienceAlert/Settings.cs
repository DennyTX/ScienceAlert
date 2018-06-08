using System;
using System.IO;
using ReeperCommon;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScienceAlert
{
    public class Settings
    {
        public delegate void SaveCallback(ConfigNode node);

        public delegate void Callback();

        public enum WarpSetting
        {
            ByExperiment,
            GlobalOn,
            GlobalOff
        }

        public enum SoundNotifySetting
        {
            ByExperiment,
            Always,
            Never
        }

        public enum ScanInterface
        {
            None,
            ScanSat
        }

        public enum ToolbarInterface
        {
            //ApplicationLauncher,
            BlizzyToolbar
        }

        private static Settings instance;

        [DoNotSerialize]
        private readonly string ConfigPath = ConfigUtil.GetDllDirectoryPath() + "/../PluginData/settings.cfg";

        [DoNotSerialize]
        private GUISkin skin;

        public ConfigNode additional = new ConfigNode("config");

        [DoNotSerialize]
        private int windowOpacity = 255;

        [DoNotSerialize]
        protected ScanInterface Interface;

#if false
        [DoNotSerialize]
        protected ToolbarInterface ToolbarType;
#endif

        [DoNotSerialize]
        public event Callback OnSave = delegate {};

        [DoNotSerialize]
        public event SaveCallback OnLoad = delegate {};

        public static Settings Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = new Settings();
                return instance;
            }
        }

        public static GUISkin Skin => Instance.skin;

        public bool DebugMode {get;private set;}

        [Subsection("General")]
        public WarpSetting GlobalWarp {get;set;}

        public float TimeWarpCheckThreshold {get;private set;}

        [Subsection("General")]
        public SoundNotifySetting SoundNotification {get;set;}

        [Subsection("General")]
        public double EvaAtmospherePressureWarnThreshold {get;private set;}

        [Subsection("General")]
        public float EvaAtmosphereVelocityWarnThreshold {get; private set;}

        [Subsection("UserInterface")] public bool ShowReportValue {get;set;}

        [Subsection("UserInterface")] public bool DisplayCurrentBiome {get;set;}

        [Subsection("UserInterface")]
        public bool FlaskAnimationEnabled {get;set;}

        [Subsection("UserInterface")] public float StarFlaskFrameRate {get;private set;}

        public int WindowOpacity
        {
            get
            {
                return windowOpacity;
            }
            set
            {
                Texture2D background = skin.window.normal.background;
                windowOpacity = value;
                Color32[] pixels = background.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i].a = (byte)Mathf.Clamp(windowOpacity, 0, 255);
                }
                background.SetPixels32(pixels);
                background.Apply();
            }
        }

        public bool EvaReportOnTop {get;set;}

        [Subsection("CrewedVesselSettings")]
        public bool CheckSurfaceSampleNotEva {get;set;}

        public ScanInterface ScanInterfaceType
        {
            get
            {
                ScanInterface @interface = Interface;
                if (@interface != ScanInterface.ScanSat)
                {
                    return Interface;
                }
                if (SCANsatInterface.IsAvailable())
                {
                    return Interface;
                }
                return ScanInterface.None;
            }
            set
            {
                Interface = value;
            }
        }

#if false
        public ToolbarInterface ToolbarInterfaceType
        {
            get
            {
                ToolbarInterface toolbarType = ToolbarType;
                if (toolbarType != ToolbarInterface.BlizzyToolbar)
                {
                    return ToolbarType;
                }
                //if (!ToolbarManager.ToolbarAvailable)
                //{
                //    return Settings.ToolbarInterface.ApplicationLauncher;
                //}
                return ToolbarInterface.BlizzyToolbar;
            }
            set
            {
                ToolbarType = value;
            }
        }
#endif
        private Settings()
        {
            skin = Object.Instantiate(HighLogic.Skin);
            skin.button = new GUIStyle(skin.button);
            skin.button.fixedHeight = 24f;
            skin.button.padding = new RectOffset
            {
                left = 2,
                right = 2,
                top = 0,
                bottom = 0
            };
            skin.button.border = new RectOffset
            {
                left = 2,
                right = 2,
                top = 1,
                bottom = 1
            };
            skin.toggle.border.top = skin.toggle.border.bottom = skin.toggle.border.left = skin.toggle.border.right = 0;
            skin.toggle.margin = new RectOffset(5, 0, 0, 0);
            skin.toggle.padding = new RectOffset
            {
                left = 5,
                top = 3,
                right = 3,
                bottom = 3
            };
            skin.box.alignment = TextAnchor.MiddleCenter;
            skin.box.padding = new RectOffset(2, 2, 8, 5);
            skin.box.contentOffset = new Vector2(0f, 0f);
            skin.horizontalSlider.margin = new RectOffset();
            skin.window = new GUIStyle(skin.GetStyle("window"));
            skin.window.onActive.background = skin.window.onFocused.background = skin.window.onNormal.background = skin.window.onHover.background = skin.window.active.background = skin.window.focused.background = skin.window.hover.background = skin.window.normal.background = skin.window.normal.background.CreateReadable();
            WindowOpacity = 255;
            skin.window.onNormal.textColor = skin.window.normal.textColor = XKCDColors.Green_Yellow;
            skin.window.onHover.textColor = skin.window.hover.textColor = XKCDColors.YellowishOrange;
            skin.window.onFocused.textColor = skin.window.focused.textColor = Color.red;
            skin.window.onActive.textColor = skin.window.active.textColor = Color.blue;
            skin.window.fontSize = 12;
            EvaAtmospherePressureWarnThreshold = 0.00035;
            EvaAtmosphereVelocityWarnThreshold = 30f;
            ScanInterfaceType = ScanInterface.None;
            ShowReportValue = false;
            EvaReportOnTop = false;
            CheckSurfaceSampleNotEva = false;
            DisplayCurrentBiome = false;
            StarFlaskFrameRate = 24f;
            FlaskAnimationEnabled = true;
            TimeWarpCheckThreshold = 5f;
            DraggableWindow.DefaultSkin = skin;
            Load();
        }


        public void Load()
        {
            Log.Debug("[ScienceAlert]:Loading settings from {0}", ConfigPath);
            if (!File.Exists(ConfigPath))
            {
                Log.Debug("[ScienceAlert]:Failed to find settings file {0}", ConfigPath);
                Save();
                return;
            }
            ConfigNode configNode = ConfigNode.Load(ConfigPath);
            if (configNode == null)
            {
                Log.Debug("[ScienceAlert]:Failed to load {0}", ConfigPath);
                return;
            }
            configNode.CreateObjectFromConfigEx(this);
            Log.LoadFrom(configNode);
            OnLoad(additional);
        }

        public void Save()
        {
            ConfigNode configNode = null;
            try
            {
                OnSave();
                configNode = this.CreateConfigFromObjectEx() ?? new ConfigNode();
            }
            catch (Exception ex)
            {
                Log.Debug("[ScienceAlert]:Exception while creating ConfigNode from settings: {0}", ex);
            }
            Log.SaveInto(configNode);
            if (configNode.CountNodes <= 0 && configNode.CountValues <= 0) return;
            Log.Debug("[ScienceAlert]:Saving settings to {0}", ConfigPath);
            configNode.Save(ConfigPath);
        }
    }
}
