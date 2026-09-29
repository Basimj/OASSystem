using OAS.Application.CRUD.Mapping;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.CustomerOrders.Mapping;

public sealed class CustomerOrderMapper : ICrudMapper<CustomerOrder, Guid, CustomerOrderDto, CreateCustomerOrderRequest, UpdateCustomerOrderRequest>
{
    public CustomerOrder Create(CreateCustomerOrderRequest source) =>
        throw new NotSupportedException("CustomerOrder creation requires customer/currency/product resolution and must use CreateCustomerOrderCommand.");

    public void Update(UpdateCustomerOrderRequest source, CustomerOrder destination) =>
        throw new NotSupportedException("CustomerOrder update requires aggregate line synchronization and must use UpdateCustomerOrderCommand.");

    public CustomerOrderDto ToRead(CustomerOrder source) => SalesContractMapping.Order(source);
}
