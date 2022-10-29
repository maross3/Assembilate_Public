using _Dev.VaporGame;

namespace Language
{
    public class Return : RuntimeError
    {
        internal object value;
        internal Return(object value) : base(value)
        {
            this.value = value;
        }
    }
}