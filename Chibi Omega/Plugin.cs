using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState;

namespace Chibi_Omega
{

    public sealed class Plugin : IDalamudPlugin
    {

        public string Name => "Chibi Omega";

        private DalamudPluginInterface _pi { get; init; }
        private ObjectTable _ot { get; init; }
        private ChatGui _cg { get; init; }
        private ClientState _cs { get; init; }
        private bool _lookingForOmega = false;

        [PluginService]
        public static SigScanner TargetModuleScanner { get; private set; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] ChatGui chatGui
        )
        {
            _pi = pluginInterface;
            _ot = objectTable;
            _cg = chatGui;
            _cs = clientState;            
            _cs.TerritoryChanged += _cs_TerritoryChanged;
            _cs_TerritoryChanged(null, _cs.TerritoryType);
        }

        private void _cs_TerritoryChanged(object sender, ushort e)
        {
            if (e == 800 || e == 804 || e == 1122)
            {
                StartLooking();
            }
            else
            {
                StopLooking();
            }
        }

        public void Dispose()
        {
            StopLooking();
        }

        private void DrawUI()
        {
            ManipulateOmegaChan();
        }

        private void StartLooking()
        {
            lock (this)
            {
                if (_lookingForOmega == false)
                {
                    _pi.UiBuilder.Draw += DrawUI;
                    _lookingForOmega = true;
                }
            }
        }

        private void StopLooking()
        {
            lock (this)
            {
                if (_lookingForOmega == true)
                {
                    _pi.UiBuilder.Draw -= DrawUI;
                    _lookingForOmega = false;
                }
            }
        }

        private unsafe void ManipulateOmegaChan()
        {
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
