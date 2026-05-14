using Android.Preferences;
// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// ПУТЬ: osu.Android/ModMenu.cs

using System;

namespace osu.Android
{
    public static class ModMenu
    {
        private static readonly object sync = new object();
        private static ISharedPreferences? prefs;

        // Ключи для SharedPreferences
        private const string KEY_AUTO_PLAY = "mod_auto_play";
        private const string KEY_NO_MISS = "mod_no_miss";
        private const string KEY_RELAX = "mod_relax";
        private const string KEY_INSTANT_SPIN = "mod_instant_spin";
        private const string KEY_FORCE_RANKED = "mod_force_ranked";
        private const string KEY_CATCH_ASSIST = "mod_catch_assist";
        private const string KEY_BIG_HITBOX = "mod_big_hitbox";

        private static bool autoPlay;
        private static bool noMiss;
        private static bool relax;
        private static bool instantSpin;
        private static bool forceRanked;
        private static bool catchAssist;
        private static bool bigHitbox;

        public static event Action? OnStateChanged;

        /// <summary>
        /// Инициализировать SharedPreferences. Вызвать при старте приложения.
        /// </summary>
        public static void Init(ISharedPreferences preferenceStore)
        {
            prefs = preferenceStore;
            LoadState();
        }

        /// <summary>
        /// Сохранить все настройки в SharedPreferences.
        /// </summary>
        private static void SaveState()
        {
            if (prefs == null) return;

            lock (sync)
            {
                var edit = prefs.Edit();
                edit.PutBoolean(KEY_AUTO_PLAY, autoPlay);
                edit.PutBoolean(KEY_NO_MISS, noMiss);
                edit.PutBoolean(KEY_RELAX, relax);
                edit.PutBoolean(KEY_INSTANT_SPIN, instantSpin);
                edit.PutBoolean(KEY_FORCE_RANKED, forceRanked);
                edit.PutBoolean(KEY_CATCH_ASSIST, catchAssist);
                edit.PutBoolean(KEY_BIG_HITBOX, bigHitbox);
                edit.Apply();
            }
        }

        /// <summary>
        /// Загрузить настройки из SharedPreferences.
        /// </summary>
        private static void LoadState()
        {
            if (prefs == null) return;

            lock (sync)
            {
                autoPlay = prefs.GetBoolean(KEY_AUTO_PLAY, false);
                noMiss = prefs.GetBoolean(KEY_NO_MISS, false);
                relax = prefs.GetBoolean(KEY_RELAX, false);
                instantSpin = prefs.GetBoolean(KEY_INSTANT_SPIN, false);
                forceRanked = prefs.GetBoolean(KEY_FORCE_RANKED, false);
                catchAssist = prefs.GetBoolean(KEY_CATCH_ASSIST, false);
                bigHitbox = prefs.GetBoolean(KEY_BIG_HITBOX, false);
            }
            // Уведомить подписчиков о загрузке состояния
            fire();
        }

        public static bool AutoPlayEnabled   { get { lock (sync) return autoPlay;    } }
        public static bool NoMissEnabled     { get { lock (sync) return noMiss;      } }
        public static bool RelaxEnabled      { get { lock (sync) return relax;       } }
        public static bool InstantSpinEnabled{ get { lock (sync) return instantSpin; } }
        public static bool ForceRankedEnabled{ get { lock (sync) return forceRanked; } }
        public static bool CatchAssistEnabled{ get { lock (sync) return catchAssist; } }
        public static bool BigHitboxEnabled  { get { lock (sync) return bigHitbox;   } }

        /// <summary>
        /// AutoPlay включает автоматическую игру — Relax несовместим.
        /// </summary>
        public static void ToggleAutoPlay()
        {
            lock (sync)
            {
                autoPlay = !autoPlay;
                if (autoPlay)
                {
                    relax = false;
                }
            }
            SaveState();
            fire();
        }

        public static void ToggleNoMiss()
        {
            lock (sync)
            {
                noMiss = !noMiss;
            }
            SaveState();
            fire();
        }

        public static void ToggleRelax()
        {
            lock (sync)
            {
                relax = !relax;
                if (relax) autoPlay = false;
            }
            SaveState();
            fire();
        }

        public static void ToggleInstantSpin()
        {
            lock (sync) instantSpin = !instantSpin;
            SaveState();
            fire();
        }

        public static void ToggleForceRanked()
        {
            lock (sync) forceRanked = !forceRanked;
            SaveState();
            fire();
        }

        public static void ToggleCatchAssist()
        {
            lock (sync) catchAssist = !catchAssist;
            SaveState();
            fire();
        }

        public static void ToggleBigHitbox()
        {
            lock (sync) bigHitbox = !bigHitbox;
            SaveState();
            fire();
        }

        public static void ResetAll()
        {
            lock (sync)
            {
                autoPlay = noMiss = relax = instantSpin =
                forceRanked = catchAssist = bigHitbox = false;
            }
            SaveState();
            fire();
        }

        public static string GetDebugInfo()
        {
            lock (sync)
            {
                return $"AutoPlay={autoPlay} NoMiss={noMiss} Relax={relax} " +
                       $"InstantSpin={instantSpin} ForceRanked={forceRanked} " +
                       $"CatchAssist={catchAssist} BigHitbox={bigHitbox}";
            }
        }

        private static void fire()
        {
            try { OnStateChanged?.Invoke(); }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("ModMenu", $"OnStateChanged error: {ex}");
            }
        }
    }
}


// ---- PATCHED ----
// Menu state persistence via SharedPreferences.
// NoMiss auto-force removed.
// BigHitbox now handles relaxed timing assist.
// -----------------
