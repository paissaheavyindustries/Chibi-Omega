using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects.Types;
using Character = Dalamud.Game.ClientState.Objects.Types.ICharacter;
using CharacterStruct = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using GameObjectStruct = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
using Dalamud.Game.Command;
using ImGuiNET;
using System.Threading.Tasks;
using System.Numerics;
using System.Diagnostics;
using System;
using Dalamud.Logging;
using ImGuiScene;
using System.Collections.Generic;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;

namespace Chibi_Omega
{

    public sealed class Plugin : IDalamudPlugin
    {

        public string Name => "Chibi Omega";

        private IDalamudPluginInterface _pi { get; init; }
        private IObjectTable _ot { get; init; }
        private IChatGui _cg { get; init; }
        private IClientState _cs { get; init; }
        private ICommandManager _cm { get; init; }
        private ITextureProvider _tp { get; init; }
        private IPluginLog _lo { get; init; }
        private bool _lookingForOmega = false;
        internal Config _cfg = new Config();
        private float _adjusterX = 0.0f;
        private Dictionary<int, ISharedImmediateTexture> _textures = new Dictionary<int, ISharedImmediateTexture>();

        public Plugin(
            IDalamudPluginInterface pluginInterface,
            IObjectTable objectTable,
            IClientState clientState,
            IChatGui chatGui,
            ICommandManager commandManager,
            ITextureProvider textureProvider,
            IPluginLog log
        )
        {
            _pi = pluginInterface;
            _ot = objectTable;
            _cg = chatGui;
            _cs = clientState;
            _cm = commandManager;
            _tp = textureProvider;
            _lo = log;
            LoadTextures();
            _pi.UiBuilder.Draw += DrawConfigUI;
            _pi.UiBuilder.OpenMainUi += OpenConfigUI;
            _pi.UiBuilder.OpenConfigUi += OpenConfigUI;
            _cm.AddHandler("/chibiomega", new CommandInfo(OnCommand)
            {
                HelpMessage = "Open Chibi Omega configuration"
            });
            _cfg = _pi.GetPluginConfig() as Config ?? new Config();
            _cs.TerritoryChanged += _cs_TerritoryChanged;
            _cs_TerritoryChanged(_cs.TerritoryType);
        }

        private void _cs_TerritoryChanged(ushort e)
        {
            if (e == 800 || e == 804 || e == 1122)
            {
                _lo.Debug("We're in correct zone, start looking");
                StartLooking();
            }
            else
            {
                _lo.Debug("Not in correct zone, stop looking");
                StopLooking();
            }
        }

        public void Dispose()
        {
            StopLooking();
            SaveConfig();
            _pi.UiBuilder.Draw -= DrawConfigUI;
            _pi.UiBuilder.OpenConfigUi -= OpenConfigUI;
            _pi.UiBuilder.OpenMainUi -= OpenConfigUI;
            UnloadTextures();
        }

        private void LoadTextures()
        {
            _textures[1] = GetTexture(5);
        }

        private void UnloadTextures()
        {
            _textures.Clear();
        }

        internal ISharedImmediateTexture GetTexture(uint id)
        {
            return _tp.GetFromGameIcon(new GameIconLookup() { IconId = id });
        }

        private void OnCommand(string command, string args)
        {
            OpenConfigUI();
        }

        public void SaveConfig()
        {
            _lo.Debug("Saving config");
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
            foreach (IGameObject go in _ot)
            {
                if (go is Character)
                {
                    Character bc = (Character)go;                    
                    CharacterStruct* bcs = (CharacterStruct*)bc.Address;
                    CharacterData cd = bcs->CharacterData;
                    if (
                        // alphascape version for testing
                        (cd.ModelCharaId == 327 && (_cfg.ApplyOnP1 == true || _cfg.ApplyOnP3 == true))
                        ||
                        // p1 beetle
                        (cd.ModelCharaId == 3771 && cd.Health == 8557964 && _cfg.ApplyOnP1 == true)
                        ||
                        // p3 final omg
                        (cd.ModelCharaId == 3775 && cd.Health == 11125976 && _cfg.ApplyOnP3 == true)
                    )
                    {
                        GameObjectStruct *gos = (GameObjectStruct*)go.Address;
                        float scale;
                        if ((cd.ModelCharaId == 327 && _cfg.ApplyOnP1 == false) || cd.ModelCharaId == 3775)
                        {
                            scale = _cfg.ScaleP3;
                        }
                        else
                        {
                            scale = _cfg.ScaleP1;
                        }
                        cd.ModelScale = scale;
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
            IDalamudTextureWrap tx = _textures[1].GetWrapOrEmpty();
            ImGui.Image(tx.ImGuiHandle, new Vector2(tx.Width / 2, tx.Height / 2));
            Vector2 cpsa = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(cps.X + (tx.Width / 2) + 10, cps.Y));
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
                Task tsk = new Task(() =>
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.FileName = @"https://discord.gg/6f9MY55";
                    p.Start();
                });
                tsk.Start();
            }
            ImGui.SameLine();
            if (ImGui.Button("GitHub") == true)
            {
                Task tsk = new Task(() =>
                {
                    Process p = new Process();
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.FileName = @"https://github.com/paissaheavyindustries/Chibi-Omega";
                    p.Start();
                });
                tsk.Start();
            }
            ImGui.SameLine();
            _adjusterX += ImGui.GetContentRegionAvail().X;
            ImGui.PopStyleColor(3);
            ImGui.End();
            ImGui.PopStyleColor(3);
        }

    }
}
