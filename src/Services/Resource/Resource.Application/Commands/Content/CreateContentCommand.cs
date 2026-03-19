using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Content
{
    public class CreateContentCommand : IRequest<ContentResponse>
    {
        public Domain.Enums.ContentType ContentType { get; set; }
        public string ContentBody { get; set; }
        public string FileName { get; set; }
        public byte[] FileBytes { get; set; }
        public int SectionId { get; set; }
    }

    public class CreateContentCommandValidator : AbstractValidator<CreateContentCommand>
    {
        public CreateContentCommandValidator()
        {
            RuleFor(x => x.ContentType)
                .IsInEnum()
                .WithMessage("ContentType must be a valid enum value.");

            RuleFor(x => x.ContentBody).NotEmpty().WithMessage("ContentBody is required.");

            RuleFor(x => x.SectionId)
                .GreaterThan(0)
                .WithMessage("SectionId must be greater than 0.");

            RuleFor(x => x)
                .Custom(
                    (command, context) =>
                    {
                        var ext = !string.IsNullOrWhiteSpace(command.FileName)
                            ? Path.GetExtension(command.FileName.Trim())?.ToLowerInvariant()
                            : null;

                        //if (command.ContentType == ContentType.Text)
                        //{
                        //    if (
                        //        !string.IsNullOrEmpty(command.FileName)
                        //        || (command.FileBytes != null && command.FileBytes.Length > 0)
                        //    )
                        //    {
                        //        context.AddFailure("For Text content, no file should be uploaded.");
                        //    }
                        //}
                        //else if (command.ContentType == ContentType.Video)
                        //{
                        //    if (
                        //        string.IsNullOrEmpty(command.FileName)
                        //        || command.FileBytes == null
                        //        || command.FileBytes.Length == 0
                        //    )
                        //    {
                        //        context.AddFailure("For Video content, a file must be uploaded.");
                        //    }
                        //    else if (
                        //        ext != ".mp4"
                        //        && ext != ".avi"
                        //        && ext != ".mov"
                        //        && ext != ".mkv"
                        //    )
                        //    {
                        //        context.AddFailure(
                        //            "Video content must be a common video file (.mp4, .avi, .mov, .mkv)."
                        //        );
                        //    }
                        //}
                        //else if (command.ContentType == ContentType.Document)
                        //{
                        //    if (
                        //        string.IsNullOrEmpty(command.FileName)
                        //        || command.FileBytes == null
                        //        || command.FileBytes.Length == 0
                        //    )
                        //    {
                        //        context.AddFailure(
                        //            "For Document content, a file must be uploaded."
                        //        );
                        //    }
                        //    else if (
                        //        ext != ".pdf"
                        //        && ext != ".doc"
                        //        && ext != ".docx"
                        //        && ext != ".ppt"
                        //        && ext != ".pptx"
                        //    )
                        //    {
                        //        context.AddFailure(
                        //            "Document content must be a document file (.pdf, .doc, .docx, .ppt, .pptx)."
                        //        );
                        //    }
                        //}
                    }
                );
        }
    }
}
