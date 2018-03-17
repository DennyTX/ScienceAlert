using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using ReeperCommon;
using ScienceAlert.ProfileData;

namespace ScienceAlert.Experiments
{
    using ProfileManager = ScienceAlertProfileManager;
    using ExperimentObserverList = List<ExperimentObserver>;

    public class ExperimentManager : MonoBehaviour
    {
        // --------------------------------------------------------------------
        //    Members of ExperimentManager
        // --------------------------------------------------------------------
        private ScienceAlert scienceAlert;
        private StorageCache vesselStorage;
        private BiomeFilter biomeFilter;

        private System.Collections.IEnumerator watcher;

        ExperimentObserverList observers = new ExperimentObserverList();

        string lastGoodBiome = string.Empty; // if BiomeFilter tells us the biome it got is probably not real, then we can use

         AudioPlayer audio;

        // --------------------------------------------------------------------
        //    Events
        // --------------------------------------------------------------------
        public delegate void ExperimentAvailableDelegate(ScienceExperiment experiment, float reportValue); // todo
        public event ExperimentAvailableDelegate OnExperimentAvailable = delegate { }; // called whenever an experiment just became available in a new subject

        public event Callback OnObserversRebuilt = delegate { }; // called whenever observers are totally recreated from scratch,
        public event Callback OnExperimentsScanned = delegate { };   // called whenever the observers rescan the ship, typically

        void Awake()
        {
            vesselStorage = gameObject.AddComponent<StorageCache>();
            biomeFilter = GetComponent<BiomeFilter>();
            scienceAlert = gameObject.GetComponent<ScienceAlert>();
            audio = GetComponent<AudioPlayer>() ?? AudioPlayer.Audio;

            scienceAlert.OnScanInterfaceChanged += OnScanInterfaceChanged;
            scienceAlert.OnToolbarButtonChanged += OnToolbarButtonChanged;

            GameEvents.onVesselWasModified.Add(OnVesselWasModified);
            GameEvents.onVesselChange.Add(OnVesselChanged);
            GameEvents.onVesselDestroy.Add(OnVesselDestroyed);
        }

        void OnDestroy()
        {
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
            GameEvents.onVesselChange.Remove(OnVesselChanged);
            GameEvents.onVesselDestroy.Remove(OnVesselDestroyed);
        }

        public void Update()
        {
            if (FlightGlobals.ActiveVessel == null) return;
            if (vesselStorage.IsBusy || watcher == null) return;
            if (PauseMenu.isOpen) return;
            if (watcher != null) watcher.MoveNext();
        }

        public void OnVesselWasModified(Vessel vessel)
        {
            if (vessel != FlightGlobals.ActiveVessel) return;
            foreach (var obs in observers)
                obs.Rescan();
            OnExperimentsScanned();
        }

        public void OnVesselChanged(Vessel newVessel)
        {
            RebuildObserverList();
        }

        public void OnVesselDestroyed(Vessel vessel)
        {
            try
            {
                if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel != vessel) return;
                observers.Clear();
                watcher = null;
            }
            catch (Exception e)
            {
                Log.Error("Something has gone really wrong in ExperimentManager.OnVesselDestroyed: {0}", e);
                observers.Clear();
                watcher = null;
            }
        }

        #region Experiment functions

        private System.Collections.IEnumerator UpdateObservers()
        {
            while (true)
            {
                if (!FlightGlobals.ready || FlightGlobals.ActiveVessel == null)
                {
                    yield return 0;
                    continue;
                }
                var expSituation = ScienceUtil.GetExperimentSituation(FlightGlobals.ActiveVessel);

                foreach (var observer in observers)
                {
                    try
                    {
#if PROFILE
                    float start = Time.realtimeSinceStartup;
#endif
                        bool newReport = false;

                        // Is exciting new research available?
                        if (observer.UpdateStatus(expSituation, out newReport))
                        {
                            // if we're timewarping, resume normal time if that setting was used
                            if (observer.StopWarpOnDiscovery || Settings.Instance.GlobalWarp == Settings.WarpSetting.GlobalOn)
                                if (Settings.Instance.GlobalWarp != Settings.WarpSetting.GlobalOff)
                                    if (TimeWarp.CurrentRateIndex > 0)
                                    {
                                        OrbitSnapshot snap = new OrbitSnapshot(FlightGlobals.ActiveVessel.GetOrbitDriver().orbit);
                                        TimeWarp.SetRate(0, true);
                                        FlightGlobals.ActiveVessel.GetOrbitDriver().orbit = snap.Load();
                                        FlightGlobals.ActiveVessel.GetOrbitDriver().orbit.UpdateFromUT(Planetarium.GetUniversalTime());
                                    }

                            scienceAlert.Button.Important = true;

                            if (observer.settings.AnimationOnDiscovery)
                                scienceAlert.Button.PlayAnimation();
                            else if (scienceAlert.Button.IsNormal) scienceAlert.Button.SetLit();

                            switch (Settings.Instance.SoundNotification)
                            {
                                case Settings.SoundNotifySetting.ByExperiment:
                                    if (observer.settings.SoundOnDiscovery)
                                        audio.PlayUI("bubbles", 2f);
                                    break;
                                case Settings.SoundNotifySetting.Always:
                                    audio.PlayUI("bubbles", 2f);
                                    break;
                            }
                            OnExperimentAvailable(observer.Experiment, observer.NextReportValue);
                        }
                        else if (!observers.Any(ob => ob.Available))
                        {
                            scienceAlert.Button.SetUnlit();
                            scienceAlert.Button.Important = false;
                        }
#if PROFILE
                    Log.Warning("Tick time ({1}): {0} ms", (Time.realtimeSinceStartup - start) * 1000f, observer.ExperimentTitle);
#endif
                    }
                    catch (Exception e)
                    {
                        Log.Debug("ExperimentManager.UpdateObservers: exception {0}", e);
                    }

                    if (TimeWarp.CurrentRate < Settings.Instance.TimeWarpCheckThreshold)
                        yield return 0; // pause until next frame
                } // end observer loop
                yield return 0;
            } // end infinite while loop
        }

        public int RebuildObserverList()
        {
            observers.Clear();
            ScanInterface scanInterface = GetComponent<ScanInterface>();

            if (scanInterface == null)
                Log.Error("ExperimentManager.RebuildObserverList: No ScanInterface component found"); // this is bad; things won't break if the scan interface

            // construct the experiment observer list ...
            foreach (var expid in ResearchAndDevelopment.GetExperimentIDs())
                if (expid != "evaReport" && expid != "surfaceSample") // special cases

                    if (FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceExperiment>().Any(mse => mse.experimentID == expid))
                        observers.Add(new ExperimentObserver(vesselStorage, ProfileManager.ActiveProfile[expid], biomeFilter, scanInterface, expid));

            observers.Add(new SurfaceSampleObserver(vesselStorage, ProfileManager.ActiveProfile["surfaceSample"], biomeFilter, scanInterface));

            try
            {
                if (ProfileManager.ActiveProfile["evaReport"].Enabled)
                {
                    if (Settings.Instance.EvaReportOnTop)
                    {
                        observers = observers.OrderBy(obs => obs.ExperimentTitle).ToList();
                        observers.Insert(0, new EvaReportObserver(vesselStorage, ProfileManager.ActiveProfile["evaReport"], biomeFilter, scanInterface));
                    }
                    else
                    {
                        observers.Add(new EvaReportObserver(vesselStorage, ProfileManager.ActiveProfile["evaReport"], biomeFilter, scanInterface));
                        observers = observers.OrderBy(obs => obs.ExperimentTitle).ToList();
                    }
                }
                else observers = observers.OrderBy(obs => obs.ExperimentTitle).ToList();
            }
            catch (NullReferenceException e)
            {
                Log.Error("ExperimentManager.RebuildObserverList: Active profile does not seem to have an \"evaReport\" entry; {0}", e);
            }

            watcher = UpdateObservers(); // to prevent any problems by rebuilding in the middle of enumeration
            OnObserversRebuilt();

            return observers.Count;
        }

        #endregion

        #region Message handling functions

        private void OnScanInterfaceChanged()
        {
            RebuildObserverList();
        }

        private void OnToolbarButtonChanged()
        {
            RebuildObserverList();
        }

        #endregion

        public ReadOnlyCollection<ExperimentObserver> Observers => new ReadOnlyCollection<ExperimentObserver>(observers);
    }
}