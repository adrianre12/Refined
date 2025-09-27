using Sandbox.ModAPI;
using System;
using System.Xml.Serialization;

namespace Catopia.Refined
{
    public class CommonSettings
    {
        const string configFilename = "Config-Refined.xml";
        const string notesFilename = "Config-Refined.txt";
        const string notes = "Setting PricePerUint = 0 disables Space Credit charges.\nPaymentType [None|PerHour|PerMWh]";

        private static CommonSettings instance;

        public int MinOfflineMins = 15;
        public int MaxOfflineHours = 120;
        public int PricePerUnit = 1000;
        private float priceUnitPercent = 5f;
        public float PriceUnitPercent
        {
            get { return priceUnitPercent; }
            set { priceUnitPercent = Clamp(value, 0, 100); }
        }
        public PaymentMode PaymentType = PaymentMode.PerHour;
        public int MaxRefineries = 10;
        public int ReserveUranium = 50;

        [XmlIgnore]
        public float PriceYieldMultiplier
        {
            get { return 1 + (100 - priceUnitPercent) * 0.01f; }
        } // 5% -> 1.95f;

        [XmlIgnore]
        public float PricePowerMultiplier
        {
            get { return priceUnitPercent * 0.01f; }
        } // 5% -> 0.05f;

        public enum PaymentMode
        {
            None,
            PerHour,
            PerMWh
        }

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
            CreateNotes();
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

        private static void CreateNotes()
        {
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(notesFilename, typeof(CommonSettings)) == false)
            {
                try
                {
                    Log.Msg($"Creating notes file {notesFilename}");
                    using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(notesFilename, typeof(CommonSettings)))
                    {
                        writer.Write(notes);
                    }

                }
                catch (Exception exc)
                {
                    Log.Msg(exc.ToString());
                    Log.Msg($"ERROR: Could Not Create {notesFilename}.");
                }
            }
        }

        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }
    }
}
