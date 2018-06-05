using System;
using System.Collections.Generic;
using System.Threading;
using ReeperCommon;
using UnityEngine;

namespace ScienceAlert.Experiments
{
    public class BiomeFilter : MonoBehaviour
    {
        private const int HALF_SEARCH_DIMENSIONS = 2;    // box around the point on the biome map to

        private CelestialBody current;      // which CelestialBody we've got a cached biome map texture for
        private Texture2D projectedMap;     // this is the cleaned biome map of the current CelestialBody
        private const float COLOR_THRESHOLD = 0.005f;       // Maximum color difference for two colors to be considered the same
        private Queue<Action> actions = new Queue<Action>(); // Actions to be performed during Update
        private Thread worker; // Worker thread for ReprojectMap

        void Start()
        {
            GameEvents.onDominantBodyChange.Add(OnDominantBodyChanged);
            GameEvents.onVesselChange.Add(OnVesselChanged);
            ReprojectBiomeMap(FlightGlobals.currentMainBody);
        }

        void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(OnVesselChanged);
            GameEvents.onDominantBodyChange.Remove(OnDominantBodyChanged);
        }

        public void Update()
        {
            lock(actions)
            {
                while (actions.Count > 0)
                {
                    actions.Dequeue()();
                }
            }
        }

        public bool GetCurrentBiome(out string biome)
        {
            biome = "N/A";

            if (FlightGlobals.ActiveVessel == null) return false;

            string possibleBiome = string.Empty;

            if (GetBiome(FlightGlobals.ActiveVessel.latitude * Mathf.Deg2Rad, FlightGlobals.ActiveVessel.longitude * Mathf.Deg2Rad, out possibleBiome))
            {
                // the biome we got is most likely good
                biome = possibleBiome;
                return true;
            }
            // the biome we got is not very accurate (e.g. polar ice caps in middle of kerbin grasslands and
            // such, due to the way the biome map is filtered).
            biome = possibleBiome;
            return false;
        }

        public bool GetBiome(double latRad, double lonRad, out string biome)
        {
            biome = string.Empty;
            var vessel = FlightGlobals.ActiveVessel;

            if (vessel == null || vessel.mainBody.BiomeMap == null || vessel.mainBody.BiomeMap.MapName == null)
                return true;

            if (!string.IsNullOrEmpty(vessel.landedAt))
            {
                biome = Vessel.GetLandedAtString(vessel.landedAt);
                return true;
            }

            var possibleBiome = vessel.mainBody.BiomeMap.GetAtt(latRad, lonRad);

            if (!IsBusy)
            {
                if (!VerifyBiomeResult(latRad, lonRad, possibleBiome)) return false;
                biome = possibleBiome.name;
                return true;
            }

            biome = possibleBiome.name;
            return true;
        }

        private bool Similar(Color first, Color second)
        {
            return Mathf.Abs(first.r - second.r) < COLOR_THRESHOLD && Mathf.Abs(first.g - second.g) < COLOR_THRESHOLD && Mathf.Abs(first.b - second.b) < COLOR_THRESHOLD;
        }

        private bool VerifyBiomeResult(double lat, double lon, CBAttributeMapSO.MapAttribute target)
        {
            if (projectedMap == null) return true; // we'll have to assume it's accurate since we can't prove otherwise
            if (target == null || target.mapColor == null) return true; // this shouldn't happen

            lon -= Mathf.PI * 0.5f;
            if (lon < 0d) lon += Mathf.PI * 2d;
            lon %= Mathf.PI * 2d;

            int x_center = (int)Math.Round(projectedMap.width * (float)(lon / (Mathf.PI * 2)), 0);
            int y_center = (int)Math.Round(projectedMap.height * ((float)(lat / Mathf.PI) + 0.5f), 0);

            for (int y = y_center - HALF_SEARCH_DIMENSIONS; y < y_center + HALF_SEARCH_DIMENSIONS; ++y)
            for (int x = x_center - HALF_SEARCH_DIMENSIONS; x < x_center + HALF_SEARCH_DIMENSIONS; ++x)
            {
                Color c = projectedMap.GetPixel(x, y);
                if (Similar(c, target.mapColor))
                    return true; // we have a match, no need to look further
            }
            return false;
        }

        private void ReprojectBiomeMap(CelestialBody newBody)
        {
            ReprojectMap(newBody);
        }

        private void ReprojectMap(CelestialBody newBody)
        {
            if (current == newBody)
            {
                return;
            }

            if (newBody == null)
            {
                current = null;
                return;
            }

            current = null;

            if (newBody.BiomeMap == null || newBody.BiomeMap.MapName == null)
            {
                projectedMap = null;
                return;
            }

            Texture2D projection = new Texture2D(newBody.BiomeMap.Width, newBody.BiomeMap.Height, TextureFormat.ARGB32, false);
            projection.filterMode = FilterMode.Point;

            float timer = Time.realtimeSinceStartup;
            Color32[] pixels = projection.GetPixels32();

            if (worker != null)
            {
                worker.Abort();
            }

            var projectionWidth = projection.width;
            var projectionHeight = projection.height;

            worker = new Thread(() =>
            {
                for (int y = 0; y < projectionHeight; ++y)
                {
                    for (int x = 0; x < projectionWidth; ++x)
                    {
                        // convert x and y into uv coordinates
                        float u = (float)x / projectionWidth;
                        float v = (float)y / projectionHeight;

                        // convert uv coordinates into latitude and longitude
                        double lat = Math.PI * v - Math.PI * 0.5;
                        double lon = 2d * Math.PI * u + Math.PI * 0.5;

                        // set biome color in our clean texture
                        pixels[y * projectionWidth + x] = (Color32)newBody.BiomeMap.GetAtt(lat, lon).mapColor;
                    }
                }

                lock (actions)
                {
                    actions.Enqueue(() =>
                    {
                        projection.SetPixels32(pixels);
                        projection.Apply();

                        current = newBody;
                        projectedMap = projection;

                        worker = null;
                    });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void OnDominantBodyChanged(GameEvents.FromToAction<CelestialBody, CelestialBody> bodies)
        {
            ReprojectBiomeMap(bodies.to);
        }

        private void OnVesselChanged(Vessel v)
        {
            ReprojectBiomeMap(v.mainBody);
        }

        public bool IsBusy => worker != null;
    }
}