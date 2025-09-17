using VRage.Utils;

namespace Catopia.Refined
{
    public static class Log
    {
        const string Prefix = "Refined";

        public static bool Debug;
        public static void Msg(string msg)
        {
            MyLog.Default.WriteLine($"{Prefix}: {msg}");
        }
    }
}
