using VRage.Utils;

namespace Refined.Controller
{
    public static class MyLogExtension
    {
        public static void WriteLineIf(this MyLog myLog, bool enable, string message)
        {
            if (enable)
                myLog.WriteLine($"[DEBUG] SeRefined.Controller: {message}");
            else myLog.WriteLine($"SeRefined.Controller: {message}");
        }
    }
}
