#if DEBUGWINDOW
using ReeperCommon;
using UnityEngine;

namespace ScienceAlert.Windows
{
    internal class DraggableDebugWindow : DraggableWindow
    {
        protected override Rect Setup()
        {
            Title = "Debug";
            Skin = Settings.Skin;
            Settings.Instance.OnSave += new Settings.Callback(AboutToSave);
            LoadFrom(Settings.Instance.additional.GetNode("DebugWindow") ?? new ConfigNode());
            Log.Debug("ALERT:DraggableDebugWindow.Setup");
            return new Rect(windowRect.x, windowRect.y, 256f, 128f);
        }

        private void AboutToSave()
        {
            Log.Debug("ALERT:DraggableDebugWindow.AboutToSave");
            SaveInto(Settings.Instance.additional.GetNode("DebugWindow") ?? Settings.Instance.additional.AddNode("DebugWindow"));
        }

        protected override void DrawUI()
        {
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true), GUILayout.MinHeight(128f));
            GUILayout.Label("Biome: to be implemented", GUILayout.MinWidth(256f));
            GUILayout.EndVertical();
        }

        protected override void OnCloseClick()
        {
            Visible = false;
        }
    }
}
#endif
