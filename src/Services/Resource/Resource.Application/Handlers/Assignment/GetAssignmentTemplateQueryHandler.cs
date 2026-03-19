using Google.Protobuf;
using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Assignment;
using Resource.Application.Specifications.Assignments;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Assignment
{
    public class GetAssignmentTemplateQueryHandler
        : IRequestHandler<GetAssignmentTemplateQuery, AssignmentQuestionsTemplate>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetAssignmentTemplateQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AssignmentQuestionsTemplate> Handle(
            GetAssignmentTemplateQuery request,
            CancellationToken cancellationToken
        )
        {
            var csv = "Content,Points,AnswerExplanation,CriterionA,CriterionAMaxPoints,CriterionB,CriterionBMaxPoints,CriterionC,CriterionCMaxPoints,CriterionD,CriterionDMaxPoints,CriterionE,CriterionEMaxPoints,CriterionF,CriterionFMaxPoints\n" +
                     "\"Write an essay about climate change\",100,\"Focus on causes and effects\",\"Thesis Statement\",20,\"Evidence\",30,\"Analysis\",30,\"Conclusion\",20,\"\",\"\",\"\",\"\"\n" +
                     "\"Submit your project proposal\",50,\"Include timeline and budget\",\"Clarity\",15,\"Feasibility\",20,\"Budget\",15,\"\",\"\",\"\",\"\",\"\",\"\"";

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

            var template = new AssignmentQuestionsTemplate
            {
                CsvFile = ByteString.CopyFrom(bytes),
                FileName = "assignment_questions_template.csv"
            };

            return await Task.FromResult(template);
        }
    }
}
