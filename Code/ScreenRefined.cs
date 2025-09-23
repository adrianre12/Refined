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
                    dirty = true;
                }
            }
        }
        private bool dirty;
        private int callCounter;


        internal ScreenRefined(IMyTextSurfaceProvider surfaceProvider, int index)
        {
            base.Init(surfaceProvider, index);
            DefaultRotationOrScale = 0.85f;
            BackgroundColor = Color.MidnightBlue;
        }

        internal void Refresh()
        {
            Log.Msg($"Refresh dirty={dirty} counter={callCounter}");
            if (!dirty && --callCounter > 0)
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

        /*        internal void ScreenDocked(int cashSC, int freeSpaceK, int maxFillK, ControllerBlockBase controller)
                {
                    var frame = GetFrame();
                    var position = new Vector2(5, 0);
                    var positionX150 = new Vector2(170, 0);
                    //Func<int, Vector2> ph = (x) => { return new Vector2(position.X + x, position.Y); };
                    //for (int x = 0; x < viewport.Width; x += 50)
                    //    frame.Add(NewTextSprite("_", ph(x)));

                    frame.Add(NewTextSprite("Station Name:", position));
                    frame.Add(NewTextSprite($"'{controller.block.CubeGrid.DisplayName}'", position + positionX150, Color.Cyan));
                    position.Y += LineSpaceing;

                    frame.Add(NewTextSprite($"{energyName} Available:", position));
                    var availableKL = (int)Math.Round(controller.energyPump.SourceEnergy.TotalAvailable / 100) / 10; //fudge the rounding for display
                    frame.Add(NewTextSprite($"{availableKL}{unitName}", position + positionX150, availableKL > freeSpaceK ? Color.Green : Color.Red));
                    position.Y += LineSpaceing;

                    frame.Add(NewTextSprite($"Price SC/{unitName}:", position));
                    frame.Add(NewTextSprite($"SC {controller.Settings.PricePerK}", position + positionX150));
                    position.Y += 2 * LineSpaceing;

                    frame.Add(NewTextSprite("Ship Name:", position));
                    frame.Add(NewTextSprite($"'{controller.dockedShipName}'", position + positionX150, Color.Cyan));
                    position.Y += LineSpaceing;

                    frame.Add(NewTextSprite("Free Space:", position));
                    var holderStr = controller.energyPump.TargetHoldersCount == 1 ? holderSingular : holderPlural;
                    frame.Add(NewTextSprite($"{freeSpaceK}{unitName} in {controller.energyPump.TargetHoldersCount} {holderStr}", position + positionX150));
                    position.Y += LineSpaceing;

                    frame.Add(NewTextSprite("Max Price:", position));
                    var maxPrice = freeSpaceK * controller.Settings.PricePerK;
                    frame.Add(NewTextSprite($"SC {maxPrice}", position + positionX150));
                    position.Y += LineSpaceing;

                    frame.Add(NewTextSprite("SC Inserted:", position));
                    frame.Add(NewTextSprite($"SC {cashSC}", position + positionX150, maxPrice > cashSC ? Color.Red : Color.Green));
                    position.Y += LineSpaceing;

                    frame.Add(NewTextSprite("Transfer:", position));
                    frame.Add(NewTextSprite($"{maxFillK}{unitName}", position + positionX150, Color.Yellow));
                    position.Y += LineSpaceing;

                    frame.Add(NewTextSprite("Total Price:", position));
                    frame.Add(NewTextSprite($"SC {maxFillK * controller.Settings.PricePerK}", position + positionX150, Color.Yellow));
                    position.Y += LineSpaceing;

                    frame.Add(NewTextSprite(noteLine, position, 0.5f));
                    position.Y += LineSpaceing;

                    if (maxFillK > 0)
                    {
                        if (controller.enableTransfer.Value)
                            frame.Add(NewTextSprite($"Press button to Stop", position + new Vector2(25, 0), Color.Red));
                        else
                            frame.Add(NewTextSprite($"Press button to Start", position + new Vector2(25, 0), Color.Green));

                    }

                    frame.Add(NewTextSprite("Insert Space Credits", position + new Vector2(280, 0), Color.Yellow));


                    frame.Dispose();
                }*/




        internal void AddText(string text)
        {
            Log.Msg($"AddText {text}");
            if (screenText.Count > 19)
                screenText.RemoveAt(0);
            screenText.Add(text);
            screenMode = Mode.Text;
            dirty = true;
        }

        internal void ClearText()
        {
            screenText.Clear();
            dirty = true;
        }

        internal void ScreenText()
        {
            var frame = GetFrame(Color.Black);
            var position = new Vector2(5, 0);
            //var positionX150 = new Vector2(150, 0);
            //Func<int, Vector2> ph = (x) => { return new Vector2(position.X + x, position.Y); };
            //for (int x = 0; x < viewport.Width; x += 50)
            //    frame.Add(NewTextSprite("_", ph(x)));

            foreach (var line in screenText)
            {
                frame.Add(NewTextSprite(line, position, GreenCRT));
                position.Y += LineSpaceing;
            }

            frame.Dispose();
            dirty = false;
        }

        internal void ScreenRun()
        {
            var frame = GetFrame();
            var position = new Vector2(5, 0);
            //var positionX150 = new Vector2(170, 0);
            Func<int, Vector2> ph = (x) => { return new Vector2(position.X + x, position.Y); };
            for (int x = 0; x < viewport.Width; x += 50)
                frame.Add(NewTextSprite("_", ph(x)));
            for (int y = 0; y < 20; ++y)
            {
                frame.Add(NewTextSprite($"{y}", position));
                position.Y += LineSpaceing;
            }
            dirty = false;
            frame.Dispose();
        }

    }
}
