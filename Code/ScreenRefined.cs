using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRageMath;

namespace Catopia.Refined
{
    internal class ScreenRefined : ScreenBase
    {
        private const int DefaultCallCounter = 2;
        private List<string> screenText = new List<string>();

        private readonly Color GreenCRT = new Color(51, 255, 0);

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


        internal ScreenRefined(IMyTextSurfaceProvider surfaceProvider, int index)
        {
            base.Init(surfaceProvider, index);
            DefaultRotationOrScale = 0.85f;
            BackgroundColor = Color.MidnightBlue;
        }

        internal void Refresh()
        {
            Log.Msg($"Refresh dirty={Dirty} counter={callCounter}");
            if (!Dirty && --callCounter > 0)
                return;
            callCounter = DefaultCallCounter;

            switch (screenMode)
            {
                case Mode.Text:
                    {
                        ScreenText();
                        break;
                    }
                case Mode.Run:
                    {
                        ScreenRun();
                        break;
                    }
            }

        }

        internal void AddText(string text)
        {
            Log.Msg($"AddText {text}");
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

        internal void ScreenText()
        {
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

        internal void ScreenRun()
        {
            var frame = GetFrame();
            var position = new Vector2(5, 0);
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
            frame.Add(NewTextSprite($"{RunInfo.ReactorPower:0.###}MWh", position + positionTab1, Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("   Uranium:", position));
            frame.Add(NewTextSprite($"{RunInfo.AvailableUranium}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing * 1.1f;

            frame.Add(NewTextSprite("Refineries:", position));
            frame.Add(NewTextSprite($"{RunInfo.NumRefineries}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("   Power:", position));
            frame.Add(NewTextSprite($"{RunInfo.TotalPower:0.###}MWh", position + positionTab1, Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("   Speed:", position));
            frame.Add(NewTextSprite($"x{RunInfo.TotalSpeed}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing;

            frame.Add(NewTextSprite("   Yield(Avg):", position));
            frame.Add(NewTextSprite($"x{RunInfo.AvgYieldMultiplier}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing * 1.1f;

            frame.Add(NewTextSprite("Containers:", position));
            frame.Add(NewTextSprite($"{RunInfo.NumContainers}", position + positionTab1, Color.Green));
            position.Y += LineSpaceing * 1.1f;

            frame.Add(NewTextSprite("Price:", position));
            frame.Add(NewTextSprite($"{RunInfo.PricePerHour} SC/hr", position + positionTab1, Color.Green));
            position.Y += LineSpaceing * 1f;

            frame.Add(NewTextSprite(" Alt. Yield:", position));
            frame.Add(NewTextSprite($"{RunInfo.Percent}%", position + positionTab1, Color.Green));
            position.Y += LineSpaceing * 1.1f;

            if (RunInfo.LastOfflineS != 0)
            {
                frame.Add(NewTextSprite("Offline:", position));
                frame.Add(NewTextSprite(TimeSpan.FromSeconds(RunInfo.LastOfflineS).ToString(@"d\d\ hh\:mm"), position + positionTab1, Color.Green));
                position.Y += LineSpaceing;


            }



            Dirty = false;
            frame.Dispose();
        }

        internal void ClearRun()
        {
            RunInfo = new ScreenRunInfo();
            Dirty = true;
        }

        internal ScreenRunInfo RunInfo = new ScreenRunInfo();
        internal struct ScreenRunInfo
        {
            internal int NumReactors;
            internal float ReactorPower;
            internal int AvailableUranium;
            internal int NumRefineries;
            internal float TotalPower;
            internal float TotalSpeed;
            internal float AvgYieldMultiplier;
            internal float MaxRefiningUnits;
            internal float RemainingRefiningUnits;
            internal int NumContainers;
            internal int PricePerHour;
            internal int Percent;
            internal int LastOfflineS;
        }

    }
}
