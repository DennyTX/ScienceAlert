#if false
namespace ScienceAlert.Toolbar
{
    public class ClickInfo
    {
        public int button;
        public bool used;

        public bool Unused => !used;

        public ClickInfo()
        {
            button = 0;
            used = false;
        }

        public void Consume()
        {
            used = true;
        }
    }
}
#endif
