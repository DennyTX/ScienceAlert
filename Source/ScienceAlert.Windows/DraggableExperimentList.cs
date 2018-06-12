using System.Collections.Generic;
using System.Linq;
using ReeperCommon;
using ScienceAlert.Experiments;
using UnityEngine;

namespace ScienceAlert.Windows
{
    class DraggableExperimentList : DraggableWindow
    {
        private const string WindowTitle = "Available Experiments";

        public ExperimentManager manager;
        public BiomeFilter biomeFilter;
        public ScanInterface scanInterface;

        private bool adjustedSkin;

        protected override Rect Setup()
        {
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
                    
                    //-----------------------------------------------------
                    // Experiment list
                    //-----------------------------------------------------

                    foreach (ExperimentObserver observer in observers)
                        if (observer.Available)
                        {
                            var content = new GUIContent(observer.ExperimentTitle);
                            if (Settings.Instance.ShowReportValue) content.text += $" ({observer.NextReportValue:0.#})";
                            if (!doAll && !GUILayout.Button(content, Settings.Skin.button, GUILayout.ExpandHeight(false)))
                                continue;
                            if (doAll && noEva && observer.Experiment.id == "evaReport")
                                continue;

                            Log.Debug("Deploying {0}", observer.ExperimentTitle);
                            AudioPlayer.Audio.PlayUI("click2");
                            observer.Deploy();
                        }

                }
            }
            GUILayout.EndVertical();
        }

        protected override void OnCloseClick()
        {
            Visible = false;
        }
    }
}
