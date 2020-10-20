using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared;
using ExileCore.Shared.Enums;
using ImGuiNET;
using SharpDX;

namespace GemUp
{
    public class GemUp : BaseSettingsPlugin<GemUpSettings>
    {
        private readonly Stopwatch _debugTimer = Stopwatch.StartNew();
        private readonly WaitTime _toPick = new WaitTime(1);
        private readonly WaitTime _wait2Ms = new WaitTime(2);
        private Vector2 _clickWindowOffset;

        private WaitTime _workCoroutine;
        private DateTime _buildDate;
        private uint _coroutineCounter;
        private int _counter;
        private bool _fullWork = true;
        private Coroutine _gemLvlCoroutine;

        public GemUp()
        {
            Name = "GemUp";
        }

        //https://stackoverflow.com/questions/826777/how-to-have-an-auto-incrementing-version-number-visual-studio
        private Version Version { get; } =
            Assembly.GetExecutingAssembly().GetName().Version;

        private string PluginVersion { get; set; }

        public override bool Initialise()
        {
            _buildDate = new DateTime(2000, 1, 1).AddDays(Version.Build)
                .AddSeconds(Version.Revision * 2);
            PluginVersion = $"{Version}";
            _gemLvlCoroutine =
                new Coroutine(MainWorkCoroutine(), this, "Gem Up");
            Core.ParallelRunner.Run(_gemLvlCoroutine);
            _gemLvlCoroutine.Pause();
            _debugTimer.Reset();
            Settings.MouseSpeed.OnValueChanged += (sender, f) =>
            {
                Mouse.MouseSpeed = Settings.MouseSpeed.Value;
            };
            _workCoroutine = new WaitTime(Settings.ExtraDelay);
            Settings.ExtraDelay.OnValueChanged += (sender, i) =>
                _workCoroutine = new WaitTime(i);
            return true;
        }


        private IEnumerator MainWorkCoroutine()
        {
            while (true)
            {
                yield return FindItemToPick();

                _coroutineCounter++;
                _gemLvlCoroutine.UpdateTicks(_coroutineCounter);
                yield return _workCoroutine;
            }
        }

        public override void DrawSettings()
        {
            ImGui.BulletText($"v{PluginVersion}");
            ImGui.BulletText($"Last Updated: {_buildDate}");
            Settings.PickUpKey = ImGuiExtension.HotkeySelector(
                "Gemup Key: " + Settings.PickUpKey.Value,
                Settings.PickUpKey);
            Settings.ExtraDelay.Value =
                ImGuiExtension.IntSlider("Extra Click Delay",
                    Settings.ExtraDelay);
            Settings.MouseSpeed.Value =
                ImGuiExtension.FloatSlider("Mouse speed", Settings.MouseSpeed);
            Settings.TimeBeforeNewClick.Value =
                ImGuiExtension.IntSlider("Time wait for new click",
                    Settings.TimeBeforeNewClick);
            Settings.IdleGemUp.Value = ImGuiExtension.Checkbox(
                "Auto Level Up Gems when standing still", Settings.IdleGemUp);
            Settings.CheckEveryXTick.Value = ImGuiExtension.IntSlider(
                "only check every X tick for gems (10)",
                Settings.CheckEveryXTick);
        }

        public override Job Tick()
        {
            if (Settings.CheckEveryXTick.Value != 0)
            {
                if (_counter <= Settings.CheckEveryXTick.Value)
                {
                    _counter++;
                    return null;
                }

                _counter = 0;
            }

            if (Input.GetKeyState(Keys.Escape)) _gemLvlCoroutine.Pause();

            var clickWhenIdle = false;

            if (Settings.IdleGemUp.Value)
                if (GameController?.Player?.GetComponent<Actor>()?.Animation ==
                    AnimationE.Idle)
                {
                    var skillGemLevelUps = GameController.Game.IngameState
                        .IngameUi.GemLvlUpPanel.GemsToLvlUp;

                    if (skillGemLevelUps.Count > 0)
                        clickWhenIdle = true;
                }

            if (Input.GetKeyState(Settings.PickUpKey.Value) ||
                clickWhenIdle)
            {
                _debugTimer.Restart();

                if (_gemLvlCoroutine.IsDone)
                {
                    var firstOrDefault =
                        Core.ParallelRunner.Coroutines.FirstOrDefault(x =>
                            x.OwnerName == nameof(GemUp));

                    if (firstOrDefault != null)
                        _gemLvlCoroutine = firstOrDefault;
                }

                _gemLvlCoroutine.Resume();
                _fullWork = false;
            }
            else
            {
                if (_fullWork)
                {
                    _gemLvlCoroutine.Pause();
                    _debugTimer.Reset();
                }
            }

            if (_debugTimer.ElapsedMilliseconds > 300)
            {
                _fullWork = true;
                LogMessage("Error gem up stop after time limit 300 ms");
                _debugTimer.Reset();
            }

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
                if (!Input.GetKeyState(Settings.PickUpKey.Value) ||
                    !GameController.Window.IsForeground()) yield break;
            }

            var currentLabels = new List<RectangleF>();

            var skillGemLevelUps = GameController.Game.IngameState.IngameUi
                .GemLvlUpPanel.GemsToLvlUp;

            if (skillGemLevelUps == null) yield break;


            foreach (var element in skillGemLevelUps)
            {
                if (element == null) continue;

                var skillGemButton =
                    element.GetChildAtIndex(1).GetClientRect();

                var skillGemText = element.GetChildAtIndex(3).Text;
                if (element.GetChildAtIndex(2).IsVisibleLocal) continue;


                if (skillGemText?.ToLower() == "click to level up")
                    currentLabels.Add(skillGemButton);
            }

            GameController.Debug["GemUp"] = currentLabels;

            var pickUpThisItem = currentLabels.FirstOrDefault();

            yield return TryToClick(pickUpThisItem);

            _fullWork = true;
        }

        private IEnumerator TryToClick(RectangleF rectangleF)
        {
            var clickPos = Misc.GetClickPos(rectangleF);
            var rectangleOfGameWindow =
                GameController.Window.GetWindowRectangleTimeCache;

            var oldMousePosition =
                Misc.RandomizePos(Mouse.GetCursorPositionVector());

            _clickWindowOffset = rectangleOfGameWindow.TopLeft;

            var vector2 = clickPos + _clickWindowOffset;

            Mouse.MoveCursorToPosition(vector2);
            yield return _wait2Ms;


            yield return Mouse.LeftClick();

            yield return _toPick;

            Mouse.MoveCursorToPosition(oldMousePosition);
        }

        #region (Re)Loading Rules

        public override void OnPluginDestroyForHotReload()
        {
            _gemLvlCoroutine.Done(true);
        }

        #endregion
    }
}