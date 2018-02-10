using System.Linq;

namespace ScienceAlert.Toolbar
{
	internal class ToolbarTypes
	{
		internal readonly System.Type iToolbarManagerType;

		internal readonly System.Type functionVisibilityType;

		internal readonly System.Type functionDrawableType;

		internal readonly ButtonTypes button;

		internal ToolbarTypes()
		{
			iToolbarManagerType = getType("Toolbar.IToolbarManager");
			functionVisibilityType = getType("Toolbar.FunctionVisibility");
			functionDrawableType = getType("Toolbar.FunctionDrawable");
			System.Type type = getType("Toolbar.IButton");
			button = new ButtonTypes(type);
		}

		internal static System.Type getType(string name)
		{
			return AssemblyLoader.loadedAssemblies.SelectMany((AssemblyLoader.LoadedAssembly a) => a.assembly.GetExportedTypes()).SingleOrDefault((System.Type t) => t.FullName == name);
		}

		internal static System.Reflection.PropertyInfo getProperty(System.Type type, string name)
		{
			return type.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
		}

		internal static System.Reflection.PropertyInfo getStaticProperty(System.Type type, string name)
		{
			return type.GetProperty(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
		}

		internal static System.Reflection.EventInfo getEvent(System.Type type, string name)
		{
			return type.GetEvent(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
		}

		internal static System.Reflection.MethodInfo getMethod(System.Type type, string name)
		{
			return type.GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
		}
	}
}
