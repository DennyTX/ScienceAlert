namespace ScienceAlert.Toolbar
{
    public delegate void ToolbarClickHandler(ClickInfo click);

    public interface IToolbar
	{
		event ToolbarClickHandler OnClick;

		IDrawable Drawable {get;set;}

		bool Important {get;set;}

		bool IsAnimating {get;}

		bool IsNormal {get;}

		bool IsLit {get;}

		void PlayAnimation();

		void StopAnimation();

		void SetUnlit();

		void SetLit();
	}
}
