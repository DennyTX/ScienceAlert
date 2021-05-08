using UnityEngine;

namespace ScienceAlert
{
    public class DefaultScanInterface : ScanInterface
    {
    }

    public class ScanInterface : MonoBehaviour
    {
        public virtual bool HaveScanData(double lat, double lon, CelestialBody body)
        {
            return true;
        }
    }
}
