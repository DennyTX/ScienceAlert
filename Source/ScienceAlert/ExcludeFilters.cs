using System;
using System.Collections;
using System.Collections.Generic;
using ReeperCommon;

using System.Linq;


namespace ScienceAlert
{
    class ExcludeFilters
    {

        internal static string[] excludedExperiments = null;
        internal static string[] excludedManufacturers = null;

        internal static bool IsExcluded(ModuleScienceExperiment exp)
        {
            bool b1 = excludedManufacturers.Contains(exp.part.partInfo.manufacturer);
            bool b2 = excludedExperiments.Contains(exp.experimentID);
            return b1 | b2;
        }

        public ExcludeFilters()
        {
            if (excludedExperiments == null)
            {
                List<string> expList = new List<string>();
                ConfigNode[] excludedNode = GameDatabase.Instance.GetConfigNodes("KEI_EXCLUDED_EXPERIMENTS");

                if (excludedNode != null)
                {
                    for (int i = excludedNode.Length - 1; i >= 0; i--)
                    {
                        string[] types = excludedNode[i].GetValues("experiment");
                        expList.AddRange(types);
                    }
                }
                else
                    Log.Error("Missing config file");

                excludedExperiments = expList.Distinct().ToArray();

#if DEBUG
                foreach (var s in excludedExperiments)
                    Log.Info("Excluded experiment: " + s);
#endif
            }

            if (excludedManufacturers == null)
            {
                List<string> expList = new List<string>();
                ConfigNode[] excludedNode = GameDatabase.Instance.GetConfigNodes("KEI_EXCLUDED_MANUFACTURERS");
                if (excludedNode != null)
                {
                    for (int i = excludedNode.Length - 1; i >= 0; i--)
                    {
                        string[] types = excludedNode[i].GetValues("manufacturer");
                        expList.AddRange(types);
                    }
                }
                else
                    Log.Error("Missing config file");

                excludedManufacturers = expList.Distinct().ToArray();
#if DEBUG
                foreach (var s in excludedManufacturers)
                    Log.Info("Excluded manufacturer: " + s);
#endif
            }

        }
    }
}
