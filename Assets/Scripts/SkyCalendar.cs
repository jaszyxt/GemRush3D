using System;
using UnityEngine;

namespace GemRush
{
    /// The living calendar (DESIGN.md: "real dates matter, gently").
    /// Tuesdays bring Gloomfang's rain to any ordinary-sky level — it is,
    /// after all, his job. The week around the anniversary of Pip's first
    /// flight brings festival skies. No FOMO: miss a day and nothing is
    /// lost; the calendar only ever adds weather, never takes anything.
    public static class SkyCalendar
    {
        public enum Weather { None, Rain, FestivalWeek }

        /// Test-only override for the "today" the calendar reads. Null in
        /// shipping builds; probes set it to exercise specific dates.
        public static DateTime? OverrideToday;

        public static DateTime Today
        {
            get { return OverrideToday ?? DateTime.Today; }
        }

        /// The weather the given date carries. Pure — probe-testable.
        public static Weather WeatherFor(DateTime date)
        {
            // Rain: Tuesdays, Gloomfang's rain-carrying day.
            if (date.DayOfWeek == DayOfWeek.Tuesday) return Weather.Rain;

            // Festival week: within ±3 days of the month/day of Pip's
            // first flight on this device (year ignored — an anniversary).
            DateTime first;
            if (DateTime.TryParse(SaveSystem.FirstFlightDate, out first))
            {
                try
                {
                    DateTime anniversary = new DateTime(
                        date.Year, first.Month, first.Day);
                    double days = Math.Abs((date - anniversary).TotalDays);
                    // Wrap across the year boundary (e.g. Dec 30 vs Jan 2).
                    if (days > 182) days = 365 - days;
                    if (days <= 3f) return Weather.FestivalWeek;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Feb 29 first flight on a non-leap year: celebrate a
                    // day early rather than skip — gentle means gentle.
                    DateTime anniversary = new DateTime(date.Year, first.Month, 1)
                        .AddDays(first.Day - 2);
                    if (Math.Abs((date - anniversary).TotalDays) <= 3f)
                        return Weather.FestivalWeek;
                }
            }
            return Weather.None;
        }

        public static Weather TodayWeather()
        {
            return WeatherFor(Today);
        }

        // Rain-washed palette for ordinary skies (the "Raindance" family
        // the Two Suns and Far Isles packs already speak).
        public static readonly Color RainSky =
            new Color(0.55f, 0.66f, 0.78f);
        public static readonly Color RainFog =
            new Color(0.52f, 0.63f, 0.75f);

        /// Applies the rainy-day weather to a level definition — the
        /// Remixes.Remixed mutation. Only scalars change (palette, flag,
        /// mood); the geometry, gems and ghosts stay exactly as they were.
        public static void RainMutation(LevelDefinition l)
        {
            l.SkyColor = RainSky;
            l.FogColor = RainFog;
            l.RainyDay = true;
        }

        /// True when the calendar's rain may fall on this level: ordinary
        /// Day skies only. Realms keep their weather identity (a rainy
        /// Long Winter or a rained-out festival would break the mood the
        /// level is made of), and bonus flight is weatherless.
        public static bool RainAppliesTo(LevelDefinition l)
        {
            return TodayWeather() == Weather.Rain &&
                !l.BonusFlight &&
                l.ResolveMood() == SoundMood.Day;
        }
    }
}
