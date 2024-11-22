using Dalamud.Plugin.Services;
using Dalamud.Plugin;
using Dalamud.IoC;

namespace Chibi_Omega
{

    internal class Service
    {

        [PluginService] public IDalamudPluginInterface pi { get; private set; }
        [PluginService] public IObjectTable ot { get; private set; }
        [PluginService] public IChatGui cg { get; private set; }
        [PluginService] public IClientState cs { get; private set; }
        [PluginService] public ICommandManager cm { get; private set; }
        [PluginService] public ITextureProvider tp { get; private set; }
        [PluginService] public IPluginLog lo { get; private set; }        

    }

}
