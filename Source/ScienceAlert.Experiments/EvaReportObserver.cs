using System.Collections.Generic;
using ReeperCommon;

namespace ScienceAlert.Experiments
{
    internal class EvaReportObserver : RequiresCrew
    {
        public EvaReportObserver(StorageCache cache, ProfileData.ExperimentSettings settings, BiomeFilter filter,
            ScanInterface scanInterface, string expid = "evaReport")
            : base(cache, settings, filter, scanInterface, expid){}

        public override bool Deploy()
        {
            if (!Available || !IsReadyOnboard) return false;
            if (FlightGlobals.ActiveVessel == null)return false;

            if (!FlightGlobals.ActiveVessel.isEVA)
            {
                if (FlightGlobals.getStaticPressure() > Settings.Instance.EvaAtmospherePressureWarnThreshold)
                    if (FlightGlobals.ActiveVessel.GetSrfVelocity().magnitude > Settings.Instance.EvaAtmosphereVelocityWarnThreshold)
                    {
                        DialogGUIBase[] options = new DialogGUIBase[2]
                        {
                            new DialogGUIButton("Science is worth a little risk", OnConfirmEva),
                            new DialogGUIButton("No, it would be a PR nightmare", null)
                        };

                        var multiOptionDialog = new MultiOptionDialog(
                            "It looks dangerous out there. Are you sure you want to send someone out? They might lose their grip!",
                            "It looks dangerous out there. Are you sure you want to send someone out? They might lose their grip!",
                            "Dangerous Condition Alert",
                            HighLogic.UISkin, options);
                        PopupDialog.SpawnPopupDialog(multiOptionDialog, false, HighLogic.UISkin);
                        return true;
                    }
                return ExpelCrewman();
            }

            var evas = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceExperiment>();
            for (int i = evas.Count - 1; i >= 0; i--)
            {
                ModuleScienceExperiment exp = evas[i];
                if (!exp.Deployed && exp.experimentID == experiment.id && !ExcludeFilters.IsExcluded(exp))
                {
                    exp.DeployExperiment();
                    break;
                }
            }
            return true;
        }

        protected void OnConfirmEva()
        {
            Log.Normal("EvaObserver: User confirmed eva despite conditions");
            Log.Normal("Expelling... {0}", ExpelCrewman() ? "success!" : "failed");
        }

        protected virtual bool ExpelCrewman()
        {
            List<ProtoCrewMember> crewChoices = new List<ProtoCrewMember>();

            //crewChoices.AddRange(crewableParts[i].protoModuleCrew);

            for (int i = crewableParts.Count - 1; i >= 0; i--)
            {
                for (int i1 = crewableParts[i].protoModuleCrew.Count - 1; i1 >= 0; i1--)
                {
                    if (crewableParts[i].protoModuleCrew[i1].type == ProtoCrewMember.KerbalType.Crew)
                        crewChoices.Add(crewableParts[i].protoModuleCrew[i1]);
                }
            }
            if (crewChoices.Count == 0) return false;
            if (MapView.MapIsEnabled) MapView.ExitMapView();

            if ((CameraManager.Instance.currentCameraMode & (CameraManager.CameraMode.Internal | CameraManager.CameraMode.IVA)) != 0)
                CameraManager.Instance.SetCameraFlight();

            var luckyKerbal = crewChoices[UnityEngine.Random.Range(0, crewChoices.Count - 1)];
            return FlightEVA.SpawnEVA(luckyKerbal.KerbalRef);
        }

        public override bool UpdateStatus(ExperimentSituations experimentSituation, out bool newReport)
        {
            newReport = false;

            // If the astronaut complex is level 0, EVA is only allowed when landed on the surface.
            if (ScenarioUpgradeableFacilities.GetFacilityLevel(SpaceCenterFacility.AstronautComplex) == 0 && experimentSituation != ExperimentSituations.SrfLanded)
            {
                Available = false;
                lastAvailableId = "";
                return false;
            }

            return base.UpdateStatus(experimentSituation, out newReport);
        }
    }
}
