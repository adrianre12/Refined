using System.Diagnostics;
using VRage.Game.Components;

namespace Catopia.Refined
{

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    internal class Session : MySessionComponentBase
    {
        public static Session Instance { get; private set; }

        public override void LoadData()
        {
            return;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            long a = stopwatch.ElapsedTicks;
            var screen = new ScreenRefined();
            long b = stopwatch.ElapsedTicks;
            screen.ScreenRun(true);
            long c = stopwatch.ElapsedTicks;

            Log.Msg($"Preload Elapsed a={a / 10.0} uS b={b / 10.0} uS c={c / 10.0} uS");
            Instance = this;
        }
    }
}
