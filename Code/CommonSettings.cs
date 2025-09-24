using Sandbox.ModAPI;
using System;

namespace Catopia.Refined
{
    internal class CommonSettings
    {
        const string configFilename = "Config-Refined.xml";


        private static CommonSettings instance;

        private int pricePerHour = 1000;
        public int PricePerHour { get { return pricePerHour; } private set { pricePerHour = value; } }

        private float priceYieldMultiplier = 0.95f;
        public float PriceYieldMultiplier { get { return priceYieldMultiplier; } private set { priceYieldMultiplier = value; } }

        private int maxOfflineHours = 120;
        public int MaxOfflineHours { get { return maxOfflineHours; } private set { maxOfflineHours = value; } }

        private int maxRefineries = 10;
        public int MaxRefineries { get { return maxRefineries; } private set { maxRefineries = value; } }


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
                    Log.Msg($"ERROR: Could Not Load Settings From {configFilename}. Using Empty Configuration.");
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
