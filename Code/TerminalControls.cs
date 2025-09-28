using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Catopia.Refined
{
    public static class TerminalControls
    {
        const string IdPrefix = "Catopia_Refined_";
        static bool Done = false;

        public static void DoOnce(IMyModContext context)
        {
            if (Done)
                return;
            Done = true;

            EditControls();
            CreateControls();
        }

        static bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<RefinedBlock>() != null;
        }

        static bool CustomHiddenCondition(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<RefinedBlock>() == null;
        }

        static void CreateControls()
        {
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyTextPanel>(""); // separators don't store the id
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                MyAPIGateway.TerminalControls.AddControl<IMyTextPanel>(c);
            }

            if (CommonSettings.Instance.EnableTestButton)
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTextPanel>(IdPrefix + "TestButton");
                c.Title = MyStringId.GetOrCompute("Test");
                c.Tooltip = MyStringId.GetOrCompute("Trigger a offline period");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                c.Action = (b) => { b?.GameLogic?.GetAs<RefinedBlock>()?.TestButtonToggle(); };

                MyAPIGateway.TerminalControls.AddControl<IMyTextPanel>(c);
            }

            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyTextPanel>(IdPrefix + "SliderReserveUranium");
                c.Title = MyStringId.GetOrCompute("Reserve Uranium");
                c.Tooltip = MyStringId.GetOrCompute("The number of Uranium ingots to leave in reactors.");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                c.Setter = (b, v) =>
                {
                    var logic = b?.GameLogic?.GetAs<RefinedBlock>();
                    if (logic != null)
                        logic.SliderReserveUranium.Value = (int)MathHelper.Clamp(v, 0f, 100f); // just a heads up that the given value here is not clamped by the game, a mod or PB can give lower or higher than the limits!
                };
                c.Getter = (b) => b?.GameLogic?.GetAs<RefinedBlock>()?.SliderReserveUranium.Value ?? 0;

                c.SetLimits(0, 100);
                //c.SetLimits((b) => 0, (b) => 10); // overload with callbacks to define limits based on the block instance.
                //c.SetDualLogLimits(0, 10, 2); // all these also have callback overloads
                //c.SetLogLimits(0, 10);

                // called when the value changes so that you can display it next to the label
                c.Writer = (b, sb) =>
                {
                    var logic = b?.GameLogic?.GetAs<RefinedBlock>();
                    if (logic != null)
                    {
                        float val = logic.SliderReserveUranium.Value;
                        sb.Append(Math.Round(val, 2)).Append(" ingots");
                    }
                };

                MyAPIGateway.TerminalControls.AddControl<IMyTextPanel>(c);
            }
        }


        static void EditControls()
        {
            List<IMyTerminalControl> controls;

            MyAPIGateway.TerminalControls.GetControls<IMyTextPanel>(out controls);

            foreach (IMyTerminalControl c in controls)
            {
                // a quick way to dump all IDs to SE's log
                /*                string name = MyTexts.GetString((c as IMyTerminalControlTitleTooltip)?.Title.String ?? "N/A");
                                string valueType = (c as ITerminalProperty)?.TypeName ?? "N/A";
                                Log.Msg($"[DEV] terminal property: id='{c.Id}'; type='{c.GetType().Name}'; valueType='{valueType}'; displayName='{name}'");
                */
                switch (c.Id)
                {
                    case "OnOff":
                    case "ShowInTerminal":
                    case "ShowInInventory":
                    case "ShowInToolbarConfig":
                    case "Name":
                    case "ShowOnHUD":
                    case "CustomData":
                        {
                            break;
                        }
                    default:
                        {
                            //c.Enabled = TerminalChainedDelegate.Create(c.Enabled, CustomHiddenCondition); // grays out
                            c.Visible = TerminalChainedDelegate.Create(c.Visible, CustomHiddenCondition); // hides
                            break;
                        }
                }
            }
        }
    }
}
