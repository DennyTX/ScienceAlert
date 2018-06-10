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
            debugWindow.OnVisibilityChange += OnWindowVisibilityChanged;
#endif
            //OnToolbarChanged();
            OnInterfaceChanged();
        }

        private void OnInterfaceChanged()
        {
            experimentList.scanInterface = GetComponent<ScanInterface>();
        }
    }
}
