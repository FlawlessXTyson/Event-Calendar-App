namespace EventCalenderApi.Helpers
{
    /// <summary>
    /// Provides current IST (India Standard Time = UTC+5:30) datetime.
    /// All event dates/times are stored and compared in IST.
    /// Registration deadlines are stored as UTC (sent from frontend as ISO string)
    /// and must be compared with UTC.
    /// </summary>
    public static class IstClock
    {
        private static readonly TimeZoneInfo _ist = GetIst();

        private static TimeZoneInfo GetIst()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
            catch { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        }

        /// <summary>Current IST time — use for event start/end comparisons.</summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _ist);

        /// <summary>Current UTC time — use for registration deadline comparisons.</summary>
        public static DateTime UtcNow => DateTime.UtcNow;
    }
}
