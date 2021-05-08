
namespace ScienceAlert.Experiments
{
    internal class SurfaceSampleObserver : EvaReportObserver
    {
        public SurfaceSampleObserver(StorageCache cache, ProfileData.ExperimentSettings settings, BiomeFilter filter,
            ScanInterface scanInterface)
            : base(cache, settings, filter, scanInterface, "surfaceSample")
        {
        }

        public override bool IsReadyOnboard
        {
            get
            {
                if (FlightGlobals.ActiveVessel == null) return false;
                if (FlightGlobals.ActiveVessel.isEVA)
                    return GetNextOnboardExperimentModule() != null;
                return Settings.Instance.CheckSurfaceSampleNotEva && base.IsReadyOnboard;
            }
        }

        public override bool UpdateStatus(ExperimentSituations experimentSituation, out bool newReport)
        {
            newReport = false;

            // Surface samples are not allowed until both astronaut complex and r&d facility reach level 1.
            if (ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) == 0 || 
                ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.ResearchAndDevelopment) == 0)
            {
                Available = false;
                lastAvailableId = "";
                return false;
            }

            return base.UpdateStatus(experimentSituation, out newReport);
        }
    }
}
