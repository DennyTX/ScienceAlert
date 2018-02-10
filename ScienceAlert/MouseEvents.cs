namespace ScienceAlert
{
    public delegate void MouseEnterHandler(MouseEnterEvent e);
    public delegate void MouseLeaveHandler(MouseLeaveEvent e);
    public delegate void ClickHandler(ClickEvent e);

    public class ClickEvent : System.EventArgs
    {
        public readonly IButton Button;

        public readonly int MouseButton;

        internal ClickEvent(object realEvent, IButton button)
        {
            System.Type type = realEvent.GetType();
            Button = button;
            MouseButton = (int)type.GetField("MouseButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetValue(realEvent);
        }
    }

    public class MouseEnterEvent : MouseMoveEvent
    {
        internal MouseEnterEvent(IButton button) : base(button)
        {
        }
    }

    public class MouseLeaveEvent : MouseMoveEvent
    {
        internal MouseLeaveEvent(IButton button) : base(button)
        {
        }
    }

    public abstract class MouseMoveEvent : System.EventArgs
    {
        public readonly IButton button;

        internal MouseMoveEvent(IButton button)
        {
            this.button = button;
        }
    }
}

