using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Commissions;
namespace OAS.Application.Sales.Commissions.Queries;
public sealed record GetCommissionRulesQuery(Guid? EmployeeId) : IQuery<IReadOnlyList<CommissionRuleDto>>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[SalesPermissions.View]; }
public sealed record GetCommissionStatementsQuery(Guid? EmployeeId, DateOnly? FromDate, DateOnly? ToDate) : IQuery<IReadOnlyList<CommissionStatementDto>>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[SalesPermissions.View]; }
public sealed record GetCommissionStatementQuery(Guid Id) : IQuery<CommissionStatementDto>, IAuthorizedRequest { public IReadOnlyCollection<string> RequiredPermissions { get; }=[SalesPermissions.View]; }
public sealed class GetCommissionRulesQueryHandler(ICommissionQueryService q):IRequestHandler<GetCommissionRulesQuery,IReadOnlyList<CommissionRuleDto>>{public Task<IReadOnlyList<CommissionRuleDto>> Handle(GetCommissionRulesQuery r,CancellationToken ct)=>q.GetRulesAsync(r.EmployeeId,ct);}
public sealed class GetCommissionStatementsQueryHandler(ICommissionQueryService q):IRequestHandler<GetCommissionStatementsQuery,IReadOnlyList<CommissionStatementDto>>{public Task<IReadOnlyList<CommissionStatementDto>> Handle(GetCommissionStatementsQuery r,CancellationToken ct)=>q.GetStatementsAsync(r.EmployeeId,r.FromDate,r.ToDate,ct);}
public sealed class GetCommissionStatementQueryHandler(ICommissionQueryService q):IRequestHandler<GetCommissionStatementQuery,CommissionStatementDto>{public async Task<CommissionStatementDto> Handle(GetCommissionStatementQuery r,CancellationToken ct)=>await q.GetStatementAsync(r.Id,ct)??throw new NotFoundException("CommissionStatement",r.Id);}
