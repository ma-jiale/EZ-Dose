using DA_Assets.FCU.Model;

namespace DA_Assets.FCU
{
    public struct FigmaSessionItem
    {
        public AuthType AuthType { get; set; }
        public AuthResult AuthResult { get; set; }
        public FigmaUser User { get; set; }
    }
}