using UnityEngine;

namespace ScienceAlert
{
	public static class Util
	{
		public static float CalculateNextReport(this ScienceSubject subject, ScienceExperiment experiment, System.Collections.Generic.List<ScienceData> onboard, float xmitScalar = 1f)
		{
			return GetNextReportValue(subject, experiment, onboard, xmitScalar);
		}

		public static float GetNextReportValue(ScienceSubject subject, ScienceExperiment experiment, System.Collections.Generic.List<ScienceData> onboard, float xmitScalar = 1f)
		{
			ScienceData scienceData = new ScienceData
                (experiment.baseValue * experiment.dataScale * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier,
                xmitScalar, 0f, subject.id, string.Empty);
            //scienceData.transmitBonus = ModuleScienceLab.GetBoostForVesselData(FlightGlobals.ActiveVessel, scienceData); ???
            xmitScalar += scienceData.transmitBonus;
			if (onboard.Count == 0)
			{
				return ResearchAndDevelopment.GetScienceValue(experiment.baseValue * experiment.dataScale, subject, xmitScalar) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
			}
			float num = ResearchAndDevelopment.GetNextScienceValue(experiment.baseValue * experiment.dataScale, subject, xmitScalar) * HighLogic.CurrentGame.Parameters.Career.ScienceGainMultiplier;
			if (onboard.Count == 1)
			{
				return num;
			}
			return num / Mathf.Pow(4f, (float)(onboard.Count - 1));
		}
	}
}
