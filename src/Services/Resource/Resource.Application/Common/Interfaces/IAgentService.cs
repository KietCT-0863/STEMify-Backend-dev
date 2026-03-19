namespace Resource.Application.Common.Interfaces
{
    public interface IAgentService
    {
        IAsyncEnumerable<string> GenerateCourseRecommendationsAsync(string userPrompt, string courses);
        IAsyncEnumerable<string> AnswerGeneralStemQuestionAsync(string userPrompt);
        IAsyncEnumerable<string> SummarizeSectionForPresentationAsync(string userPrompt, List<string> sectionContents);
    }
}
