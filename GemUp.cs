using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;
using Input = ExileCore.Input;

namespace GemUp
{
    public class GemUp : BaseSettingsPlugin<GemUpSettings>
    {
        private readonly Stopwatch DebugTimer = Stopwatch.StartNew();
        
        private WaitTime _workCoroutine;
        public DateTime buildDate;
        private uint coroutineCounter;
        private Vector2 _clickWindowOffset;
        private Vector2 cursorBeforeGemUp;
        private bool FullWork = true;
        public string MagicRuleFile;
        private WaitTime mainWorkCoroutine = new WaitTime(5);
        public string NormalRuleFile;
        private Coroutine gemUpCoroutine;
        public string RareRuleFile;
        private WaitTime tryToGem = new WaitTime(7);
        private WaitTime waitPlayerMove = new WaitTime(10);
        private readonly WaitTime waitForNextTry = new WaitTime(1000);
        private readonly WaitTime toGemUp = new WaitTime(10);
        private readonly WaitTime wait3ms = new WaitTime(3);

        public GemUp()
        {
            Name = "GemUp";
        }

        //https://stackoverflow.com/questions/826777/how-to-have-an-auto-incrementing-version-number-visual-studio
        public Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;
        public string PluginVersion { get; set; }

        public override bool Initialise()
        {
            buildDate = new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);
            PluginVersion = $"{Version}";
            gemUpCoroutine = new Coroutine(MainWorkCoroutine(), this, "Gem Up");
            Core.ParallelRunner.Run(gemUpCoroutine);
            gemUpCoroutine.Pause();
            DebugTimer.Reset();
            Settings.MouseSpeed.OnValueChanged += (sender, f) => { Mouse.speedMouse = Settings.MouseSpeed.Value; };
            _workCoroutine = new WaitTime(Settings.ExtraDelay);
            Settings.ExtraDelay.OnValueChanged += (sender, i) => _workCoroutine = new WaitTime(i);
            return true;
        }

        private IEnumerator MainWorkCoroutine()
        {
            while (true)
            {
                yield return GemItUp();

                coroutineCounter++;
                gemUpCoroutine.UpdateTicks(coroutineCounter);
                yield return _workCoroutine;
            }
        }

        

        public override Job Tick()
        {
            if (Input.GetKeyState(Keys.Escape)) gemUpCoroutine.Pause();

            if (Input.GetKeyState(Settings.GemUpKey.Value))
            {
                DebugTimer.Restart();

                if (gemUpCoroutine.IsDone)
                {
                    var firstOrDefault = Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(GemUp));

                    if (firstOrDefault != null)
                        gemUpCoroutine = firstOrDefault;
                }

                gemUpCoroutine.Resume();
                FullWork = false;
            }
            else
            {
                if (FullWork)
                {
                    gemUpCoroutine.Pause();
                    DebugTimer.Reset();
                }
            }

            if (DebugTimer.ElapsedMilliseconds > 2000)
            {
                FullWork = true;
                LogMessage("Error gem up stop after time limit 2000 ms", 1);
                DebugTimer.Reset();
            }

            return null;
        }

        

        
        //main
        private IEnumerator GemItUp()
        {
            if (!Input.GetKeyState(Settings.GemUpKey.Value) || !GameController.Window.IsForeground()) yield break;
 
            yield return TryToGemUp();
            FullWork = true;
        }

        private IEnumerator TryToGemUp()
        {
            var SkillGemLevelUps = GameController.Game.IngameState.IngameUi.GemLvlUpPanel.GetChildAtIndex(0);
            if (SkillGemLevelUps == null || !SkillGemLevelUps.IsVisible) yield return waitForNextTry;


            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;
            var oldMousePosition = Mouse.GetCursorPositionVector();
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            //hier gems erkennen


            foreach (Element element in SkillGemLevelUps.Children)
            {
                //if (element == null) continue;
                RectangleF skillGemButton = element.GetChildAtIndex(1).GetClientRect();

                string skillGemText = element.GetChildAtIndex(3).Text;
                if (element.GetChildAtIndex(2).IsVisibleLocal) continue;

                var clientRectCenter = skillGemButton.Center;

                var vector2 = clientRectCenter + _clickWindowOffset;

                if (skillGemText?.ToLower() == "click to level up")
                {
                    Mouse.MoveCursorToPosition(vector2);
                    yield return wait3ms;
                    Mouse.MoveCursorToPosition(vector2);
                    yield return wait3ms;
                    yield return Mouse.LeftClick();
                    yield return toGemUp;



                }
                
            }
            Mouse.MoveCursorToPosition(oldMousePosition);
            yield return waitForNextTry;
            

            //   
        }


    



        public override void OnPluginDestroyForHotReload()
        {
            gemUpCoroutine.Done(true);
        }
      
    }
}
