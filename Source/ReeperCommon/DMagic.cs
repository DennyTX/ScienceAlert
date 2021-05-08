//
// This file copied from [x]science
//
// Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International Public License

using System;

namespace ReeperCommon
{
    /// <summary>
    /// Class to access the DMagic API via reflection so we don't have to recompile when the DMagic mod updates. If the DMagic API changes, we will need to modify this code.
    /// </summary>

    public class DMagicFactory
    {
        private static bool _dmagicIsInstalled = false;
        private static bool _dmagicSciAnimateGenIsInstalled = false;

        public static bool DMagic_IsInstalled { get { return _dmagicIsInstalled; } }
        public static bool DMagicScienceAnimateGeneric_IsInstalled { get { return _dmagicSciAnimateGenIsInstalled; } }


        static internal ScienceAlert.DMagicStuff DMStuff { get; private set; }
        static internal ScienceAlert.DMagic_SciAnimGenFactory DMM_SciAnimGenericStuff { get; private set; }


        static public void InitDMagicFactory()
        {
            _dmagicIsInstalled = false;

            if (HasMod("DMagic"))
            {
                _dmagicIsInstalled = true;
                doit_DMStuff();
            }
            if (HasMod("DMModuleScienceAnimateGeneric"))
            {
                _dmagicSciAnimateGenIsInstalled = true;
                doit_DMSciAnimGenStuff();
            }

        }

        static void doit_DMStuff()
        {
            DMStuff = new ScienceAlert.DMagicStuff();
        }
        private static bool HasMod(string modIdent)
        {
            foreach (AssemblyLoader.LoadedAssembly a in AssemblyLoader.loadedAssemblies)
            {
                if (modIdent == a.name)
                    return true;
            }
            return false;
        }

        internal static bool RunExperiment(string sid, ModuleScienceExperiment exp, bool runSingleUse = true)
        {
            return ScienceAlert.DMagicStuff.fetch.RunExperiment(sid, exp, runSingleUse);
        }
        static void doit_DMSciAnimGenStuff()
        {
            DMM_SciAnimGenericStuff = new ScienceAlert.DMagic_SciAnimGenFactory();
            if (DMM_SciAnimGenericStuff != null)
            {
                string ver = GetAssemblyInfo.GetVersionStringFromAssembly("DMModuleScienceAnimateGeneric");
                if (String.Compare(ver, "0.23") < 0)
                {
                    Log.Error("Old version of DMModuleScienceAnimateGeneric installed, disabling any references to that");
                    DMM_SciAnimGenericStuff = null;
                    _dmagicSciAnimateGenIsInstalled = false;
                }
                else
                    Log.Info("DMModuleScienceAnimateGeneric version: " + GetAssemblyInfo.GetVersionStringFromAssembly("DMModuleScienceAnimateGeneric"));
            }

        }


        internal static bool RunSciAnimGenExperiment(string sid, ModuleScienceExperiment exp, bool runSingleUse = true)
        {
              return ScienceAlert.DMagic_SciAnimGenFactory.fetch.RunExperiment(sid, exp, runSingleUse);
        }

        internal static Type getType(string name)
        {
            Type type = null;
#if false
            foreach (var s in AssemblyLoader.loadedAssemblies)
            {
                foreach (var s2 in s.assembly.GetTypes())
                {
                    if (s2.FullName == name)

                    {
                        type = s2;
                        return type;
                    }
                }
            }
            return null;
            //
            // The following is the original code, but was replaced because it was generating exceptions when 
            // the DMagic was loaded and the DMModuleScienceAnimationGeneric was not
            //
#else
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName != null && t.FullName == name)
                {
                    type = t;
                }
            });
            return type;
#endif
        }


    }
}