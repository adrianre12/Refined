using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
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

            CreateControls();
        }

        static bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            return b?.GameLogic?.GetAs<RefinedBlock>() != null;
        }

        static void CreateControls()
        {
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyCargoContainer>(""); // separators don't store the id
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                MyAPIGateway.TerminalControls.AddControl<IMyCargoContainer>(c);
            }

            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCargoContainer>(IdPrefix + "TestButton");
                c.Title = MyStringId.GetOrCompute("Test");
                c.Tooltip = MyStringId.GetOrCompute("Trigger a offline period");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                c.Action = (b) => { b?.GameLogic?.GetAs<RefinedBlock>()?.TestButtonToggle(); };

                MyAPIGateway.TerminalControls.AddControl<IMyCargoContainer>(c);
            }
        }
    }
}
