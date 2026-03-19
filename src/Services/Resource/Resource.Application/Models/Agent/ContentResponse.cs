namespace Resource.Application.Models.Agent.ContentResponse
{
    public sealed class ContentResponse
    {
        public Candidate[] Candidates { get; set; }
        public PromptFeedback PromptFeedback { get; set; }
    }

    public sealed class PromptFeedback
    {
        public SafetyRating[] SafetyRatings { get; set; }
    }

    public sealed class Candidate
    {
        public Content Content { get; set; }
        public string FinishReason { get; set; }
        public int Index { get; set; }
        public SafetyRating[] SafetyRatings { get; set; }
    }

    public sealed class Content
    {
        public Part[] Parts { get; set; }
        public string Role { get; set; }
    }

    public sealed class Part
    {
        // This one interests us the most
        public string Text { get; set; }
    }

    public sealed class SafetyRating
    {
        public string Category { get; set; }
        public string Probability { get; set; }
    }
}
