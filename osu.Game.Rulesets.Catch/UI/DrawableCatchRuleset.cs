// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osuTK;

#if ANDROID
using osu.Android;
#endif

namespace osu.Game.Rulesets.Catch.UI
{
    public partial class DrawableCatchRuleset : DrawableScrollingRuleset<CatchHitObject>
    {
        protected override bool UserScrollSpeedAdjustment => false;

        /// <summary>
        /// Список модов, которые реально применяются во время игры.
        /// Заполняется из ModMenu (оверлей) — встроенные моды игры игнорируются.
        /// </summary>
        private IReadOnlyList<Mod> activeMods = new List<Mod>();

        public DrawableCatchRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
            : base(ruleset, beatmap, mods)
        {
            Direction.Value = ScrollingDirection.Down;
            TimeRange.Value = GetTimeRange(beatmap.Difficulty.ApproachRate);
            VisualisationMethod = ScrollVisualisationMethod.Constant;
        }

        private CatchTouchInputMapper? touchInputMapper;
        private MouseInputHelper? relaxHelper;

        [BackgroundDependencyLoader]
        private void load()
        {
            // Строим список активных модов из состояния оверлея
            refreshModsFromOverlay();
            updateInputMapping();

#if ANDROID
            // Отслеживаем переключение оверлея в рантайме.
            ModMenu.OnStateChanged += onModMenuStateChanged;
#endif
        }

#if ANDROID
        private void onModMenuStateChanged()
        {
            Schedule(() =>
            {
                refreshModsFromOverlay();
                updateInputMapping();
            });
        }
#endif

        private void updateInputMapping()
        {
#if ANDROID
            if (ModMenu.RelaxEnabled)
            {
                if (touchInputMapper != null)
                {
                    KeyBindingInputManager.Remove(touchInputMapper);
                    touchInputMapper = null;
                }

                if (relaxHelper == null)
                {
                    relaxHelper = new RelaxMouseInputHelper((CatchPlayfield)Playfield);
                    KeyBindingInputManager.Add(relaxHelper);
                }
            }
            else
            {
                if (relaxHelper != null)
                {
                    KeyBindingInputManager.Remove(relaxHelper);
                    relaxHelper = null;
                }

                if (touchInputMapper == null)
                {
                    touchInputMapper = new CatchTouchInputMapper();
                    KeyBindingInputManager.Add(touchInputMapper);
                }
            }
#else
            if (touchInputMapper == null)
            {
                touchInputMapper = new CatchTouchInputMapper();
                KeyBindingInputManager.Add(touchInputMapper);
            }
#endif
        }

        /// <summary>
        /// Обновляет <see cref="activeMods"/> на основе текущего состояния ModMenu.
        /// Вызывается при каждом старте игры.
        /// </summary>
        private void refreshModsFromOverlay()
        {
#if ANDROID
            var mods = new List<Mod>();

            if (ModMenu.AutoPlayEnabled)
                mods.Add(new Mods.CatchModAutoplay());

            if (ModMenu.NoMissEnabled && !ModMenu.AutoPlayEnabled)
                mods.Add(new Mods.CatchModNoFail());

            if (ModMenu.RelaxEnabled)
                mods.Add(new Mods.CatchModRelax());

            activeMods = mods;
#endif
        }

        public void RefreshOverlayState()
        {
#if ANDROID
            refreshModsFromOverlay();
            updateInputMapping();
#endif
        }

        private class RelaxMouseInputHelper : MouseInputHelper, IKeyBindingHandler<CatchAction>
        {
            private readonly CatchPlayfield playfield;

            public RelaxMouseInputHelper(CatchPlayfield playfield)
            {
                this.playfield = playfield;
            }

            public bool OnPressed(KeyBindingPressEvent<CatchAction> _)
            {
                // Block key-based movement while Relax is active.
                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<CatchAction> _)
            {
            }

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                // Directly map touch/mouse position to catcher X when Relax is active.
                var relativeX = Math.Clamp(e.MousePosition.X / playfield.DrawSize.X, 0f, 1f);
                playfield.CatcherArea.SetCatcherPosition(relativeX * CatchPlayfield.WIDTH);
                return false;
            }
        }

        /// <summary>
        /// Возвращает true если мод данного типа активен через оверлей.
        /// Используется в Catch-специфичных системах (Catcher, HealthProcessor и т.д.)
        /// </summary>
        public bool IsOverlayModActive<T>() where T : Mod
        {
#if ANDROID
            if (typeof(T) == typeof(ModAutoplay) || typeof(T) == typeof(Mods.CatchModAutoplay))
                return ModMenu.AutoPlayEnabled;

            if (typeof(T) == typeof(ModNoFail) || typeof(T) == typeof(Mods.CatchModNoFail))
                return ModMenu.NoMissEnabled;

            if (typeof(T) == typeof(ModRelax) || typeof(T) == typeof(Mods.CatchModRelax))
                return ModMenu.RelaxEnabled;
#endif
            return false;
        }

        protected double GetTimeRange(float approachRate) =>
            IBeatmapDifficultyInfo.DifficultyRange(approachRate, 1800, 1200, 450);

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay) =>
            new CatchFramedReplayInputHandler(replay);

        protected override ReplayRecorder CreateReplayRecorder(Score score) =>
            new CatchReplayRecorder(score, (CatchPlayfield)Playfield);

        protected override Playfield CreatePlayfield() => new CatchPlayfield(Beatmap.Difficulty);

        public override PlayfieldAdjustmentContainer CreatePlayfieldAdjustmentContainer() =>
            new CatchPlayfieldAdjustmentContainer();

        protected override PassThroughInputManager CreateInputManager() =>
            new CatchInputManager(Ruleset.RulesetInfo);

        public override DrawableHitObject<CatchHitObject>? CreateDrawableRepresentation(CatchHitObject h) => null;

        protected override ResumeOverlay CreateResumeOverlay() =>
            new DelayedResumeOverlay { Scale = new Vector2(0.65f) };

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
#if ANDROID
                ModMenu.OnStateChanged -= onModMenuStateChanged;
#endif
            }

            base.Dispose(isDisposing);
        }
    }
}
