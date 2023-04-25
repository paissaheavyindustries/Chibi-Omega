using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using ImGuiNET;
using System.Threading.Tasks;
using System.Numerics;
using System.Diagnostics;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System;
using Dalamud.Logging;
using ImGuiScene;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using Dalamud.Data;

namespace Chibi_Omega
{

    public sealed class Plugin : IDalamudPlugin
    {

        public string Name => "Chibi Omega";

        private DalamudPluginInterface _pi { get; init; }
        private ObjectTable _ot { get; init; }
        private ChatGui _cg { get; init; }
        private ClientState _cs { get; init; }
        private CommandManager _cm { get; init; }
        private DataManager _dm { get; init; }
        private bool _lookingForOmega = false;
        internal Config _cfg = new Config();
        private float _adjusterX = 0.0f;
        private Dictionary<int, TextureWrap> _textures = new Dictionary<int, TextureWrap>();

        [PluginService]
        public static SigScanner TargetModuleScanner { get; private set; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] DataManager dataManager
        )
        {
            _pi = pluginInterface;
            _ot = objectTable;
            _cg = chatGui;
            _cs = clientState;
            _cm = commandManager;
            _dm = dataManager;
            LoadTextures();
            _pi.UiBuilder.Draw += DrawConfigUI;
            _pi.UiBuilder.OpenConfigUi += OpenConfigUI;
            _cm.AddHandler("/chibiomega", new CommandInfo(OnCommand)
            {
                HelpMessage = "Open Chibi Omega configuration"
            });
            _cfg = _pi.GetPluginConfig() as Config ?? new Config();
            _cs.TerritoryChanged += _cs_TerritoryChanged;
            _cs_TerritoryChanged(null, _cs.TerritoryType);
        }

        private void _cs_TerritoryChanged(object sender, ushort e)
        {
            if (e == 800 || e == 804 || e == 1122)
            {
                PluginLog.Debug("We're in correct zone, start looking");
                StartLooking();
            }
            else
            {
                PluginLog.Debug("Not in correct zone, stop looking");
                StopLooking();
            }
        }

        public void Dispose()
        {
            StopLooking();
            SaveConfig();
            _pi.UiBuilder.Draw -= DrawConfigUI;
            _pi.UiBuilder.OpenConfigUi -= OpenConfigUI;
            UnloadTextures();
        }

        private void LoadTextures()
        {
            _textures[1] = GetTexture(5);
        }

        private void UnloadTextures()
        {
            foreach (KeyValuePair<int, TextureWrap> kp in _textures)
            {
                if (kp.Value != null)
                {
                    kp.Value.Dispose();
                }
            }
            _textures.Clear();
        }

        internal TextureWrap? GetTexture(uint id)
        {
            return _dm.GetImGuiTextureIcon(id);
        }

        private void OnCommand(string command, string args)
        {
            OpenConfigUI();
        }

        public void SaveConfig()
        {
            PluginLog.Debug("Saving config");
            _pi.SavePluginConfig(_cfg);
        }

        private void LookForOmega()
        {
            ManipulateOmegaChan();
        }

        private void OpenConfigUI()
        {
            _cfg.Opened = true;
        }

        private void StartLooking()
        {
            lock (this)
            {
                if (_lookingForOmega == false)
                {
                    _pi.UiBuilder.Draw += LookForOmega;
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
                    _pi.UiBuilder.Draw -= LookForOmega;
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
                    if (
                        // alphascape version for testing
                        (bcs->ModelCharaId == 327 && (_cfg.ApplyOnP1 == true || _cfg.ApplyOnP3 == true))
                        ||
                        // p1 beetle
                        (bcs->ModelCharaId == 3771 && bcs->Health == 8557964 && _cfg.ApplyOnP1 == true)
                        ||
                        // p3 final omg
                        (bcs->ModelCharaId == 3775 && bcs->Health == 11125976 && _cfg.ApplyOnP3 == true)
                    )
                    {
                        GameObjectStruct *gos = (GameObjectStruct*)go.Address;
                        float scale;
                        if ((bcs->ModelCharaId == 327 && _cfg.ApplyOnP1 == false) || bcs->ModelCharaId == 3775)
                        {
                            scale = _cfg.ScaleP3;
                        }
                        else
                        {
                            scale = _cfg.ScaleP1;
                        }
                        bcs->ModelScale = scale;
                        gos->Scale = scale;
                        return;
                    }
                }
            }
        }

        private void DrawConfigUI()
        {
            if (_cfg.Opened == false)
            {
                return;
            }
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.496f, 0.058f, 0.323f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.496f, 0.058f, 0.323f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            ImGui.SetNextWindowSize(new Vector2(300, 500), ImGuiCond.FirstUseEver);
            bool open = true;
            if (ImGui.Begin(Name, ref open, ImGuiWindowFlags.NoCollapse) == false)
            {
                ImGui.End();
                ImGui.PopStyleColor(3);
                return;
            }
            if (open == false)
            {
                _cfg.Opened = false;
                ImGui.End();
                ImGui.PopStyleColor(3);
                return;
            }
            ImGuiStylePtr style = ImGui.GetStyle();
            Vector2 fsz = ImGui.GetContentRegionAvail();
            fsz.Y -= ImGui.GetTextLineHeight() + (style.ItemSpacing.Y * 2) + style.WindowPadding.Y;
            ImGui.BeginChild("ChibiOmgFrame", fsz);
            Vector2 cps = ImGui.GetCursorPos();
            ImGui.Image(_textures[1].ImGuiHandle, new Vector2(_textures[1].Width, _textures[1].Height));
            Vector2 cpsa = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(cps.X + _textures[1].Width + 10, cps.Y));
            ImGui.TextWrapped("Please be aware that changes are not applied in real time, they apply after zoning in or wiping.");
            Vector2 cps2 = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(cpsa.X, Math.Max(cpsa.Y, cps2.Y)));
            ImGui.Separator();
            bool applyP1 = _cfg.ApplyOnP1;
            if (ImGui.Checkbox("Apply to P1 Beetle Omega", ref applyP1) == true)
            {
                _cfg.ApplyOnP1 = applyP1;
            }
            if (applyP1 == false)
            {
                ImGui.BeginDisabled();
            }
            float scaleP1 = _cfg.ScaleP1;
            Vector2 sz = ImGui.GetContentRegionAvail();
            ImGui.PushItemWidth(sz.X);
            ImGui.Text(Environment.NewLine + "P1 scale:");            
            if (ImGui.SliderFloat("##P1 scale", ref scaleP1, 0.1f, 1.0f, ((int)Math.Floor(scaleP1 * 100.0f)).ToString() + " %%") == true)
            {
                _cfg.ScaleP1 = scaleP1;
            }
            if (applyP1 == false)
            {
                ImGui.EndDisabled();
            }
            ImGui.Separator();
            bool applyP3 = _cfg.ApplyOnP3;
            if (ImGui.Checkbox("Apply to P3 Final Omega", ref applyP3) == true)
            {
                _cfg.ApplyOnP3 = applyP3;
            }
            if (applyP3 == false)
            {
                ImGui.BeginDisabled();
            }
            float scaleP3 = _cfg.ScaleP3;
            ImGui.Text(Environment.NewLine + "P3 scale:");
            if (ImGui.SliderFloat("##P3 scale", ref scaleP3, 0.1f, 1.0f, ((int)Math.Floor(scaleP3 * 100.0f)).ToString() + " %%") == true)
            {
                _cfg.ScaleP3 = scaleP3;
            }
            if (applyP3 == false)
            {
                ImGui.EndDisabled();
            }
            ImGui.PopItemWidth();
            ImGui.EndChild();
            ImGui.Separator();
            Vector2 fp = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(_adjusterX, fp.Y));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.496f, 0.058f, 0.323f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.496f, 0.058f, 0.323f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            if (ImGui.Button("Discord") == true)
            {
                Task tx = new Task(() =>
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.FileName = @"https://discord.gg/6f9MY55";
                    p.Start();
                });
                tx.Start();
            }
            ImGui.SameLine();
            if (ImGui.Button("GitHub") == true)
            {
                Task tx = new Task(() =>
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.FileName = @"https://github.com/paissaheavyindustries/Chibi-Omega";
                    p.Start();
                });
                tx.Start();
            }
            ImGui.SameLine();
            _adjusterX += ImGui.GetContentRegionAvail().X;
            ImGui.PopStyleColor(3);
            ImGui.End();
            ImGui.PopStyleColor(3);
        }

    }
}
