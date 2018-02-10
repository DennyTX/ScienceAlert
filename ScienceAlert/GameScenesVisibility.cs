using ScienceAlert.Toolbar;

namespace ScienceAlert
{
	public class GameScenesVisibility : IVisibility
	{
		private object realGameScenesVisibility;

		private System.Reflection.PropertyInfo visibleProperty;

		public bool Visible => (bool)visibleProperty.GetValue(realGameScenesVisibility, null);

	    public GameScenesVisibility(params GameScenes[] gameScenes)
		{
			System.Type type = ToolbarTypes.getType("Toolbar.GameScenesVisibility");
			realGameScenesVisibility = System.Activator.CreateInstance(type, gameScenes);
			visibleProperty = ToolbarTypes.getProperty(type, "Visible");
		}
	}
}
