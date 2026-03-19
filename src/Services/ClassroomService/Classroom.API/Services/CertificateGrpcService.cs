using Classroom.Application.Features.Certificates.Queries.GetCertificateById;
using Classroom.Application.Features.Certificates.Queries.GetCertificateList;
using Grpc.Core;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.API.Services
{
    public class CertificateGrpcService : GrpcCertificate.GrpcCertificateBase
    {
        private readonly IMediator _mediator;

        public CertificateGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        //public override async Task<GrpcCertificateResponse> CreateCertificate(
        //    CreateCertificateRequest request,
        //    ServerCallContext context
        //)
        //{
        //    try
        //    {
        //        var command = new CreateCertificateCommand
        //        {
        //            CertificateType = request.CertificateType,
        //            CourseEnrollmentId = request.CourseEnrollmentId,
        //            CurriculumEnrollmentId = request.CurriculumEnrollmentId,
        //            UserId = Guid.Parse(request.UserId),
        //        };

        //        var result = await _mediator.Send(command);
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new RpcException(
        //            new Status(StatusCode.Internal, $"CreateCertificate failed: {ex.Message}")
        //        );
        //    }
        //}

        public override async Task<GrpcCertificateDetail> GetCertificateById(
            GetCertificateRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var query = new GetCertificateByIdQuery(request.Id);
                var result = await _mediator.Send(query);

                if (result == null)
                    throw new RpcException(
                        new Status(StatusCode.NotFound, $"Certificate with ID {request.Id} not found.")
                    );

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"GetCertificate failed: {ex.Message}")
                );
            }
        }

        //public override async Task<DeleteCertificateResponse> DeleteCertificate(
        //    DeleteCertificateRequest request,
        //    ServerCallContext context
        //)
        //{
        //    try
        //    {
        //        var command = new DeleteCertificateCommand(request.Id);
        //        await _mediator.Send(command);

        //        return new DeleteCertificateResponse() { Success = true };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new RpcException(
        //            new Status(StatusCode.Internal, $"DeleteCertificate failed: {ex.Message}")
        //        );
        //    }
        //}

        public override async Task<GrpcPagedCertificatesResponse> GetPagedCertificates(
            GetCertificatesRequest request,
            ServerCallContext context
        )
        {
            try
            {
                Classroom.Domain.Enums.CertificateType? type = null;
                if (!string.IsNullOrWhiteSpace(request.CertificateType))
                {
                    if (
                        System.Enum.TryParse<Classroom.Domain.Enums.CertificateType>(
                            request.CertificateType,
                            true,
                            out var parsedStatus
                        )
                    )
                    {
                        type = parsedStatus;
                    }
                }
                var query = new GetCertificateListQuery(
                    new Classroom.Application.Specifications.Certificates.CertificateParams
                    {
                        Search = request.Search,
                        PageNumber = request.PageNumber,
                        PageSize = request.PageSize,
                        OrderBy = request.OrderBy,
                        UserId = request.UserId,
                        Type = type,
                        CourseEnrollmentId = request.CourseEnrollmentId,
                        CurriculumEnrollmentId = request.CurriculumEnrollmentId,
                    }
                );
                var result = await _mediator.Send(query);
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"QueryCertificates failed: {ex.Message}")
                );
            }
        }
    }
}
