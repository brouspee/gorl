// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// ПУТЬ: osu.Android/ModMenu.cs

using System;
using System.IO;
using System.Text.Json;
using Android.Content;
using Android.Preferences;

namespace osu.Android
{
    public static class ModMenu
    {
        private static readonly object sync = new object();
        private static ISharedPreferences? prefs;

        private static bool autoPlay;
        private static bool relax;
        private static bool instantSpin;
        private static bool forceRanked;
        private static bool catchAssist;
        private static bool bigHitbox;
        private static bool easyTiming;

        public static event Action? OnStateChanged;

        // ---- Свойства ----
        public static bool AutoPlayEnabled    { get { lock (sync) return autoPlay; } }
        public static bool NoMissEnabled  { get { lock (sync) return false; } }
        public static bool RelaxEnabled   { get { lock (sync) return relax; } }
        public static bool InstantSpinEnabled { get { lock (sync) return instantSpin; } }
        public static bool ForceRankedEnabled { get { lock (sync) return forceRanked; } }
        public static bool CatchAssistEnabled { get { lock (sync) return catchAssist; } }
        public static bool BigHitboxEnabled { get { lock (sync) return bigHitbox; } }
        public static bool EasyTimingEnabled { get { lock (sync) return easyTiming; } }

        // ---- Toggle методы ----
        public static void ToggleAutoPlay()
        {
            lock (sync) { autoPlay = !autoPlay; if (autoPlay) relax = false; }
            SaveSettings();
            fire();
        }

        public static void ToggleNoMiss() { }

        public static void ToggleRelax()
        {
            lock (sync) { relax = !relax; if (relax) autoPlay = false; }
            SaveSettings();
            fire();
        }

        public static void ToggleInstantSpin()
        {
            lock (sync) instantSpin = !instantSpin;
            SaveSettings();
            fire();
        }

        public static void ToggleForceRanked()
        {
            lock (sync) forceRanked = !forceRanked;
            SaveSettings();
            fire();
        }

        public static void ToggleCatchAssist()
        {
            lock (sync) catchAssist = !catchAssist;
            SaveSettings();
            fire();
        }

        public static void ToggleBigHitbox()
        {
            lock (sync) bigHitbox = !bigHitbox;
            SaveSettings();
            fire();
        }

        public static void ToggleEasyTiming()
        {
            lock (sync) easyTiming = !easyTiming;
            SaveSettings();
            fire();
        }

        public static void ResetAll()
        {
            lock (sync) autoPlay = relax = instantSpin = forceRanked = catchAssist = bigHitbox = easyTiming = false;
            SaveSettings();
            fire();
        }

        // ---- Инициализация ----
        public static void Init(Context context)
        {
            prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            LoadSettings();
        }

        // ---- Сохранение/Загрузка ----
        private static void SaveSettings()
        {
            try
            {
                var edit = prefs?.Edit();
                edit?.PutBoolean("mod_autoPlay", autoPlay);
                edit?.PutBoolean("mod_relax", relax);
                edit?.PutBoolean("mod_instantSpin", instantSpin);
                edit?.PutBoolean("mod_forceRanked", forceRanked);
                edit?.PutBoolean("mod_catchAssist", catchAssist);
                edit?.PutBoolean("mod_bigHitbox", bigHitbox);
                edit?.PutBoolean("mod_easyTiming", easyTiming);
                edit?.Apply();
            }
            catch { }
        }

        private static void LoadSettings()
        {
            try
            {
                lock (sync)
                {
                    if (prefs != null)
                    {
                        autoPlay = prefs.GetBoolean("mod_autoPlay", false);
                        relax = prefs.GetBoolean("mod_relax", false);
                        instantSpin = prefs.GetBoolean("mod_instantSpin", false);
                        forceRanked = prefs.GetBoolean("mod_forceRanked", false);
                        catchAssist = prefs.GetBoolean("mod_catchAssist", false);
                        bigHitbox = prefs.GetBoolean("mod_bigHitbox", false);
                        easyTiming = prefs.GetBoolean("mod_easyTiming", false);
                    }
                }
                fire();
            }
            catch { }
        }

        public static string GetDebugInfo() =>
            $"Auto={autoPlay} Relax={relax} BigHitbox={bigHitbox} EasyTiming={easyTiming}";

        private static void fire() => OnStateChanged?.Invoke();
    }
}


// ---- PATCHED ----
// NoMiss убран.
// EasyTiming добавлен.
// SharedPreferences работает.
// -----------------