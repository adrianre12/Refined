using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using VRageMath;

namespace Catopia.Refined
{
    public class CommonSettings
    {
        const string configFilename = "Config-Refined.xml";
        const string notesFilename = "Config-Refined.txt";
        private static readonly List<string> notes = new List<string>{ "Setting PricePerUint = 0 disables Space Credit charges.",
            "PaymentType [None|PerHour|PerMWh]",
            "For PerMWh consider setting PricePerUnit to at least the price of one Uranium Ingot, as one ingot equals 1 MWh.",
            "The vanilla price of Uranium ingots is 76823, but only PriceUnitPercent of this is charged.",
            "Example: A Refinery uses 560KW, with PricePerUnit=5%. The cost per hour = 0.56 * 0.05 * 76823 = 2151 SC/h",
            "EnableTestButton adds a button to the terminal that adds ores to the inventories and sets the offline period to 1 day",
            "Don't use this in production, it gives free ingots!",
            "EnableTiming logs elapsed time for the Run and Screen refresh."};

        private static CommonSettings instance;

        public int MinOfflineMins = 15;
        public int MaxOfflineHours = 120;
        public int PricePerUnit = 76823;
        private float priceUnitPercent = 5f;
        public float PriceUnitPercent
        {
            get { return priceUnitPercent; }
            set { priceUnitPercent = MathHelper.Clamp(value, 0, 100); }
        }
        public PaymentMode PaymentType = PaymentMode.PerMWh;
        public int MaxRefineries = 10;

        public bool EnableTestButton = false;
        public bool EnableTiming = false;


        public string Debug = null;

        [XmlIgnore]
        public float PriceYieldMultiplier
        {
            get { return (100 - priceUnitPercent) * 0.01f; }
        } // 5% -> 0.95f;

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
                        foreach (string note in notes)
                            writer.WriteLine(note);
                    }

                }
                catch (Exception exc)
                {
                    Log.Msg(exc.ToString());
                    Log.Msg($"ERROR: Could Not Create {notesFilename}.");
                }
            }
        }

    }
}
