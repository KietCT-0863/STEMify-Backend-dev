namespace BuildingBlocks.Authorization.Services;

public interface IGrpcContextService
{
    Dictionary<string, string> GetGrpcMetadata();

    Dictionary<string, string> GetGrpcMetadata(
        int organizationId,
        int subscriptionId,
        IEnumerable<string> permissions);

    (int OrganizationId, int SubscriptionId)? GetOrganizationContext();

    Task<IEnumerable<string>> GetCurrentPermissionsAsync();
}
