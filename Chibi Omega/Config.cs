using Dalamud.Configuration;

namespace Chibi_Omega
{

    public class Config : IPluginConfiguration
    {

        public int Version { get; set; } = 0;

        public bool ApplyOnP1 { get; set; } = true;
        public bool ApplyOnP3 { get; set; } = true;

        public float ScaleP1 { get; set; } = 0.1f;
        public float ScaleP3 { get; set; } = 0.1f;

        public bool Opened { get; set; } = true;

    }

}
