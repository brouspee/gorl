// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content.PM;
using Microsoft.Maui.Devices;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;
using osu.Game;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens;
using osu.Game.Updater;
using osu.Game.Utils;
using osuTK;

namespace osu.Android
{
    public partial class OsuGameAndroid : OsuGame
    {
        [Cached]
        private readonly OsuGameActivity gameActivity;

        private readonly PackageInfo packageInfo;

        // Флаг для отслеживания подписки на изменения модов
        private bool isModIntegrationSetup;

        public override Vector2 ScalingContainerTargetDrawSize => new Vector2(1024, 1024 * DrawHeight / DrawWidth);

        public OsuGameAndroid(OsuGameActivity activity)
            : base(null)
        {
            gameActivity = activity;
            packageInfo = Application.Context.ApplicationContext!.PackageManager!.GetPackageInfo(Application.Context.ApplicationContext.PackageName!, 0).AsNonNull();
        }

        public override string Version
        {
            get
            {
                if (!IsDeployedBuild)
                    return @"local " + (DebugUtils.IsDebugBuild ? @"debug" : @"release");

                return packageInfo.VersionName.AsNonNull();
            }
        }

        public override Version AssemblyVersion => new Version(packageInfo.VersionName.AsNonNull().Split('-').First());

        protected override void LoadComplete()
        {
            base.LoadComplete();
            
            UserPlayingState.BindValueChanged(_ => updateOrientation());

            // Инициализация интеграции с ModMenu
            setupModIntegration();
        }

        /// <summary>
        /// Настройка интеграции ModMenu с системой модов игры
        /// </summary>
        private void setupModIntegration()
        {
            if (isModIntegrationSetup)
                return;

            try
            {
                // Подписываемся на изменения состояния ModMenu
                ModMenu.OnStateChanged += onModMenuStateChanged;
                
                isModIntegrationSetup = true;

                global::Android.Util.Log.Info("OsuGameAndroid", "ModMenu integration setup completed");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("OsuGameAndroid", $"Failed to setup ModMenu integration: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик изменения состояния ModMenu
        /// </summary>
        private void onModMenuStateChanged()
        {
            try
            {
                // Применяем моды при изменении состояния
                Schedule(() => applyModMenuMods());
                
                global::Android.Util.Log.Debug("OsuGameAndroid", $"ModMenu state changed: {ModMenu.GetDebugInfo()}");
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("OsuGameAndroid", $"Error handling ModMenu state change: {ex.Message}");
            }
        }

        /// <summary>
        /// Применить моды из ModMenu к текущему рулсету
        /// </summary>
        private void applyModMenuMods()
        {
            try
            {
                // Получаем текущий рулсет
                var ruleset = Ruleset.Value.CreateInstance();
                if (ruleset == null)
                {
                    global::Android.Util.Log.Warn("OsuGameAndroid", "Cannot apply mods: ruleset is null");
                    return;
                }

                // ModMenu управляет игровым поведением напрямую через Catch-специфичный код.
                // Не добавляем скрытые режимы в SelectedMods, чтобы сохранить стандартные ranked semantics.
                if (!SelectedMods.Disabled)
                {
                    global::Android.Util.Log.Info("OsuGameAndroid", "Refreshing ModMenu overlay state without changing selected mods");
                }
                else
                {
                    global::Android.Util.Log.Warn("OsuGameAndroid", "Cannot refresh ModMenu state: SelectedMods is disabled");
                }

                // Если мы уже в игре, обновляем состояние живого Catch-рулсета.
                if (ScreenStack.CurrentScreen is osu.Game.Screens.Play.Player player && player.DrawableRuleset is osu.Game.Rulesets.Catch.UI.DrawableCatchRuleset catchDrawable)
                    catchDrawable.RefreshOverlayState();
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("OsuGameAndroid", $"Error applying ModMenu mods: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Переопределяем смену экрана для применения модов при входе в игру
        /// </summary>
        protected override void ScreenChanged(IOsuScreen? current, IOsuScreen? newScreen)
        {
            base.ScreenChanged(current, newScreen);

            if (newScreen != null)
            {
                updateOrientation();

                // Если переходим на экран игры, применяем моды из ModMenu
                if (newScreen.GetType().Name.Contains("Player"))
                {
                    try
                    {
                        applyModMenuMods();
                        global::Android.Util.Log.Info("OsuGameAndroid", "Applied ModMenu mods on Player screen enter");
                    }
                    catch (Exception ex)
                    {
                        global::Android.Util.Log.Error("OsuGameAndroid", $"Error applying mods on screen change: {ex.Message}");
                    }
                }
            }
        }

        private void updateOrientation()
        {
            var orientation = MobileUtils.GetOrientation(this, (IOsuScreen)ScreenStack.CurrentScreen, gameActivity.IsTablet);

            switch (orientation)
            {
                case MobileUtils.Orientation.Locked:
                    gameActivity.RequestedOrientation = ScreenOrientation.Locked;
                    break;

                case MobileUtils.Orientation.Portrait:
                    gameActivity.RequestedOrientation = ScreenOrientation.Portrait;
                    break;

                case MobileUtils.Orientation.Default:
                    gameActivity.RequestedOrientation = gameActivity.DefaultOrientation;
                    break;
            }
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);
            host.Window.CursorState |= CursorState.Hidden;
        }

        protected override UpdateManager CreateUpdateManager() => new MobileUpdateNotifier();

        protected override BatteryInfo CreateBatteryInfo() => new AndroidBatteryInfo();

        /// <summary>
        /// Очистка ресурсов при закрытии игры
        /// </summary>
        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing && isModIntegrationSetup)
            {
                try
                {
                    ModMenu.OnStateChanged -= onModMenuStateChanged;
                    global::Android.Util.Log.Info("OsuGameAndroid", "ModMenu integration disposed");
                }
                catch (Exception ex)
                {
                    global::Android.Util.Log.Error("OsuGameAndroid", $"Error disposing ModMenu integration: {ex.Message}");
                }
            }

            base.Dispose(isDisposing);
        }

        private class AndroidBatteryInfo : BatteryInfo
        {
            public override double? ChargeLevel => Battery.ChargeLevel;

            public override bool OnBattery => Battery.PowerSource == BatteryPowerSource.Battery;
        }
    }
}
