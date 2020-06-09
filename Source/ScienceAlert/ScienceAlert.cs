using System;
using System.Collections;
using System.Collections.Generic;
using ReeperCommon;
using ScienceAlert.Experiments;
using ScienceAlert.ProfileData;
using ScienceAlert.Windows;
using UnityEngine;
using KSP.UI.Screens;

using ToolbarControl_NS;

using System.Linq;

namespace ScienceAlert
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(ScienceAlert.MODID, ScienceAlert.MODNAME);
        }
    }



    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class ScienceAlert : MonoBehaviour
    {
        internal ToolbarControl toolbarControl;
        private ScanInterface scanInterface;
        public DraggableOptionsWindow optionsWindow;
        public static ScienceAlert Instance = null;
        internal ExcludeFilters excludeFilters;

        private Settings.ScanInterface scanInterfaceType;
        public event Callback OnScanInterfaceChanged = delegate { };
        public event Callback OnToolbarButtonChanged = delegate { };

        internal const string MODID = "ScienceAlert_NS";
        internal const string MODNAME = "Science Alert";

        private const string BlizzyNormalFlaskTexture = "ScienceAlert/PluginData/Textures/flask";
        private const string StockNormalFlaskTexture = "ScienceAlert/PluginData/Textures/flask-38";
        private int FrameCount = 100;
        private List<string> StarFlaskTextures = new List<string>();
        private List<string> StarFlaskTextures38 = new List<string>();

        void CreateButton()
        {
            if (toolbarControl == null)
            {
                Log.Write("ExperimentManager.CreateButton", Log.LEVEL.INFO);

                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(ButtonLeftClicked, ButtonLeftClicked,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                    MODID,
                    "saButton",
                    StockNormalFlaskTexture,
                    BlizzyNormalFlaskTexture,
                    "Left-click to view alert experiments; Right-click for settings"
                );
                toolbarControl.AddLeftRightClickCallbacks(null, ButtonRightClicked);
            }
        }

        private void SliceAtlasTexture()
        {
            Func<int, int, string> GetFrame = delegate (int frame, int desiredLen)
            {
                string str = frame.ToString();
                while (str.Length < desiredLen)
                    str = "0" + str;
                return str;
            };

            // load textures
            try
            {
                if (!GameDatabase.Instance.ExistsTexture(BlizzyNormalFlaskTexture))
                {
                    // load normal flask texture
                    Log.Debug("Loading normal flask texture");

                    Texture2D nflask = ResourceUtil.LoadImage("flask.png");
                    if (nflask == null)
                    {
                        Log.Error("Failed to create normal flask texture!");
                    }
                    else
                    {
#if GAMEDATABASE
                        GameDatabase.TextureInfo ti = new GameDatabase.TextureInfo(null, nflask, false, true, true);
                        ti.name = BlizzyNormalFlaskTexture;
                        GameDatabase.Instance.databaseTexture.Add(ti);
                        // Log.Debug("Created normal flask texture {0}", ti.name);
#endif
                    }
                    //
                    // Load textures for animation here
                    //
                    Texture2D sheet = ResourceUtil.LoadImage("sheet.png");
                    if (!ToolbarControl.LoadImageFromFile(ref sheet, "GameData/ScienceAlert/PluginData/Textures/sheet.png"))
                    {
                        Log.Error("Unable to ToolbarControl.LoadImageFromFile for sheet.png, falling back to ResourceUtil.LoadImage");
                    }


                    if (sheet == null)
                    {
                        Log.Error("Failed to create sprite sheet texture!");
                    }
                    else
                    {
                        var rt = RenderTexture.GetTemporary(sheet.width, sheet.height);
                        var oldRt = RenderTexture.active;
                        int invertHeight = ((FrameCount - 1) / (sheet.width / 24)) * 24;

                        Graphics.Blit(sheet, rt);
                        RenderTexture.active = rt;

                        for (int i = 0; i < FrameCount; ++i)
                        {
                            StarFlaskTextures.Add(BlizzyNormalFlaskTexture + GetFrame(i + 1, 4));
                            Texture2D sliced = new Texture2D(24, 24, TextureFormat.ARGB32, false);

                            sliced.ReadPixels(new Rect((i % (sheet.width / 24)) * 24, /*invertHeight -*/ (i / (sheet.width / 24)) * 24, 24, 24), 0, 0);
                            sliced.Apply();

                            GameDatabase.TextureInfo ti = new GameDatabase.TextureInfo(null, sliced, false, false, true);

                            ti.name = StarFlaskTextures.Last();
                            GameDatabase.Instance.databaseTexture.Add(ti);

                            // Log.Debug("Added sheet texture {0}", ti.name);
                        }

                        RenderTexture.active = oldRt;
                        RenderTexture.ReleaseTemporary(rt);
                    }

                    sheet = ResourceUtil.LoadImage("sheet-38.png");

                    if (!ToolbarControl.LoadImageFromFile(ref sheet, "GameData/ScienceAlert/PluginData/Textures/sheet-38.png"))
                    {
                        Log.Error("Unable to ToolbarControl.LoadImageFromFile for sheet-38, falling back to ResourceUtil.LoadImage");
                    }

                    if (sheet == null)
                    {
                        Log.Error("Failed to create sprite sheet texture!");
                    }
                    else
                    {
                        var rt = RenderTexture.GetTemporary(sheet.width, sheet.height);
                        var oldRt = RenderTexture.active;
                        int invertHeight = ((FrameCount - 1) / (sheet.width / 38)) * 38;

                        Graphics.Blit(sheet, rt);
                        RenderTexture.active = rt;

                        for (int i = 0; i < FrameCount; ++i)
                        {
                            StarFlaskTextures38.Add(BlizzyNormalFlaskTexture + "-38-" + GetFrame(i + 1, 4));
                            Texture2D sliced = new Texture2D(38, 38, TextureFormat.ARGB32, false);

                            sliced.ReadPixels(new Rect((i % (sheet.width / 38)) * 38, /*invertHeight -*/ (i / (sheet.width / 38)) * 38, 38, 38), 0, 0);
                            sliced.Apply();

                            GameDatabase.TextureInfo ti = new GameDatabase.TextureInfo(null, sliced, false, false, true);
                            ti.name = StarFlaskTextures38.Last();
                            ti.texture = sliced;
                            GameDatabase.Instance.databaseTexture.Add(ti);
                            // Log.Debug("Added sheet texture {0}", ti.name);

                        }

                        RenderTexture.active = oldRt;
                        RenderTexture.ReleaseTemporary(rt);
                    }
                    Log.Debug("Finished loading sprite sheet-38 textures.");
                }
                else
                { // textures already loaded
                    for (int i = 0; i < FrameCount; ++i)
                    {
                        StarFlaskTextures.Add(BlizzyNormalFlaskTexture + GetFrame(i + 1, 4));
                        StarFlaskTextures38.Add(BlizzyNormalFlaskTexture + "-38-" + GetFrame(i + 1, 4));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Failed to load textures: {0}", e);
            }
        }

        void ButtonLeftClicked()
        {
            WindowEventLogic.experimentList.Visible = !WindowEventLogic.experimentList.Visible;
            DraggableExperimentList.Instance.CheckForCollection();
        }

        void ButtonRightClicked()
        {
            WindowEventLogic.optionsWindow.Visible = !WindowEventLogic.optionsWindow.Visible;
        }

        IEnumerator animation;
        string TexturePath;
        public bool IsAnimating => animation != null;
        public bool IsLit => animation == null && TexturePath != BlizzyNormalFlaskTexture;
        private float FrameRate = 24f;
        private int CurrentFrame = 0;

        internal void PlayAnimation()
        {
            Log.Write("PlayAnimation", Log.LEVEL.INFO);
            if (animation == null) animation = DoAnimation();
            //StartCoroutine(DoAnimation());
        }
        /// <summary>
        /// Is called by Update whenever animation exists to
        /// update animation frame.
        ///
        /// Note: I didn't make this into an actual coroutine
        /// because StopCoroutine seems to sometimes throw
        /// exceptions
        /// </summary>
        /// <returns></returns>
        IEnumerator DoAnimation()
        {
            float elapsed = 0f;
            while (true)
            {
                while (elapsed < 1f / FrameRate)
                {
                    elapsed += Time.deltaTime;
                    yield return new WaitForSeconds(1f / FrameRate);
                }
                elapsed -= 1f / FrameRate;
                CurrentFrame = (CurrentFrame + 1) % FrameCount;
                TexturePath = StarFlaskTextures[CurrentFrame];
                toolbarControl.SetTexture(StarFlaskTextures38[CurrentFrame], StarFlaskTextures[CurrentFrame]);
            }
        }
        internal void StopAnimation()
        {
            Log.Write("StopAnimation", Log.LEVEL.INFO);
            animation = null;
            //StopCoroutine(DoAnimation());
        }
        /// <summary>
        /// Switch to normal flask texture
        /// </summary>
        public void SetUnlit()
        {
            //Log.Write("SetUnlit", Log.LEVEL.INFO);
            animation = null;
            TexturePath = BlizzyNormalFlaskTexture;
            toolbarControl.SetTexture(StockNormalFlaskTexture, BlizzyNormalFlaskTexture);
        }

        public void SetLit()
        {
            //Log.Write("SetLit", Log.LEVEL.INFO);
            animation = null;
            TexturePath = StarFlaskTextures[0];
            toolbarControl.SetTexture(StarFlaskTextures38[0], StarFlaskTextures[0]);
        }

        private void Update()
        {

            if (animation != null)
            {
                animation.MoveNext();
            }
        }

        public Settings.ScanInterface ScanInterfaceType
        {
            get
            {
                return scanInterfaceType;
            }
            set
            {
                if (value == scanInterfaceType && scanInterface != null) return;
                if (scanInterface != null)
                {
                    DestroyImmediate(GetComponent<ScanInterface>());
                }
                try
                {
                    switch (value)
                    {
                        case Settings.ScanInterface.None:
                            scanInterface = gameObject.AddComponent<DefaultScanInterface>();
                            break;
                        case Settings.ScanInterface.ScanSat:
                            if (!SCANsatInterface.IsAvailable())
                            {
                                ScanInterfaceType = Settings.ScanInterface.None;
                                return;
                            }
                            scanInterface = gameObject.AddComponent<SCANsatInterface>();
                            break;
                        default:
                            throw new NotImplementedException("Unrecognized interface type");
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("[ScienceAlert]:ScienceAlert.ScanInterfaceType failed with exception {0}", ex);
                    ScanInterfaceType = Settings.ScanInterface.None;
                    return;
                }
                scanInterfaceType = value;
                OnScanInterfaceChanged();
            }
        }

        void Start()
        {
            StartCoroutine(DoStart());
        }
        private IEnumerator DoStart()
        {
            while (ResearchAndDevelopment.Instance == null)
            {
                yield return 0;
            }
            while (FlightGlobals.ActiveVessel == null)
            {
                yield return 0;
            }
            while (!FlightGlobals.ready)
            {
                yield return 0;
            }
            Instance = this;

            while (ScienceAlertProfileManager.Instance == null || !ScienceAlertProfileManager.Instance.Ready)
            {
                yield return 0;
            }

            try
            {
                ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment("asteroidSample");
                if (experiment != null)
                {
                    experiment.experimentTitle = "Sample (Asteroid)";
                }
            }
            catch (KeyNotFoundException)
            {
                Destroy(this);
            }
             DMagicFactory.InitDMagicFactory();


            gameObject.AddComponent<AudioPlayer>().LoadSoundsFrom(ConfigUtil.GetDllDirectoryPath() + "/../sounds");
            gameObject.AddComponent<BiomeFilter>();
            gameObject.AddComponent<ExperimentManager>();
            gameObject.AddComponent<WindowEventLogic>();
            excludeFilters = new ExcludeFilters();
            ScanInterfaceType = Settings.Instance.ScanInterfaceType;

            SliceAtlasTexture();
            CreateButton();
        }

        public void OnDestroy()
        {
            if (toolbarControl != null)
            {
                toolbarControl.OnDestroy();
                Destroy(toolbarControl);
            }
            Settings.Instance.Save();
            Instance = null;
        }
    }
}
