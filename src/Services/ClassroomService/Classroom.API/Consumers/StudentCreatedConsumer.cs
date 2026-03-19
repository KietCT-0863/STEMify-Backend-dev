using EventBus.Messages;
using MassTransit;
using MediatR;

namespace Classroom.API.Consumers
{
    public class StudentCreatedConsumer : IConsumer<StudentCreatedEvent>
    {
        private readonly IMediator _mediator;
        private readonly ILogger<StudentCreatedConsumer> _logger;

        public StudentCreatedConsumer(IMediator mediator, ILogger<StudentCreatedConsumer> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StudentCreatedEvent> context)
        {
            //_logger.LogInformation(
            //    "StudentCreatedConsumer: Consuming StudentCreatedEvent for student with ID {StudentId}",
            //    context.Message.Id
            //);
            //var student = context.Message.ToCreateStudentCommand();
            //await _mediator.Send(student, context.CancellationToken);
        }
    }
}
