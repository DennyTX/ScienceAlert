#if false
using System;
//using ScienceAlert.Toolbar;
using UnityEngine;

namespace ScienceAlert
{
    public class PopupMenuDrawable : IDrawable
    {
        private object realPopupMenuDrawable;

        private System.Reflection.MethodInfo updateMethod;

        private System.Reflection.MethodInfo drawMethod;

        private System.Reflection.MethodInfo addOptionMethod;

        private System.Reflection.MethodInfo addSeparatorMethod;

        private System.Reflection.MethodInfo destroyMethod;

        private System.Reflection.EventInfo onAnyOptionClickedEvent;

        public event Action OnAnyOptionClicked
        {
            add
            {
                onAnyOptionClickedEvent.AddEventHandler(realPopupMenuDrawable, value);
            }
            remove
            {
                onAnyOptionClickedEvent.RemoveEventHandler(realPopupMenuDrawable, value);
            }
        }

        public PopupMenuDrawable()
        {
            Type type = ToolbarTypes.getType("Toolbar.PopupMenuDrawable");
            realPopupMenuDrawable = Activator.CreateInstance(type, null);
            updateMethod = ToolbarTypes.getMethod(type, "Update");
            drawMethod = ToolbarTypes.getMethod(type, "Draw");
            addOptionMethod = ToolbarTypes.getMethod(type, "AddOption");
            addSeparatorMethod = ToolbarTypes.getMethod(type, "AddSeparator");
            destroyMethod = ToolbarTypes.getMethod(type, "Destroy");
            onAnyOptionClickedEvent = ToolbarTypes.getEvent(type, "OnAnyOptionClicked");
        }

        public void Update()
        {
            updateMethod.Invoke(realPopupMenuDrawable, null);
        }

        public Vector2 Draw(Vector2 position)
        {
            return (Vector2)drawMethod.Invoke(realPopupMenuDrawable, new object[]
            {
                position
            });
        }

        public IButton AddOption(string text)
        {
            object realButton = addOptionMethod.Invoke(realPopupMenuDrawable, new object[]
            {
                text
            });
            return new Button(realButton, new ToolbarTypes());
        }

        public void AddSeparator()
        {
            addSeparatorMethod.Invoke(realPopupMenuDrawable, null);
        }

        public void Destroy()
        {
            destroyMethod.Invoke(realPopupMenuDrawable, null);
        }
    }
}
#endif
