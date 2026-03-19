namespace Contracts.Domains
{
    public abstract class EntityBase<TKey> : Interfaces.EntityBase<TKey>
    {
        public TKey Id { get; set; }
    }
}
