using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.EndOfService;
using OAS.Contracts.Features.Employees.Payroll;

namespace OAS.Client.Features.Employees.Services;

public interface IPayrollClientService
{
    Task<IReadOnlyList<PayrollPolicyDto>> GetPoliciesAsync(CancellationToken ct = default);
    Task<ApiCallResult<Guid>> CreatePolicyAsync(CreatePayrollPolicyRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> UpdatePolicyAsync(Guid id, UpdatePayrollPolicyRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> ActivatePolicyAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> CancelPolicyAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PayrollPeriodDto>> GetPeriodsAsync(CancellationToken ct = default);
    Task<ApiCallResult<Guid>> CreatePeriodAsync(CreatePayrollPeriodRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> LockPeriodAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> ReopenPeriodAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> ClosePeriodAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PayrollRunDto>> GetRunsAsync(Guid? periodId = null, byte? status = null, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> CreateRunAsync(CreatePayrollRunRequest request, CancellationToken ct = default);
    Task<PayrollPrevalidationDto?> GetPrevalidationAsync(Guid runId, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> CalculateRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> ReviewRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> ApproveRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> ReopenRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> PostRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> CloseRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> CancelRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<EmployeePayrollDto>> GetRunEmployeesAsync(Guid runId, CancellationToken ct = default);
    Task<EmployeePayrollDto?> GetEmployeePayrollAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeePayrollDto>> GetEmployeeHistoryAsync(Guid employeeId, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> ReviewEmployeePayrollAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default);
    Task<ApiCallResult<SalaryPaymentResultDto>> PayEmployeePayrollAsync(Guid id, SalaryPaymentRequest request, CancellationToken ct = default);
    Task<PayslipDto?> GetPayslipAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<EndOfServiceSettlementDto>> GetEndOfServiceAsync(Guid? employeeId = null, CancellationToken ct = default);
    Task<EndOfServiceSettlementDto?> GetEndOfServiceAsync(Guid id, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> CreateEndOfServiceAsync(CreateEndOfServiceSettlementRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> CalculateEndOfServiceAsync(Guid id, CalculateEndOfServiceRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> ReviewEndOfServiceAsync(Guid id, EndOfServiceTransitionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> ApproveEndOfServiceAsync(Guid id, EndOfServiceTransitionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> PostEndOfServiceAsync(Guid id, EndOfServiceTransitionRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> PayEndOfServiceAsync(Guid id, EndOfServicePaymentRequest request, CancellationToken ct = default);
    Task<ApiCallResult<Guid>> CompleteEndOfServiceAsync(Guid id, EndOfServiceTransitionRequest request, CancellationToken ct = default);
}
