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
            long a = stopwatch.ElapsedTicks;
            var screen = new ScreenRefined();
            screen.Refresh(true);
            long b = stopwatch.ElapsedTicks;
            screen.ScreenRun(true);
            screen.ScreenText(true);
            screen.GetFrame(Color.Black, true);
            screen.NewTextSprite("", Vector2.Zero);
            screen.NewTextSprite("", Vector2.Zero, Color.Black);

            long c = stopwatch.ElapsedTicks;

            Log.Msg($"Preload ScreenRun Elapsed a={a / 10.0} uS b={b / 10.0} uS c={c / 10.0} uS");

            stopwatch.Restart();
            var refinedBlock = new RefinedBlock();
            a = stopwatch.ElapsedTicks;
            refinedBlock.Run(true);
            b = stopwatch.ElapsedTicks;
            refinedBlock.Paused(true);
            refinedBlock.LoadFromModStorage(true);
            refinedBlock.SaveToModStorage(true);
            refinedBlock.CheckDuplicate(true);
            c = stopwatch.ElapsedTicks;

            Log.Msg($"Preload refinedBlock Elapsed a={a / 10.0} uS b={b / 10.0} uS c={c / 10.0} uS");

            stopwatch.Restart();
            var containerInfo = new ContainerInfo();
            containerInfo.FindContainerInventories(null, null, true);
            containerInfo.ConsumeRefinaryTime(true);
            containerInfo.ConsumeCreditUnits(true);
            a = stopwatch.ElapsedTicks;

            var refineryInfo = new RefineryInfo();
            refineryInfo.FindRefineriesInfo(null, true);
            refineryInfo.DisableRefineries(true);
            refineryInfo.Refresh(true);
            refineryInfo.RefinaryElapsedTime(true);
            refineryInfo.ConsumeUranium(0, 0, true);
            b = stopwatch.ElapsedTicks;

            var reactorInfo = new ReactorInfo();
            reactorInfo.FindReactorInfo(null, true);
            reactorInfo.Refresh(true);
            reactorInfo.ConsumeUranium(0, true);
            c = stopwatch.ElapsedTicks;

            Log.Msg($"Preload Infos Elapsed a={a / 10.0} uS b={b / 10.0} uS c={c / 10.0} uS");

            stopwatch.Restart();
            containerInfo.RefineNext(true);
            a = stopwatch.ElapsedTicks;
            containerInfo.RefineContainer(null, true);
            b = stopwatch.ElapsedTicks;
            containerInfo.RefineInventoryOre(null, null, true);
            c = stopwatch.ElapsedTicks;

            Log.Msg($"Preload Refines Elapsed a={a / 10.0} uS b={b / 10.0} uS c={c / 10.0} uS");

            CommonSettings settings = CommonSettings.Instance;
            var x = settings.PriceYieldMultiplier;
            x = settings.PricePowerMultiplier;
            x = settings.PriceUnitPercent;

            Instance = this;
        }
    }
}
