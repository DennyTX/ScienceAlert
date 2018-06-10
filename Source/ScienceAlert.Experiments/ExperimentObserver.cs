using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ReeperCommon;
using ScienceAlert.ProfileData;

namespace ScienceAlert.Experiments
{
    using ScienceModuleList = List<ModuleScienceExperiment>;

    public class ExperimentObserver
    {
        private ScienceModuleList modules; // all ModuleScienceExperiments onboard that represent our experiment
        protected ScienceExperiment experiment; // The actual experiment that will be performed
        protected StorageCache storage; // Represents possible storage locations on the vessel
        public ExperimentSettings settings; // settings for this experiment
        protected string lastAvailableId; // Id of the last time the experiment was available
        protected string lastBiomeQuery; // the last good biome result we had

        protected BiomeFilter biomeFilter
            ; // Provides a little more accuracy when it comes to determining current biome (the original biome map has some filtering done on it)

        protected ScanInterface scanInterface; // Determines whether we're allowed to know if an experiment is available
        protected float nextReportValue; // take a guess
        protected bool requireControllable; // Vessel needs to be controllable for the experiment to be available

        // events
        public ExperimentManager.ExperimentAvailableDelegate OnAvailable = delegate { };

        public ExperimentObserver(StorageCache cache, ExperimentSettings expSettings, BiomeFilter filter,
            ScanInterface scanMapInterface, string expid)
        {
            settings = expSettings;
            biomeFilter = filter;
            requireControllable = true;

            if (scanMapInterface == null)
                scanMapInterface = new DefaultScanInterface();

            scanInterface = scanMapInterface;

            experiment = ResearchAndDevelopment.GetExperiment(expid);

            if (experiment == null)
                Log.Error("Failed to get experiment '{0}'", expid);

            storage = cache;
            Rescan();
        }

        ~ExperimentObserver()
        {

        }

        public virtual void Rescan()
        {
            modules = new ScienceModuleList();
            if (FlightGlobals.ActiveVessel == null) return;

            ScienceModuleList potentials = FlightGlobals.ActiveVessel
                .FindPartModulesImplementing<ModuleScienceExperiment>();

            foreach (var potential in potentials)
                if (potential.experimentID == experiment.id && !ExcludeFilters.IsExcluded(potential))
                    modules.Add(potential);
        }

        protected virtual float GetScienceTotal(ScienceSubject subject, out List<ScienceData> data)
        {
            if (subject == null)
            {
                data = new List<ScienceData>();
                return 0f;
            }

            var found = storage.FindStoredData(subject.id);
            data = found;

            if (found.Count == 0)
            {
                return subject.science;
            }
            float potentialScience = subject.science +
                                     ResearchAndDevelopment.GetScienceValue(data.First().dataAmount, subject) *
                                     HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;

            if (found.Count > 1)
            {
                float secondReport =
                    ResearchAndDevelopment.GetNextScienceValue(experiment.baseValue * experiment.dataScale, subject) *
                    HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
                potentialScience += secondReport;
                if (found.Count > 2)
                    for (int i = 3; i < found.Count; ++i)
                        potentialScience += secondReport / Mathf.Pow(4f, i - 2);
            }
            return potentialScience;
        }

        protected float GetBodyScienceValueMultipler(ExperimentSituations sit)
        {
            var b = FlightGlobals.currentMainBody;
            switch (sit)
            {
                case ExperimentSituations.FlyingHigh:
                    return b.scienceValues.FlyingHighDataValue;
                case ExperimentSituations.FlyingLow:
                    return b.scienceValues.FlyingLowDataValue;
                case ExperimentSituations.InSpaceHigh:
                    return b.scienceValues.InSpaceHighDataValue;
                case ExperimentSituations.InSpaceLow:
                    return b.scienceValues.InSpaceLowDataValue;
                case ExperimentSituations.SrfLanded:
                    return b.scienceValues.LandedDataValue;
                case ExperimentSituations.SrfSplashed:
                    return b.scienceValues.SplashedDataValue;
                default:
                    return 0f;
            }
        }

        protected float CalculateNextReportValue(ScienceSubject subject, ExperimentSituations situation,
            List<ScienceData> stored)
        {
            if (stored.Count == 0)
                return ResearchAndDevelopment.GetScienceValue(experiment.baseValue * experiment.dataScale, subject) *
                       HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;

            float experimentValue =
                ResearchAndDevelopment.GetNextScienceValue(experiment.baseValue * experiment.dataScale, subject) *
                HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;

            if (stored.Count == 1) return experimentValue;
            return experimentValue / Mathf.Pow(4f, stored.Count - 1);
        }

        public virtual bool UpdateStatus(ExperimentSituations experimentSituation, out bool newReport)
        {
            newReport = false;

            if (FlightGlobals.ActiveVessel == null)
            {
                Available = false;
                lastAvailableId = "";
                return false;
            }

            if (!settings.Enabled || (requireControllable && !FlightGlobals.ActiveVessel.IsControllable))
            {
                Available = false;
                lastAvailableId = "";
                return false;
            }

            bool lastStatus = Available;
            var vessel = FlightGlobals.ActiveVessel;

            if (!storage.IsBusy && IsReadyOnboard)
            {
                // does this experiment even apply in the current situation?
                if (experiment.IsAvailableWhile(experimentSituation, vessel.mainBody))
                {
                    var biome = string.Empty;
                    if (experiment.BiomeIsRelevantWhile(experimentSituation))
                    {
                        // biome matters; check to make sure we have biome data available
                        if (scanInterface.HaveScanData(vessel.latitude, vessel.longitude, vessel.mainBody))
                        {
                            if (biomeFilter.GetBiome(vessel.latitude * Mathf.Deg2Rad, vessel.longitude * Mathf.Deg2Rad,
                                out biome))
                            {
                                lastBiomeQuery = biome;
                            }
                            else
                            {
                                biome = lastBiomeQuery; // use last good known value
                            }
                        }
                        else
                        {
                            // no biome data available
                            Available = false;
                            lastAvailableId = "";
                            return false;
                        }
                    }

                    try
                    {
                        var subject = ResearchAndDevelopment.GetExperimentSubject(experiment, experimentSituation,
                            vessel.mainBody, biome, null);
                        List<ScienceData> data = null;
                        float scienceTotal = GetScienceTotal(subject, out data);

                        switch (settings.Filter)
                        {
                            case ExperimentSettings.FilterMethod.Unresearched:
                                // Fairly straightforward: total science + potential should be zero
                                Available = scienceTotal < 0.0005f;
                                break;

                            case ExperimentSettings.FilterMethod.NotMaxed:
                                // <98% of science cap
                                Available = scienceTotal < subject.scienceCap * 0.98f *
                                            HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
                                break;

                            case ExperimentSettings.FilterMethod.LessThanFiftyPercent:
                                Available = scienceTotal < subject.scienceCap * 0.5f *
                                            HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
                                break;

                            case ExperimentSettings.FilterMethod.LessThanNinetyPercent:
                                Available = scienceTotal < subject.scienceCap * 0.9f *
                                            HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
                                break;

                            default: // this should NEVER occur, but nice to have a safety measure
                                // in place if I add a filter option and forget to add its logic
                                Log.Error("Unrecognized experiment filter!");
                                data = new List<ScienceData>();
                                break;
                        }

                        nextReportValue = subject.CalculateNextReport(experiment, data);
                        Available = Available && nextReportValue > 0.01f;
                        Available = Available && nextReportValue >
                                    ScienceAlertProfileManager.ActiveProfile.ScienceThreshold;

                        if (Available)
                        {
                            if (lastAvailableId != subject.id)
                            {
                                lastStatus =
                                    false; // force a refresh, in case we're going from available -> available in different subject id
                                newReport = true; // we've available on a brand new report
                            }

                            lastAvailableId = subject.id;
                        }
                    }
                    catch (NullReferenceException e)
                    {
                        Log.Error(
                            "Failed to create {0} ScienceSubject. If you can manage to reproduce this error, let me know.",
                            experiment.id);
                        Log.Error("Exception was: {0}", e);
                        Available = lastStatus;
                    }
                }
                else
                {
                    Available = false;
                }
            }
            else Available = false; // no experiments ready

            return Available != lastStatus && Available;
        }

        public virtual bool Deploy()
        {
            if (!Available) return false;
            if (FlightGlobals.ActiveVessel == null) return false;
            if (requireControllable && !FlightGlobals.ActiveVessel.IsControllable) return false;

            var deployable = GetNextOnboardExperimentModule();

            if (!deployable) return false;

            try
            {
                deployable.GetType()
                    .InvokeMember("DeployExperiment",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreReturn |
                        System.Reflection.BindingFlags.InvokeMethod, null, deployable, null);
            }
            catch (Exception e)
            {
                Log.Error(
                    "Failed to invoke \"DeployExperiment\" using GetType(), falling back to base type after encountering exception {0}",
                    e);
                deployable.DeployExperiment();
            }
            return true;
        }



        #region Properties
        
        protected ModuleScienceExperiment GetNextOnboardExperimentModule()
        {
            foreach (var module in modules)
                if (!module.Deployed && !module.Inoperable)
                    return module;
            return null;
        }

        public virtual bool IsReadyOnboard => GetNextOnboardExperimentModule() != null;


        public virtual bool Available { get; protected set; }

        public string ExperimentTitle => experiment.experimentTitle;

        public virtual int OnboardExperimentCount => modules.Count;

        public bool SoundOnDiscovery => settings.SoundOnDiscovery;

        public bool AnimateOnDiscovery => settings.AnimationOnDiscovery;

        public bool StopWarpOnDiscovery => settings.StopWarpOnDiscovery;

        public float NextReportValue
        {
            get { return nextReportValue; }
            private set { nextReportValue = value; }
        }

        public ScienceExperiment Experiment => experiment;

        #endregion
    }
}
