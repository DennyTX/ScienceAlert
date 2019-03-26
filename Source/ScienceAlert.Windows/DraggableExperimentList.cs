using System.Collections.Generic;
using System.Collections;
using System.Linq;
using ReeperCommon;
using ScienceAlert.Experiments;
using UnityEngine;

namespace ScienceAlert.Windows
{
    class DraggableExperimentList : DraggableWindow
    {
        internal static DraggableExperimentList Instance;
        private const string WindowTitle = "Available Experiments";

        public ExperimentManager manager;
        public BiomeFilter biomeFilter;
        public ScanInterface scanInterface;

        private bool adjustedSkin;

        protected override Rect Setup()
        {
            Instance = this;
            Title = "Available Experiments";
            ShrinkHeightToFit = true;
            Skin = Instantiate(Settings.Skin); // we'll be altering it a little bit to make sure the buttons are the right size
            Settings.Instance.OnSave += AboutToSave;
            LoadFrom(Settings.Instance.additional.GetNode("ExperimentWindow") ?? new ConfigNode());
            return new Rect(windowRect.x, windowRect.y, 256f, 128f);
        }

        private void AboutToSave()
        {
            SaveInto(Settings.Instance.additional.GetNode("ExperimentWindow") ?? Settings.Instance.additional.AddNode("ExperimentWindow"));
        }

        private void LateUpdate()
        {
            if (FlightGlobals.ActiveVessel != null)
                if (Settings.Instance.DisplayCurrentBiome)
                {
                    // if SCANsat is enabled, don't show biome names for unscanned areas
                    if (Settings.Instance.ScanInterfaceType == Settings.ScanInterface.ScanSat && scanInterface != null)
                    {
                        if (!scanInterface.HaveScanData(FlightGlobals.ActiveVessel.latitude, FlightGlobals.ActiveVessel.longitude, FlightGlobals.ActiveVessel.mainBody))
                        {
                            Title = "Data not found";
                            return;
                        }
                    }
                    Title = GetBiomeString();
                    return;
                }
            Title = WindowTitle; // default experiment window title
        }

        private string GetBiomeString()
        {
            string biome = Title;
            if (biomeFilter.GetCurrentBiome(out biome))
            {
                return biome;
            }
            return WindowTitle;
        }

        protected new void OnGUI()
        {
            if (!adjustedSkin)
            {
                Skin.window.stretchHeight = true;
                List<string> experimentTitles = new List<string>();
                ResearchAndDevelopment.GetExperimentIDs().ForEach(id => experimentTitles.Add(ResearchAndDevelopment.GetExperiment(id).experimentTitle));
                Skin.button.fixedWidth = Mathf.Max(64f, experimentTitles.Max(title =>
                {
                    float minWidth = 0f;
                    float maxWidth = 0f;
                    Skin.button.CalcMinMaxWidth(new GUIContent(title + " (123.4)"), out minWidth, out maxWidth);
                    return maxWidth;
                }));
                adjustedSkin = true;
            }
            base.OnGUI();
        }

        internal List<ModuleScienceContainer> msc;

        internal void CheckForCollection()
        {
            msc = new List<ModuleScienceContainer>();
            {
                var parts = FlightGlobals.ActiveVessel.Parts.FindAll(p => p.Modules.Contains("ModuleScienceContainer"));

                for (int i = parts.Count - 1; i > 0; i--)
                {
                    Part part = parts[i];
                    if (part.Modules["ModuleScienceContainer"].Events["CollectAllEvent"].guiActive)
                    {
                        var m = part.Modules["ModuleScienceContainer"] as ModuleScienceContainer;
                        msc.Add(m);
                    }
                }
            }
        }



        public int AnyAvailableScienceContainers()
        {
            int dataCnt = 0;
            if (FlightGlobals.ActiveVessel != null)
            {
                Vessel activeVessel = FlightGlobals.ActiveVessel;

                var parts = FlightGlobals.ActiveVessel.Parts.FindAll(p => p.Modules.Contains("ModuleScienceContainer"));

                for (int i = parts.Count - 1; i > 0; i--)
                {
                    var m = parts[i].Modules["ModuleScienceContainer"] as ModuleScienceContainer;
                    if (m.capacity == 0 || m.GetStoredDataCount() < m.capacity)
                        return 1;
                }
                return 0;
            }
            return dataCnt;
        }

        bool doAll = false;
        bool noEva = false;
        protected override void DrawUI()
        {
            GUILayout.BeginVertical();
            {
                var observers = manager.Observers;

                if (observers.All(eo => !eo.Available))
                {
                    GUILayout.Label("(no experiments available)");
                }
                else
                {
                    doAll = false;
                    if (GUILayout.Button("Deploy All", Settings.Skin.button))
                    {
                        doAll = true;
                        noEva = false;
                    }

                    if (GUILayout.Button("Deploy All except EVA", Settings.Skin.button, GUILayout.Height(35)))
                    {
                        doAll = true;
                        noEva = true;
                    }
                }
                if (AnyAvailableScienceContainers() > 0)
                {
                    if (msc != null && msc.Count > 0)
                    {

                        if (GUILayout.Button("Collect All", Settings.Skin.button))
                        {
                            foreach (var m in msc)
                            {
                                m.CollectAllEvent();
                            }
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        GUILayout.Button("Collect All (no science containers available)");
                        GUI.enabled = true;
                    }
                }

                //-----------------------------------------------------
                // Experiment list
                //-----------------------------------------------------

                for (int i = observers.Count() - 1; i >= 0; i--)
                {
                    if (observers[i].Available)
                    {
                        var content = new GUIContent(observers[i].ExperimentTitle);
                        color = "";
                        if (!observers[i].rerunnable) color = lblYellowColor;
                        if (!observers[i].resettable) color = lblRedColor;
                        if (Settings.Instance.ShowReportValue) content.text += $" ({observers[i].NextReportValue:0.#})";
                        if (color != "")
                            content.text = Colorized(color, content.text);
                        if (!doAll && !GUILayout.Button(content, Settings.Skin.button, GUILayout.ExpandHeight(false)))
                            continue;
                        if (doAll && noEva && observers[i].Experiment.id == "evaReport")
                            continue;

                        Log.Debug("Deploying {0}", observers[i].ExperimentTitle);
                        AudioPlayer.Audio.PlayUI("click2");
                        observers[i].Deploy();
                    }
                }            
            }

            GUILayout.EndVertical();
        }
        //string lblGreenColor = "00ff00";
        //string lblDrkGreenColor = "ff9d00";
        //string lblBlueColor = "3DB1FF";
        string lblYellowColor = "FFD966";
        string lblRedColor = "f90000";

        string color = "";
        string Colorized(string color, string txt)
        {
            return string.Format("<color=#{0}>{1}</color>", color, txt);
        }
        protected override void OnCloseClick()
        {
            Visible = false;
        }
    }
}
