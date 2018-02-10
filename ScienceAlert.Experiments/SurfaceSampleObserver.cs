
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
    }
}