namespace Contracts.Domains.Interfaces
{
    public interface EntityBase<T>
    {
        T Id { get; set; }
    }
}
