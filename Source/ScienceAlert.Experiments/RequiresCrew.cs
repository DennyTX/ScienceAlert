using System.Collections.Generic;
using ReeperCommon;

namespace ScienceAlert.Experiments
{
    class RequiresCrew : ExperimentObserver
    {
        protected List<Part> crewableParts = new List<Part>();

        public RequiresCrew(StorageCache cache, ProfileData.ExperimentSettings settings, BiomeFilter filter, 
            ScanInterface scanInterface, string expid)
            : base(cache, settings, filter, scanInterface, expid)
        {
            requireControllable = false;
        }

        public override void Rescan()
        {
            base.Rescan();
            crewableParts.Clear();
            if (FlightGlobals.ActiveVessel == null) return;

            FlightGlobals.ActiveVessel.parts.ForEach(p =>
            {
                if (p.CrewCapacity > 0) crewableParts.Add(p);
            });

        }


        public override bool IsReadyOnboard
        {
            get
            {
                foreach (var crewable in crewableParts)
                    if (crewable.protoModuleCrew.Count > 0)
                        return true;
                return false;
            }
        }
    }
}
