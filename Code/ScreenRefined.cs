using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRageMath;

namespace Catopia.Refined
{
    internal class ScreenRefined : ScreenBase
    {
        private const int DefaultCallCounter = 2;
        private List<string> screenText = new List<string>();

        private readonly Color GreenCRT = new Color(51, 255, 0);
        private CommonSettings settings = CommonSettings.Instance;

        private Stopwatch stopwatch = new Stopwatch();

        internal enum Mode
        {
            Text,
            Run
        }
        private Mode screenMode = Mode.Text;
        internal Mode ScreenMode
        {
            get
            {
                return screenMode;
            }
            set
            {
                if (screenMode != value)
                {
                    screenMode = value;
                    Dirty = true;
                }
            }
        }
        internal bool Dirty;
        private int callCounter;

        internal ScreenRefined() { }
        internal ScreenRefined(IMyTextSurfaceProvider surfaceProvider, int index)
        {
            base.Init(surfaceProvider, index);
            DefaultRotationOrScale = 0.85f;
            BackgroundColor = Color.MidnightBlue;
        }

        internal void Refresh(bool preLoad = false)
        {
            if (preLoad) return;

            if (Log.Debug) Log.Msg($"Refresh dirty={Dirty} counter={callCounter}");
            if (!Dirty && --callCounter > 0)
                return;
            callCounter = DefaultCallCounter;

            //if (settings.EnableTiming) stopwatch.Restart();
            switch (screenMode)
            {
                case Mode.Text:
                    {
                        //if (settings.EnableTiming) Log.Msg($"Refresh Elapsed before ScreenText() call {stopwatch.ElapsedTicks / 10.0} uS");

                        ScreenText();
                        break;
                    }
                case Mode.Run:
                    {
                        //if (settings.EnableTiming) Log.Msg($"Refresh Elapsed before ScreenRun() call {stopwatch.ElapsedTicks / 10.0} uS");

                        ScreenRun();
                        break;
                    }
            }
            //if (settings.EnableTiming) Log.Msg($"Refresh Elapsed total {stopwatch.ElapsedTicks / 10.0} uS");
        }

        internal void AddText(string text)
        {
            if (Log.Debug) Log.Msg($"AddText {text}");
            if (screenText.Count > 19)
                screenText.RemoveAt(0);
            screenText.Add(text);
            screenMode = Mode.Text;
            Dirty = true;
        }

        internal void ClearText()
        {
            screenText.Clear();
            Dirty = true;
        }

        internal void ScreenText(bool preLoad = false)
        {
            if (preLoad) return;

            var frame = GetFrame(Color.Black);

            var position = new Vector2(5, 0);

            foreach (var line in screenText)
            {
                frame.Add(NewTextSprite(line, position, GreenCRT));
                position.Y += LineSpaceing;
            }

            frame.Dispose();
            Dirty = false;

        }

        internal void ScreenRun(bool preLoad = false)
        {
            if (preLoad)
                return;
            //if (settings.EnableTiming) Log.Msg($"ScreenRun Elapsed entered ScreenRun {stopwatch.ElapsedTicks / 10.0} uS");
            var frame = GetFrame(BackgroundColor);
            //if (settings.EnableTiming) Log.Msg($"ScreenRun Elapsed after GetFrame {stopwatch.ElapsedTicks / 10.0} uS");

            var position = new Vector2(5, 5);
            var positionTab1 = new Vector2(170, 0);

            /*            for (int x = 0; x < viewport.Width; x += 50)
                            frame.Add(NewTextSprite("_", new Vector2(position.X + x, position.Y)));
                          for (int y = 0; y < 20; ++y)
                                    {
                                        frame.Add(NewTextSprite($"{y}", position));
                                        position.Y += LineSpaceing;
                                    }*/

            frame.Add(NewTextSprite("Reactors:", position));
            frame.Add(NewTextSprite($"{RunInfo.NumReactors}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("   Power:", position));
            frame.Add(NewTextSprite($"{RunInfo.ReactorPower:0.###} MWh", position + positionTab1, Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("   Uranium:", position));
            frame.Add(NewTextSprite($"{RunInfo.AvailableUranium}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing * 1.2f;

            frame.Add(NewTextSprite("Refineries:", position));
            frame.Add(NewTextSprite($"{RunInfo.NumRefineries}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("   Power:", position));
            frame.Add(NewTextSprite($"{RunInfo.TotalPower:0.###} MWh", position + positionTab1, Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("   Speed:", position));
            frame.Add(NewTextSprite($"x{RunInfo.TotalSpeed}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("   Yield(Avg):", position));
            frame.Add(NewTextSprite($"x{RunInfo.AvgYieldMultiplier}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing * 1.2f;

            frame.Add(NewTextSprite("Containers:", position));
            frame.Add(NewTextSprite($"{RunInfo.NumContainers}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing * 1.2f;

            frame.Add(NewTextSprite("Payment type:", position));
            frame.Add(NewTextSprite($"{settings.PaymentType}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing * 1f;

            switch (settings.PaymentType)
            {
                case CommonSettings.PaymentMode.PerHour:
                    {
                        frame.Add(NewTextSprite("Price SC/hr:", position));
                        frame.Add(NewTextSprite($"{settings.PricePerUnit} SC", position + positionTab1, Color.Green));
                        position.Y += LineSpaceing * 1f;

                        frame.Add(NewTextSprite("   % Ingots:", position));
                        frame.Add(NewTextSprite($"{settings.PriceUnitPercent:0.#}%", position + positionTab1, Color.Green));
                        break;
                    }
                case CommonSettings.PaymentMode.PerMWh:
                    {
                        frame.Add(NewTextSprite("Price SC/MWh:", position));
                        frame.Add(NewTextSprite($"{settings.PricePerUnit} SC", position + positionTab1, Color.Green));
                        position.Y += LineSpaceing * 1f;

                        frame.Add(NewTextSprite("   % MWh:", position));
                        frame.Add(NewTextSprite($"{settings.PriceUnitPercent:0.#}%", position + positionTab1, Color.Green));
                        break;

                    }
                default:
                    {
                        break;
                    }

            }
            position.Y += LineSpaceing * 2f;

            if (RunInfo.LastOfflineS != 0)
            {
                frame.Add(NewTextSprite("Offline Period:", position));
                frame.Add(NewTextSprite(TimeSpan.FromSeconds(RunInfo.LastOfflineS).ToString(@"d\d\ hh\:mm"), position + positionTab1, Color.Green));
                position.Y += LineSpaceing;

                if (RunInfo.OresProcessed == 0)
                {
                    frame.Add(NewTextSprite("Refining:", position));
                    frame.Add(NewTextSprite("No Ore.", position + positionTab1, Color.Yellow));
                    position.Y += LineSpaceing;
                }
                else
                {
                    frame.Add(NewTextSprite("Refining:", position));
                    frame.Add(NewTextSprite(TimeSpan.FromSeconds(RunInfo.TotalRefiningTime).ToString(@"d\d\ hh\:mm"), position + positionTab1, Color.Green));
                    position.Y += LineSpaceing;

                    frame.Add(NewTextSprite("   Ores:", position));
                    frame.Add(NewTextSprite($"{RunInfo.OresProcessed}", position + positionTab1, Color.Green));
                    position.Y += LineSpaceing;



                    switch (settings.PaymentType)
                    {
                        case CommonSettings.PaymentMode.PerHour:
                            {
                                frame.Add(NewTextSprite("   Paid for:", position));
                                frame.Add(NewTextSprite(TimeSpan.FromSeconds(RunInfo.CreditUnitsUsed).ToString(@"d\d\ hh\:mm"), position + positionTab1, Color.Green));
                                position.Y += LineSpaceing;

                                frame.Add(NewTextSprite("   Cost:", position));
                                frame.Add(NewTextSprite($"{RunInfo.SCpaid} SC", position + positionTab1, Color.Green));
                                position.Y += LineSpaceing;

                                frame.Add(NewTextSprite("   Ingots taken:", position));
                                frame.Add(NewTextSprite($"{RunInfo.AvgPercentCharge:0.#}%", position + positionTab1, Color.Green));
                                position.Y += LineSpaceing * 1.2f;
                                break;
                            }
                        case CommonSettings.PaymentMode.PerMWh:
                            {
                                frame.Add(NewTextSprite("   Paid for:", position));
                                frame.Add(NewTextSprite($"{RunInfo.CreditUnitsUsed / 3600.0:0.###} MWh", position + positionTab1, Color.Green));
                                position.Y += LineSpaceing;

                                frame.Add(NewTextSprite("   Cost:", position));
                                frame.Add(NewTextSprite($"{RunInfo.SCpaid} SC", position + positionTab1, Color.Green));
                                position.Y += LineSpaceing;

                                frame.Add(NewTextSprite("   Power taken:", position));
                                frame.Add(NewTextSprite($"{RunInfo.MWhPayment:0.###} MWh", position + positionTab1, Color.Green));
                                position.Y += LineSpaceing * 1.2f;
                                break;
                            }
                        default:
                            {
                                position.Y += LineSpaceing * 0.2f;
                                break;
                            }
                    }

                    frame.Add(NewTextSprite("Uranium Used:", position));
                    frame.Add(NewTextSprite($"{RunInfo.UraniumUsed:0.##}", position + positionTab1, Color.Green));
                    position.Y += LineSpaceing;
                }
            }

            Dirty = false;
            frame.Dispose();
            //            if (settings.EnableTiming) Log.Msg($"ScreenRun Elapsed Total {stopwatch.ElapsedTicks / 10.0} uS");

        }

        internal void ClearRun()
        {
            RunInfo = new ScreenRunInfo();
            Dirty = true;
        }

        internal ScreenRunInfo RunInfo = new ScreenRunInfo();
        internal class ScreenRunInfo
        {
            internal int NumReactors;
            internal float ReactorPower;
            internal int AvailableUranium;
            internal int NumRefineries;
            internal float TotalPower;
            internal float TotalSpeed;
            internal float AvgYieldMultiplier;
            internal float MaxRefiningTime;
            internal float RemainingRefiningTime;
            internal int NumContainers;
            internal int LastOfflineS;
            internal int SCpaid;
            internal int CreditUnitsUsed;
            internal float TotalRefiningTime;
            internal float UraniumUsed;
            internal float MWhPayment;
            internal int OresProcessed;
            internal float AvgPercentCharge;

            internal ScreenRunInfo()
            {
                var settings = CommonSettings.Instance;
            }
        }

    }
}
