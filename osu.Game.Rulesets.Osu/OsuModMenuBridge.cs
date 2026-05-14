// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// ПУТЬ: osu.Game.Rulesets.Osu/OsuModMenuBridge.cs

using System;

namespace osu.Game.Rulesets.Osu
{
    public static class OsuModMenuBridge
    {
        private static Func<bool> getAutoPlay   = () => false;
        private static Func<bool> getRelax      = () => false;
        private static Func<bool> getBigHitbox   = () => false;

        public static bool AutoPlayEnabled  => getAutoPlay();
        public static bool NoMissEnabled    => false; // Убран
        public static bool RelaxEnabled     => getRelax();
        public static bool BigHitboxEnabled => getBigHitbox();

        public static void Init(
            Func<bool> autoPlay,
            Func<bool> relax,
            Func<bool> bigHitbox)
        {
            getAutoPlay  = autoPlay;
            getRelax     = relax;
            getBigHitbox = bigHitbox;
        }
    }
}