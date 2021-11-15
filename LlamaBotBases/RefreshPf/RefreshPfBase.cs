﻿using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using GreyMagic;
using LlamaLibrary.Logging;
using TreeSharp;

namespace LlamaBotBases.RefreshPf
{
    public class RefreshPfBase : BotBase
    {
        private static readonly LLogger Log = new LLogger(typeof(RefreshPfBase).Name, Colors.Pink);

        private const int Offset0 = 0x1CA;
        private const int Offset2 = 0x160;
        private Composite _root;
        private int agentId = 0;

        public RefreshPfBase()
        {
        }

        public override string Name => "PF Refresh";
        public override PulseFlags PulseFlags => PulseFlags.Chat | PulseFlags.Party | PulseFlags.Windows | PulseFlags.GameEvents;

        public override bool IsAutonomous => true;
        public override bool RequiresProfile => false;

        public override Composite Root => _root;

        public override bool WantButton { get; } = false;

        public AtkAddonControl PfWindow => RaptureAtkUnitManager.GetWindowByName("LookingForGroup");
        public AtkAddonControl PfConditionWindow => RaptureAtkUnitManager.GetWindowByName("LookingForGroupCondition");

        public override void Start()
        {
            agentId = GetAgent();
            _root = new ActionRunCoroutine(r => Run());
        }

        private async Task<bool> Run()
        {
            switch (Core.Me.Icon)
            {
                case PlayerIcon.Recruiting_Party_Members:
                    await Coroutine.Sleep(1000);
                    break;
                case PlayerIcon.Trial_Adventurer:
                case PlayerIcon.New_Adventurer:
                case PlayerIcon.None:
                {
                    if (PfWindow == null)
                    {
                        AgentModule.ToggleAgentInterfaceById(agentId);
                        await Coroutine.Wait(5000, () => PfWindow != null);

                        if (PfWindow != null)
                        {
                            PfWindow.SendAction(1, 3, 0xE);
                            await Coroutine.Wait(5000, () => PfConditionWindow != null);

                            if (PfConditionWindow != null)
                            {
                                var elements = ___Elements(PfConditionWindow);
                                var data = Core.Memory.ReadString((IntPtr)elements[184].Data, Encoding.UTF8);
                                if (data != "")
                                {
                                    PfConditionWindow.SendAction(1, 3, 0x0);
                                    Log.Information("Registering PF");
                                    await Coroutine.Sleep(2000);
                                    await Coroutine.Wait(5000, () => PfConditionWindow == null);
                                    if (PfWindow != null)
                                    {
                                        Log.Information("Closing PF window");
                                        PfWindow.SendAction(1, 3, uint.MaxValue);
                                    }
                                }
                                else
                                {
                                    Log.Error("No Comment Setup. Quiting");
                                    TreeRoot.Stop("No Comment Setup");
                                }
                            }
                            else
                            {
                                Log.Error("Condition window didn't open");
                                TreeRoot.Stop("Shit Happens");
                            }
                        }
                        else
                        {
                            Log.Error("PF window didn't open");
                            TreeRoot.Stop("Shit Happens");
                        }
                    }

                    break;
                }
            }

            return true;
        }

        private int GetAgent()
        {
            var patternFinder = new PatternFinder(Core.Memory);
            var agentVtable = patternFinder.Find("48 8D 05 ? ? ? ? 48 89 51 ? 48 89 01 48 8B F9 48 8D 05 ? ? ? ? 4C 89 79 ? Add 3 TraceRelative");

            return AgentModule.FindAgentIdByVtable(agentVtable);
        }

        private static TwoInt[] ___Elements(AtkAddonControl WindowByName)
        {
            if (WindowByName == null)
            {
                return null;
            }

            var elementCount = ElementCount(WindowByName);
            var addr = Core.Memory.Read<IntPtr>(WindowByName.Pointer + Offset2);
            return Core.Memory.ReadArray<TwoInt>(addr, elementCount);
        }

        private static ushort ElementCount(AtkAddonControl WindowByName)
        {
            return WindowByName != null ? Core.Memory.Read<ushort>(WindowByName.Pointer + Offset0) : (ushort)0;
        }
    }
}