namespace OAS.Contracts.Purchasing.CustomerDemand;

public sealed record AssignCustomerDemandSupplierRequest(
    Guid? PreferredSupplierId,
    string RowVersion);
