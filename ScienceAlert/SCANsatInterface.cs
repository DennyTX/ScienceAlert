using ReeperCommon;
using System.Linq;

namespace ScienceAlert
{
	internal class SCANsatInterface : ScanInterface
	{
		private delegate bool IsCoveredDelegate(double lat, double lon, CelestialBody body, int mask);
        private const string SCANutilTypeName = "SCANsat.SCANUtil";
        private static IsCoveredDelegate _isCovered = (double lat, double lon, CelestialBody body, int mask) => true;
		private static System.Reflection.MethodInfo _method;
		private static bool _ran;

		private void OnDestroy()
		{
			_ran = false;
		}

		public override bool HaveScanData(double lat, double lon, CelestialBody body)
		{
			return _isCovered(lat, lon, body, 8);
		}

		public static bool IsAvailable()
		{
			if (_method != null && _isCovered != null) return true;
			if (_ran) return false;
			_ran = true;
			try
			{
				System.Type type = AssemblyLoader.loadedAssemblies.SelectMany((AssemblyLoader.LoadedAssembly loaded) => loaded.assembly.GetExportedTypes()).SingleOrDefault((System.Type t) => t.FullName == "SCANsat.SCANUtil");
				bool result;
				if (type == null)
				{
					result = false;
					return result;
				}
				_method = type.GetMethod("isCovered", new System.Type[]
				{
					typeof(double),
					typeof(double),
					typeof(CelestialBody),
					typeof(int)
				});
				if (_method == null)
				{
					result = false;
					return result;
				}
				_isCovered = (IsCoveredDelegate)System.Delegate.CreateDelegate(typeof(IsCoveredDelegate), _method);
			    Log.Debug(_isCovered == null
			        ? "[ScienceAlert]:SCANsatInterface: Failed to create method delegate"
			        : "[ScienceAlert]:SCANsatInterface: Interface available");
			    result = _isCovered != null;
				return result;
			}
			catch (System.Exception ex)
			{
				Log.Debug("[ScienceAlert]:Exception in SCANsatInterface.IsAvailable: {0}", ex);
			}
			return false;
		}
	}
}
