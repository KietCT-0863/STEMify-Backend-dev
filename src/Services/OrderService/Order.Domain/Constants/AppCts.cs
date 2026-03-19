using System.Reflection;

namespace Order.Domain.Constants
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

            public static string OrganizationTypePath = Path.Combine(JsonPath, "organizationTypeData.json");
            public static string OrganizationPath = Path.Combine(JsonPath, "organizationData.json");
        }
    }
}
