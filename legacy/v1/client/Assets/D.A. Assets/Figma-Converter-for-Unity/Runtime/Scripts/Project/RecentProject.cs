using System;

namespace DA_Assets.FCU
{
    [Serializable]
    public struct RecentProject
    {
        public string Url { get; set; }
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
    }
}

