using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using Dalamud.Game.Gui;
using System.Xml.Serialization;
using System;

namespace Chibi_Omega
{

    public sealed class Plugin : IDalamudPlugin
    {

        public string Name => "Chibi Omega";

        private DalamudPluginInterface _pi { get; init; }
        private ObjectTable _ot { get; init; }
        private ChatGui _cg { get; init; }
        private DateTime nextCheck = DateTime.Now.AddSeconds(1);

        [PluginService]
        public static SigScanner TargetModuleScanner { get; private set; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] ChatGui chatGui
        )
        {
            _pi = pluginInterface;
            _ot = objectTable;
            _cg = chatGui;
            _pi.UiBuilder.Draw += DrawUI;
        }

        public void Dispose()
        {
            _pi.UiBuilder.Draw -= DrawUI;
        }

        private void DrawUI()
        {
            ManipulateOmegaChan();
        }

        private unsafe void ManipulateOmegaChan()
        {
            if (DateTime.Now < nextCheck)
            {
                return;
            }
            nextCheck = DateTime.Now.AddSeconds(1);
            foreach (GameObject go in _ot)
            {
                if (go is Character)
                {
                    Character bc = (Character)go;
                    CharacterStruct* bcs = (CharacterStruct*)bc.Address;
                    if (bcs->ModelCharaId == 327)
                    {
                        GameObjectStruct *gos = (GameObjectStruct*)go.Address;
                        bcs->ModelScale = 0.1f;
                        gos->Scale = 0.1f;
                        return;
                    }
                }
            }
        }

    }
}
