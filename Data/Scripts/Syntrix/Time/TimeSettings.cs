// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;

namespace Syntrix.Time
{
    /// <summary>
    /// Settings of the world clock, file Syntrix-Time.xml in the world storage of Syntrix (written with these defaults on
    /// the first start; the server sends it to every player). The world time is the same on every server because it
    /// only depends on the real UTC time and these values:
    ///   world days = start + (UTC now - ReferenceDate) * Factor,   Factor = 1440 / real minutes per world day.
    /// Changing the factor without a jump: ReferenceDate = now, StartYear/StartDay/StartHour = the current world date
    /// (chat /syxtime shows it), then the new factor.
    /// </summary>
    public class TimeSettings
    {
        /// <summary>World days per real day (6 = one world day lasts 240 real minutes). Same on every server.</summary>
        public double Factor = 6.0;
        /// <summary>Real UTC time of the start date.</summary>
        public string ReferenceDate = "2026-11-11T00:00:00Z";
        /// <summary>World year at the reference date.</summary>
        public int StartYear = 1;
        /// <summary>Day of the year at the reference date (from 1).</summary>
        public int StartDay = 1;
        /// <summary>Hour at the reference date.</summary>
        public double StartHour = 0.0;
        /// <summary>Days per world year.</summary>
        public int YearLength = 365;
        /// <summary>Player machines turn the sky with the sun.</summary>
        public bool RotateSky = true;
        /// <summary>
        /// Server: once a minute the game time is set so that the local time on the -Z side of every planet equals the
        /// world time (with a solar day other than the world day in proportion). Sun and local time then keep running
        /// with real time, also while the server is off. The admin slider "time of day" only works until the next minute.
        /// </summary>
        public bool CoupleSun = true;
        /// <summary>Own calendars of planets (local date beside the local time).</summary>
        public List<PlanetCalendar> Calendars = new List<PlanetCalendar>
        {
            new PlanetCalendar { Name = "Earth", Planets = "Sol-Earth", Kind = "Date", Start = "2200-01-01" },
        };
    }

    /// <summary>A planet calendar: one local day per solar day, the date changes at 00:00 local time.</summary>
    public class PlanetCalendar
    {
        /// <summary>Name shown with the date, also a text key (Syntrix_Time_Calendar_&lt;Name&gt;).</summary>
        public string Name = "";
        /// <summary>Planet SubtypeIds, separated by commas.</summary>
        public string Planets = "";
        /// <summary>"Date": Gregorian date from Start (yyyy-MM-dd); "Days": day number counted from Start (a number).</summary>
        public string Kind = "Date";
        /// <summary>Date or day number at world day 0 (year 1, day 1) at the -Z meridian.</summary>
        public string Start = "";
    }
}
