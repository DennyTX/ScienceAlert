using UnityEngine;

namespace ScienceAlert
{
    public class DefaultScanInterface : ScanInterface
    {
    }

    public class ScanInterface : MonoBehaviour
    {
        public virtual bool HaveScanData(double lat, double lon, CelestialBody body)
        {
            return true;
        }
    }
#if false
    public interface IVisibility
    {
        bool Visible
        {
            get;
        }
    }
#endif
#if false
    public interface IToolbarManager
    {
        IButton add(string ns, string id);
    }
#endif
#if false
    public interface IDrawable
    {
        void Update();

        Vector2 Draw(Vector2 position);
    }
#endif
#if false
    public interface IButton
    {
        event ClickHandler OnClick;
        event MouseEnterHandler OnMouseEnter;
        event MouseLeaveHandler OnMouseLeave;

        string Text {get;set;}

        Color TextColor {get;set;}

        string TexturePath {get;set;}

        string ToolTip {get;set;}

        bool Visible {get;set;}

        IVisibility Visibility {get;set;}

        bool EffectivelyVisible {get;}

        bool Enabled {get;set;}

        bool Important {get;set;}

        IDrawable Drawable {get;set;}

        void Destroy();
    }
#endif
}
