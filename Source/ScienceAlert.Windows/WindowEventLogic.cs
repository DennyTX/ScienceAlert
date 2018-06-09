using ReeperCommon;
using ScienceAlert.Experiments;

using UnityEngine;

namespace ScienceAlert.Windows
{
    /// <summary>
    /// This class controls which window(s) are shown when
    /// </summary>
    class WindowEventLogic : MonoBehaviour
    {
        internal static DraggableExperimentList experimentList;
        internal static DraggableOptionsWindow optionsWindow;
#if false
        internal static DraggableDebugWindow debugWindow;
#endif
        internal ScienceAlert scienceAlert;

        private void Awake()
        {
            Log.Normal("Customizing DraggableWindow");

            DraggableWindow.CloseTexture = ResourceUtil.LocateTexture("ScienceAlert.Resources.btnClose.png");
            DraggableWindow.LockTexture = ResourceUtil.LocateTexture("ScienceAlert.Resources.btnLock.png");
            DraggableWindow.UnlockTexture = ResourceUtil.LocateTexture("ScienceAlert.Resources.btnUnlock.png");
            DraggableWindow.ButtonHoverBackground =  ResourceUtil.LocateTexture("ScienceAlert.Resources.btnBackground.png");

            DraggableWindow.ButtonSound = "click1";

            scienceAlert = GetComponent<ScienceAlert>();

            optionsWindow = new GameObject("ScienceAlert.OptionsWindow").AddComponent<DraggableOptionsWindow>();
            optionsWindow.scienceAlert = GetComponent<ScienceAlert>();
            optionsWindow.manager = GetComponent<ExperimentManager>();
            experimentList = new GameObject("ScienceAlert.ExperimentList").AddComponent<DraggableExperimentList>();
            experimentList.biomeFilter = GetComponent<BiomeFilter>();
            experimentList.manager = GetComponent<ExperimentManager>();
#if false
            debugWindow = new GameObject("ScienceAlert.DebugWindow").AddComponent<DraggableDebugWindow>();
            debugWindow.Visible = false;
#endif
            optionsWindow.Visible = experimentList.Visible = false;
        }

        private void Start()
        {

            //scienceAlert.OnToolbarButtonChanged += OnToolbarChanged;
            scienceAlert.OnScanInterfaceChanged += OnInterfaceChanged;
#if false
            optionsWindow.OnVisibilityChange += OnWindowVisibilityChanged;
            experimentList.OnVisibilityChange += OnWindowVisibilityChanged;
            debugWindow.OnVisibilityChange += OnWindowVisibilityChanged;
#endif
            //OnToolbarChanged();
            OnInterfaceChanged();
        }

#if false
        private void OnToolbarChanged()
        {
            scienceAlert.Button.OnClick += OnToolbarClick;
        }
#endif
        private void OnInterfaceChanged()
        {
            experimentList.scanInterface = GetComponent<ScanInterface>();
        }

#if falsae
        private void OnToolbarClick(Toolbar.ClickInfo clickInfo)
        {
            if (optionsWindow.Visible || experimentList.Visible || debugWindow.Visible)
            {
                Log.Debug("WindowEventLogic: Hiding window(s)");
                optionsWindow.Visible = experimentList.Visible = debugWindow.Visible = false;
                AudioPlayer.Audio.PlayUI("click1", 0.5f);
            }
            else
            {
                switch (clickInfo.button)
                {
                    case 0: // left click, show experiment list
                        experimentList.Visible = true;
                        break;
                    case 1: // right click, show options window
                        optionsWindow.Visible = true;
                        break;
                    case 2: // middle click, show debug window (for AppLauncher this is alt + right click)
                        debugWindow.Visible = true;
                        break;
                }
                AudioPlayer.Audio.PlayUI("click1", 0.5f);
            }
        }

        private void OnWindowVisibilityChanged(bool tf)
        {
            //if (scienceAlert.ToolbarType == Settings.ToolbarInterface.ApplicationLauncher)
            //    if (tf)
            //    {
            //        GetComponent<Toolbar.AppLauncherInterface>().button.SetTrue(false);
            //    }
            //    else
            //    {
            //        if (!experimentList.Visible && !optionsWindow.Visible && !debugWindow.Visible)
            //            GetComponent<Toolbar.AppLauncherInterface>().button.SetFalse(false);
            //    }
        }
#endif
#if false
        private void Update()
        {
            var mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            DraggableWindow[] windows = new DraggableWindow[] { optionsWindow, experimentList, debugWindow };
        }
#endif
    }
}
