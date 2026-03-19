using Google.Protobuf.WellKnownTypes;
using System.Runtime.InteropServices;

namespace Shared.Helper
{
    public static class TimezoneHelper
    {
        private static TimeZoneInfo GetVietnamTimeZone()
        {
            string timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "SE Asia Standard Time"
                : "Asia/Ho_Chi_Minh";
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }

        public static DateTimeOffset GetCurrentVietnamTime()
        {
            try
            {
                var utcNow = DateTimeOffset.UtcNow;
                var vietnamTimeZone = GetVietnamTimeZone();
                return TimeZoneInfo.ConvertTime(utcNow, vietnamTimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                return DateTimeOffset.UtcNow;
            }
        }

        // DateOnly → Timestamp (UTC)
        public static Timestamp ToTimestamp(this DateOnly date)
        {
            var utcDateTime = DateTime.SpecifyKind(
                date.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc
            );
            return Timestamp.FromDateTime(utcDateTime);
        }

        // Timestamp → DateOnly (UTC)
        public static DateOnly ToDateOnly(this Timestamp timestamp)
        {
            var utcDateTime = timestamp.ToDateTime();
            return DateOnly.FromDateTime(utcDateTime);
        }

        // DateTime → Timestamp
        public static Timestamp ToUtcTimestamp(this DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            else if (dateTime.Kind == DateTimeKind.Local)
            {
                dateTime = dateTime.ToUniversalTime();
            }

            return Timestamp.FromDateTime(dateTime);
        }

        // Timestamp → DateTime (UTC)
        public static DateTime ToDateTimeUtc(this Timestamp timestamp)
        {
            return timestamp.ToDateTime();
        }
    }
}
