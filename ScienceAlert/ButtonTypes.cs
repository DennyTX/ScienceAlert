using System;
using System.Reflection;
using ScienceAlert.Toolbar;

namespace ScienceAlert
{
	internal class ButtonTypes
	{
		internal readonly Type iButtonType;
		internal readonly PropertyInfo textProperty;
		internal readonly PropertyInfo textColorProperty;
		internal readonly PropertyInfo texturePathProperty;
		internal readonly PropertyInfo toolTipProperty;
		internal readonly PropertyInfo visibleProperty;
		internal readonly PropertyInfo visibilityProperty;
		internal readonly PropertyInfo effectivelyVisibleProperty;
		internal readonly PropertyInfo enabledProperty;
		internal readonly PropertyInfo importantProperty;
		internal readonly PropertyInfo drawableProperty;
		internal readonly EventInfo onClickEvent;
		internal readonly EventInfo onMouseEnterEvent;
		internal readonly EventInfo onMouseLeaveEvent;
		internal readonly MethodInfo destroyMethod;

		internal ButtonTypes(System.Type iButtonType)
		{
			this.iButtonType = iButtonType;
			textProperty = ToolbarTypes.getProperty(iButtonType, "Text");
			textColorProperty = ToolbarTypes.getProperty(iButtonType, "TextColor");
			texturePathProperty = ToolbarTypes.getProperty(iButtonType, "TexturePath");
			toolTipProperty = ToolbarTypes.getProperty(iButtonType, "ToolTip");
			visibleProperty = ToolbarTypes.getProperty(iButtonType, "Visible");
			visibilityProperty = ToolbarTypes.getProperty(iButtonType, "Visibility");
			effectivelyVisibleProperty = ToolbarTypes.getProperty(iButtonType, "EffectivelyVisible");
			enabledProperty = ToolbarTypes.getProperty(iButtonType, "Enabled");
			importantProperty = ToolbarTypes.getProperty(iButtonType, "Important");
			drawableProperty = ToolbarTypes.getProperty(iButtonType, "Drawable");
			onClickEvent = ToolbarTypes.getEvent(iButtonType, "OnClick");
			onMouseEnterEvent = ToolbarTypes.getEvent(iButtonType, "OnMouseEnter");
			onMouseLeaveEvent = ToolbarTypes.getEvent(iButtonType, "OnMouseLeave");
			destroyMethod = ToolbarTypes.getMethod(iButtonType, "Destroy");
		}
	}
}
