using System.Reflection;

namespace Product.Domain.Constants
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

            public static string PlanPath = Path.Combine(JsonPath, "planData.json");
            public static string KitPath = Path.Combine(JsonPath, "kitData.json");
            public static string KitImagePath = Path.Combine(JsonPath, "kitImageData.json");
            public static string ComponentPath = Path.Combine(JsonPath, "componentData.json");
            public static string KitComponentPath = Path.Combine(JsonPath, "kitComponentData.json");
        }
    }
}
