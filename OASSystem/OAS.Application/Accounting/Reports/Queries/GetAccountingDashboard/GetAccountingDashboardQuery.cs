using OAS.Application.Abstractions.Messaging;
using OAS.Contracts.Accounting.Reports;

namespace OAS.Application.Accounting.Reports.Queries.GetAccountingDashboard;

public sealed record GetAccountingDashboardQuery : IQuery<AccountingDashboardDto>;
