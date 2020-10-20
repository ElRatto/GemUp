using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using Random_Features.Libs;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Input = ExileCore.Input;

namespace GemUp
{
    public class GemUp : BaseSettingsPlugin<GemUpSettings>
    {
        private const string PickitRuleDirectory = "Pickit Rules";
        private readonly List<Entity> _entities = new List<Entity>();
        private readonly Stopwatch _pickUpTimer = Stopwatch.StartNew();
        private readonly Stopwatch DebugTimer = Stopwatch.StartNew();
        private readonly WaitTime toPick = new WaitTime(1);
        private readonly WaitTime wait1ms = new WaitTime(1);
        private readonly WaitTime wait2ms = new WaitTime(2);
        private readonly WaitTime wait3ms = new WaitTime(3);
        private readonly WaitTime waitForNextTry = new WaitTime(1);
        private Vector2 _clickWindowOffset;
        private Dictionary<string, int> _weightsRules = new Dictionary<string, int>();
        private WaitTime _workCoroutine;
        public DateTime buildDate;
        private uint coroutineCounter;
        private bool FullWork = true;
        public string MagicRuleFile;
        private WaitTime mainWorkCoroutine = new WaitTime(5);
        public string NormalRuleFile;
        private Coroutine gemupCoroutine;
        public string RareRuleFile;
        private WaitTime tryToPick = new WaitTime(7);
        public string UniqueRuleFile;
        private WaitTime waitPlayerMove = new WaitTime(10);
        private List<string> _customItems = new List<string>();

        public GemUp()
        {
            Name = "GemUp";
        }

        //https://stackoverflow.com/questions/826777/how-to-have-an-auto-incrementing-version-number-visual-studio
        public Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;
        public string PluginVersion { get; set; }
        private List<string> PickitFiles { get; set; }

        public override bool Initialise()
        {
            buildDate = new DateTime(2000, 1, 1).AddDays(Version.Build).AddSeconds(Version.Revision * 2);
            PluginVersion = $"{Version}";
            gemupCoroutine = new Coroutine(MainWorkCoroutine(), this, "Gem Up");
            Core.ParallelRunner.Run(gemupCoroutine);
            gemupCoroutine.Pause();
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
                yield return FindItemToPick();

                coroutineCounter++;
                gemupCoroutine.UpdateTicks(coroutineCounter);
                yield return _workCoroutine;
            }
        }

        public override void DrawSettings()
        {
            ImGui.BulletText($"v{PluginVersion}");
            ImGui.BulletText($"Last Updated: {buildDate}");
            Settings.PickUpKey = ImGuiExtension.HotkeySelector("Gemup Key: " + Settings.PickUpKey.Value.ToString(), Settings.PickUpKey);      
            Settings.ExtraDelay.Value = ImGuiExtension.IntSlider("Extra Click Delay", Settings.ExtraDelay);
            Settings.MouseSpeed.Value = ImGuiExtension.FloatSlider("Mouse speed", Settings.MouseSpeed);
            Settings.TimeBeforeNewClick.Value = ImGuiExtension.IntSlider("Time wait for new click", Settings.TimeBeforeNewClick);
            Settings.IdleGemUp.Value = ImGuiExtension.Checkbox("Auto Level Up Gems when standing still", Settings.IdleGemUp);

        }

        public override Job Tick()
        {
            if (Input.GetKeyState(Keys.Escape)) gemupCoroutine.Pause();

            var pickupwhenidle = false;

            if (Settings.IdleGemUp.Value)
            {
                if (GameController?.Player?.GetComponent<Actor>()?.Animation == AnimationE.Idle)
                {
                    
                    var SkillGemLevelUps = GameController.Game.IngameState.IngameUi.GemLvlUpPanel.GemsToLvlUp;

                    if (SkillGemLevelUps.Count > 0)
                    {
                        pickupwhenidle = true;
                        //LogMessage("can lvl up", 1);
                    }


                }

            }

            if (Input.GetKeyState(Settings.PickUpKey.Value) || pickupwhenidle == true)
            {
                DebugTimer.Restart();

                if (gemupCoroutine.IsDone)
                {
                    var firstOrDefault = Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(GemUp));

                    if (firstOrDefault != null)
                        gemupCoroutine = firstOrDefault;
                }

                gemupCoroutine.Resume();
                FullWork = false;
            }
            else
            {
                if (FullWork)
                {
                    gemupCoroutine.Pause();
                    DebugTimer.Reset();
                }
            }

            if (DebugTimer.ElapsedMilliseconds > 300)
            {
                FullWork = true;
                LogMessage("Error gem up stop after time limit 300 ms", 1);
                DebugTimer.Reset();
            }
            //Graphics.DrawText($@"PICKIT :: Debug Tick Timer ({DebugTimer.ElapsedMilliseconds}ms)", new Vector2(100, 100), FontAlign.Left);
            //DebugTimer.Reset();

            return null;
        }


        private IEnumerator FindItemToPick()
        {
            if (Settings.IdleGemUp.Value) //gems beim idlen aufleveln
            {
                if (!GameController.Window.IsForeground()) yield break;
            }
            else //mit key
            {
                if ((!Input.GetKeyState(Settings.PickUpKey.Value)) || !GameController.Window.IsForeground()) yield break;
            }

            var currentLabels = new List<RectangleF>();

            var SkillGemLevelUps = GameController.Game.IngameState.IngameUi.GemLvlUpPanel.GemsToLvlUp;

            if (SkillGemLevelUps == null) yield break;


            foreach (Element element in SkillGemLevelUps)
            {
                if (element == null) continue;
                
                RectangleF skillGemButton = element.GetChildAtIndex(1).GetClientRect();

                string skillGemText = element.GetChildAtIndex(3).Text;
                if (element.GetChildAtIndex(2).IsVisibleLocal) continue;


                if (skillGemText?.ToLower() == "click to level up")
                {
                    currentLabels.Add(skillGemButton);

                }


            }

            GameController.Debug["GemUp"] = currentLabels;

            var pickUpThisItem = currentLabels.FirstOrDefault();

            yield return TryToPickV2(pickUpThisItem);

            FullWork = true;
        }

        private IEnumerator TryToPickV2(RectangleF pickItItem)
        {
            if (pickItItem == null)
            {
                FullWork = true;
                LogMessage("Gem is not valid.", 5, Color.Red);
                yield break;
            }

            var clickpoint = Misc.GetClickPos(pickItItem);
            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

            var oldMousePosition = Misc.RandomizePos(Mouse.GetCursorPositionVector()); //nicht genau an alte position
            
            _clickWindowOffset = rectangleOfGameWindow.TopLeft;

            var vector2 = clickpoint + _clickWindowOffset;

            Mouse.MoveCursorToPosition(vector2);
            yield return wait2ms;


            yield return Mouse.LeftClick();

            yield return toPick;

            Mouse.MoveCursorToPosition(oldMousePosition);

        }

        #region (Re)Loading Rules


        public override void OnPluginDestroyForHotReload()
        {
            gemupCoroutine.Done(true);
        }

        #endregion

        #region Adding / Removing Entities

        public override void EntityAdded(Entity Entity)
        {
        }

        public override void EntityRemoved(Entity Entity)
        {
        }

        #endregion
    }
}
