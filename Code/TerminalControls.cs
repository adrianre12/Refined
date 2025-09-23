using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using VRage;
using VRage.Game.ModAPI;
using VRage.Utils;

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

            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyTextPanel>(IdPrefix + "TestButton");
                c.Title = MyStringId.GetOrCompute("Test");
                c.Tooltip = MyStringId.GetOrCompute("Trigger a offline period");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                c.Action = (b) => { b?.GameLogic?.GetAs<RefinedBlock>()?.TestButtonToggle(); };

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
                string name = MyTexts.GetString((c as IMyTerminalControlTitleTooltip)?.Title.String ?? "N/A");
                string valueType = (c as ITerminalProperty)?.TypeName ?? "N/A";
                Log.Msg($"[DEV] terminal property: id='{c.Id}'; type='{c.GetType().Name}'; valueType='{valueType}'; displayName='{name}'");

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
