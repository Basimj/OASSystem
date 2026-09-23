using FluentValidation; namespace OAS.Application.Accounting.Customers.Commands.SetCustomerStatus;
public sealed class SetCustomerStatusCommandValidator:AbstractValidator<SetCustomerStatusCommand>{public SetCustomerStatusCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.RowVersion).NotEmpty();}}
