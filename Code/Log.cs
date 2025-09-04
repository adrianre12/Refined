using VRage.Utils;

namespace Catopia.Refined
{
    public static class Log
    {
        const string Prefix = "Refined";

        public static bool DebugLog;
        public static void Msg(string msg)
        {
            MyLog.Default.WriteLine($"{Prefix}: {msg}");
        }

        public static void Debug(string msg)
        {
            if (DebugLog)
                Msg($"[DEBUG] {msg}");
        }
    }
}
