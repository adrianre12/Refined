using System.Diagnostics;
using VRage.Game.Components;
using VRageMath;

namespace Catopia.Refined
{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    internal class Session : MySessionComponentBase
    {
        public static Session Instance { get; private set; }

        public override void LoadData()
        {

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var screen = new ScreenRefined();
            screen.Refresh(true);
            screen.ScreenMode = ScreenRefined.Mode.Text;
            screen.ClearRun();
            screen.ClearText();
            screen.ScreenRun(true);
            screen.ScreenText(true);
            screen.GetFrame(Color.Black, true);
            screen.NewTextSprite("", Vector2.Zero);
            screen.NewTextSprite("", Vector2.Zero, Color.Black);

            var refinedBlock = new RefinedBlock();
            refinedBlock.Run(true);
            refinedBlock.Paused(true);
            refinedBlock.LoadFromModStorage(true);
            refinedBlock.SaveToModStorage(true);
            refinedBlock.CheckDuplicate(true);

            var containerInfo = new ContainerInfo(null, 0);
            containerInfo.FindContainerInventories(null, null, true);
            containerInfo.ConsumeRefinaryTime(true);
            containerInfo.ConsumeCreditUnits(true);
            float tmpF;
            containerInfo.RefinaryTimeCheck(0, 0, out tmpF, true);

            var refineryInfo = new RefineryInfo();
            refineryInfo.FindRefineriesInfo(null, true);
            refineryInfo.DisableRefineries(true);
            refineryInfo.Refresh(true);
            refineryInfo.RefinaryElapsedTime(true);
            refineryInfo.ConsumeUranium(0, 0, true);
            refineryInfo.CalcMaxRefiningTime(true);
            refineryInfo.EnableRefineries(true);

            var reactorInfo = new ReactorInfo();
            reactorInfo.FindReactorInfo(null, true);
            reactorInfo.Refresh(true);
            reactorInfo.ConsumeUranium(0, true);
            int tmpI = reactorInfo.MWseconds;

            containerInfo.RefineNext(true);
            containerInfo.RefineEnd(true);
            containerInfo.RefineContainer(null, true);
            containerInfo.RefineInventoryOre(null, null, true);

            CommonSettings settings = CommonSettings.Instance;
            var x = settings.PriceYieldMultiplier;
            x = settings.PricePowerMultiplier;
            x = settings.PriceUnitPercent;

            Log.Msg($"Preload Total Elapsed = {stopwatch.ElapsedTicks / 10.0} uS");
            Instance = this;
        }
    }
}
