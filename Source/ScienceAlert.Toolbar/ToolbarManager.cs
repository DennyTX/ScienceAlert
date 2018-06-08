#if false
namespace ScienceAlert.Toolbar
{
    public class ToolbarManager : IToolbarManager
    {
        private static bool? toolbarAvailable;
        private static IToolbarManager instance_;
        private object realToolbarManager;
        private System.Reflection.MethodInfo addMethod;
        private System.Collections.Generic.Dictionary<object, IButton> buttons = new System.Collections.Generic.Dictionary<object, IButton>();
        private ToolbarTypes types = new ToolbarTypes();

        public static bool ToolbarAvailable
        {
            get
            {
                if (!toolbarAvailable.HasValue)
                    toolbarAvailable = Instance != null;
                return toolbarAvailable.Value;
            }
        }

        public static IToolbarManager Instance
        {
            get
            {
                if (toolbarAvailable == false || instance_ != null) return instance_;
                System.Type type = ToolbarTypes.getType("Toolbar.ToolbarManager");
                if (type == null) return instance_;
                object value = ToolbarTypes.getStaticProperty(type, "Instance").GetValue(null, null);
                instance_ = new ToolbarManager(value);
                return instance_;
            }
        }

        private ToolbarManager(object realToolbarManager)
        {
            this.realToolbarManager = realToolbarManager;
            addMethod = ToolbarTypes.getMethod(types.iToolbarManagerType, "add");
        }

        public IButton add(string ns, string id)
        {
            object obj = addMethod.Invoke(realToolbarManager, new object[]
            {
                ns,
                id
            });
            IButton button = new Button(obj, types);
            buttons.Add(obj, button);
            return button;
        }
    }
}
#endif
