using ReeperCommon;
using UnityEngine;

namespace ScienceAlert.Experiments
{
    public class BiomeFilter : MonoBehaviour
    {
        public bool GetCurrentBiome(out string biome)
        {
            biome = "N/A";

            if (FlightGlobals.ActiveVessel == null) return false;

            string possibleBiome = string.Empty;

            if (GetBiome(FlightGlobals.ActiveVessel.latitude, FlightGlobals.ActiveVessel.longitude, out possibleBiome))
            {
                biome = possibleBiome;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool GetBiome(double lat, double lon, out string biome)
        {
            biome = string.Empty;
            var vessel = FlightGlobals.ActiveVessel;

            if (vessel == null || vessel.mainBody.BiomeMap == null || vessel.mainBody.BiomeMap.MapName == null || vessel.mainBody.BiomeMap.Attributes.Length == 0)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(vessel.landedAt))
            {
                biome = Vessel.GetLandedAtString(vessel.landedAt);
                return true;
            }

            biome = ScienceUtil.GetExperimentBiome(vessel.mainBody, lat, lon);

            return true;
        }
    }
}
