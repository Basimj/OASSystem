namespace OAS.Contracts.Common.Base;

public interface IEntityDto<TKey> where TKey : notnull
{
    TKey Id { get; }
}
