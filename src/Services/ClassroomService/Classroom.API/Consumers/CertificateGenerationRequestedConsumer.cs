using Classroom.Application.Features.Certificates.Commands.CreateCertificate;
using EventBus.Messages;
using MassTransit;
using MediatR;
using ServiceStack;

namespace Classroom.API.Consumers
{
    public class CertificateGenerationRequestedConsumer : IConsumer<CertificateGenerationRequestedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CertificateGenerationRequestedConsumer> _logger;

        public CertificateGenerationRequestedConsumer(
            IMediator mediator,
            ILogger<CertificateGenerationRequestedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CertificateGenerationRequestedEvent> context)
        {
            var message = context.Message;

            _logger.LogInformation(
                "Received CertificateGenerationRequestedEvent for Enrollment {Id}",
                message.CourseEnrollmentId
            );

            var cmd = new CreateCertificateCommand
            {
                UserId = message.UserId,
                CertificateType = message.CertificateType.ToEnumOrDefault(Domain.Enums.CertificateType.Course),
                CourseEnrollmentId = message.CourseEnrollmentId,
                CurriculumEnrollmentId = message.CurriculumEnrollmentId,
            };

            await _mediator.Send(cmd);

            _logger.LogInformation(
                "Certificate generated for Enrollment {Id}",
                message.CourseEnrollmentId
            );
        }
    }
}
