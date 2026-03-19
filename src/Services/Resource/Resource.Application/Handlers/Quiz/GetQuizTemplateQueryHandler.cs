using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using MediatR;
using Resource.Application.Queries.Assignment;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Quiz
{
    public class GetQuizTemplateQueryHandler
        : IRequestHandler<GetQuizTemplateQuery, QuizQuestionsTemplate>
    {
        public GetQuizTemplateQueryHandler()
        {
        }

        public async Task<QuizQuestionsTemplate> Handle(
            GetQuizTemplateQuery request,
            CancellationToken cancellationToken
        )
        {
            var csv = "Content,Points,AnswerExplanation,OptionA,OptionB,OptionC,OptionD,OptionE,OptionF,CorrectAnswer\n" +
                      "\"What is 2+2?\",10,\"Basic arithmetic\",\"3\",\"4\",\"5\",\"6\",\"\",\"\",\"B\"\n" +
                      "\"What is the capital of France?\",10,\"Paris is the capital and most populous city of France\",\"London\",\"Berlin\",\"Paris\",\"Madrid\",\"\",\"\",\"C\"";

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            var template = new QuizQuestionsTemplate
            {
                CsvFile = ByteString.CopyFrom(bytes),
                FileName = "quiz_questions_template.csv"
            };

            return await Task.FromResult(template);
        }
    }
}