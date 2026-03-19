namespace Notification.Domain.Entities
{
    public class FCMessage
    {
        public List<string> tokens { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public Dictionary<string, string> data { get; set; }
    }
}
