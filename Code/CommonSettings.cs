using Sandbox.ModAPI;
using System;

namespace Catopia.Refined
{
    public class CommonSettings
    {
        const string configFilename = "Config-Refined.xml";


        private static CommonSettings instance;

        public int MinOfflineMins = 15;
        public int MaxOfflineHours = 120;
        public int PricePerHour = 1000;
        public float PriceYieldMultiplier = 0.95f;
        public int MaxRefineries = 10;
        public int ReserveUranium = 50;

        public CommonSettings() { }

        public static CommonSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = LoadSettings();
                }
                return instance;
            }
        }

        private static CommonSettings LoadSettings()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(configFilename, typeof(CommonSettings)) == true)
            {
                try
                {
                    CommonSettings config = null;
                    var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(configFilename, typeof(CommonSettings));
                    string configcontents = reader.ReadToEnd();
                    config = MyAPIGateway.Utilities.SerializeFromXML<CommonSettings>(configcontents);

                    Log.Msg($"Loaded Existing Settings From {configFilename}");
                    return config;
                }
                catch (Exception exc)
                {
                    Log.Msg(exc.ToString());
                    Log.Msg($"ERROR: Could Not Load Settings From {configFilename}. Using Default Configuration.");
                    return new CommonSettings();
                }

            }

            Log.Msg($"{configFilename} Doesn't Exist. Creating Default Configuration. ");

            var defaultSettings = new CommonSettings();

            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(configFilename, typeof(CommonSettings)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML<CommonSettings>(defaultSettings));
                }

            }
            catch (Exception exc)
            {
                Log.Msg(exc.ToString());
                Log.Msg($"ERROR: Could Not Create {configFilename}. Default Settings Will Be Used.");
            }

            return defaultSettings;
        }
    }
}
