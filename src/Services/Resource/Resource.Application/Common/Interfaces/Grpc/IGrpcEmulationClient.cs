using Emulator.API.Protos;

namespace Resource.Application.Common.Interfaces.Grpc
{
    public interface IGrpcEmulationClient
    {
        Task<EmulationDetailResponse> GetEmulationByIdAsync(string id);
    }
}
