using System.Reflection;

namespace Notification.Domain.Constants
{
    public class AppCts
    {
        public static readonly string AbsoluteProjectPath =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;

        /// <summary>
        /// Location of Fake Json Filepath
        /// </summary>
        public static class SeederRelativePath
        {
            public static string JsonPath = Path.Combine("Helpers", "SeedData");

            public static string NotificationPath = Path.Combine(JsonPath, "notificationData.json");
        }
    }
}
