using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.EndOfService;
using OAS.Contracts.Features.Employees.Payroll;

namespace OAS.Client.Features.Employees.Services;

public sealed class PayrollClientService(OasApiClient api) : IPayrollClientService
{
    private const string Root = "api/employees/payroll";

    public async Task<IReadOnlyList<PayrollPolicyDto>> GetPoliciesAsync(CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<PayrollPolicyDto>>($"{Root}/policies", ct) ?? [];
    public Task<ApiCallResult<Guid>> CreatePolicyAsync(CreatePayrollPolicyRequest request, CancellationToken ct = default) => api.PostResultAsync<CreatePayrollPolicyRequest, Guid>($"{Root}/policies", request, ct);
    public Task<ApiCallResult<Guid>> UpdatePolicyAsync(Guid id, UpdatePayrollPolicyRequest request, CancellationToken ct = default) => api.PutResultAsync<UpdatePayrollPolicyRequest, Guid>($"{Root}/policies/{id:D}", request, ct);
    public Task<ApiCallResult<Guid>> ActivatePolicyAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default) => api.PostResultAsync<PayrollLifecycleRequest, Guid>($"{Root}/policies/{id:D}/activate", request, ct);
    public Task<ApiCallResult<Guid>> CancelPolicyAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default) => api.PostResultAsync<PayrollLifecycleRequest, Guid>($"{Root}/policies/{id:D}/cancel", request, ct);

    public async Task<IReadOnlyList<PayrollPeriodDto>> GetPeriodsAsync(CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<PayrollPeriodDto>>($"{Root}/periods", ct) ?? [];
    public Task<ApiCallResult<Guid>> CreatePeriodAsync(CreatePayrollPeriodRequest request, CancellationToken ct = default) => api.PostResultAsync<CreatePayrollPeriodRequest, Guid>($"{Root}/periods", request, ct);
    public Task<ApiCallResult<Guid>> LockPeriodAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default) => api.PostResultAsync<PayrollLifecycleRequest, Guid>($"{Root}/periods/{id:D}/lock", request, ct);
    public Task<ApiCallResult<Guid>> ReopenPeriodAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default) => api.PostResultAsync<PayrollLifecycleRequest, Guid>($"{Root}/periods/{id:D}/reopen", request, ct);
    public Task<ApiCallResult<Guid>> ClosePeriodAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default) => api.PostResultAsync<PayrollLifecycleRequest, Guid>($"{Root}/periods/{id:D}/close", request, ct);

    public async Task<IReadOnlyList<PayrollRunDto>> GetRunsAsync(Guid? periodId = null, byte? status = null, CancellationToken ct = default)
    {
        var q = new List<string>();
        if (periodId.HasValue) q.Add($"periodId={periodId:D}");
        if (status.HasValue) q.Add($"status={status.Value}");
        var uri = $"{Root}/runs" + (q.Count == 0 ? string.Empty : "?" + string.Join('&', q));
        return await api.GetAsync<IReadOnlyList<PayrollRunDto>>(uri, ct) ?? [];
    }
    public Task<ApiCallResult<Guid>> CreateRunAsync(CreatePayrollRunRequest request, CancellationToken ct = default) => api.PostResultAsync<CreatePayrollRunRequest, Guid>($"{Root}/runs", request, ct);
    public Task<PayrollPrevalidationDto?> GetPrevalidationAsync(Guid runId, CancellationToken ct = default) => api.GetAsync<PayrollPrevalidationDto>($"{Root}/runs/{runId:D}/prevalidation", ct);
    public Task<ApiCallResult<Guid>> CalculateRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default) => TransitionRun(runId, "calculate", request, ct);
    public Task<ApiCallResult<Guid>> ReviewRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default) => TransitionRun(runId, "review", request, ct);
    public Task<ApiCallResult<Guid>> ApproveRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default) => TransitionRun(runId, "approve", request, ct);
    public Task<ApiCallResult<Guid>> ReopenRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default) => TransitionRun(runId, "reopen", request, ct);
    public Task<ApiCallResult<Guid>> PostRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default) => TransitionRun(runId, "post", request, ct);
    public Task<ApiCallResult<Guid>> CloseRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default) => TransitionRun(runId, "close", request, ct);
    public Task<ApiCallResult<Guid>> CancelRunAsync(Guid runId, PayrollLifecycleRequest request, CancellationToken ct = default) => TransitionRun(runId, "cancel", request, ct);

    public async Task<IReadOnlyList<EmployeePayrollDto>> GetRunEmployeesAsync(Guid runId, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<EmployeePayrollDto>>($"{Root}/runs/{runId:D}/employees", ct) ?? [];
    public Task<EmployeePayrollDto?> GetEmployeePayrollAsync(Guid id, CancellationToken ct = default) => api.GetAsync<EmployeePayrollDto>($"{Root}/{id:D}", ct);
    public async Task<IReadOnlyList<EmployeePayrollDto>> GetEmployeeHistoryAsync(Guid employeeId, CancellationToken ct = default) => await api.GetAsync<IReadOnlyList<EmployeePayrollDto>>($"{Root}/employee/{employeeId:D}", ct) ?? [];
    public Task<ApiCallResult<Guid>> ReviewEmployeePayrollAsync(Guid id, PayrollLifecycleRequest request, CancellationToken ct = default) => api.PostResultAsync<PayrollLifecycleRequest, Guid>($"{Root}/{id:D}/review", request, ct);
    public Task<ApiCallResult<SalaryPaymentResultDto>> PayEmployeePayrollAsync(Guid id, SalaryPaymentRequest request, CancellationToken ct = default) => api.PostResultAsync<SalaryPaymentRequest, SalaryPaymentResultDto>($"{Root}/{id:D}/payments", request, ct);
    public Task<PayslipDto?> GetPayslipAsync(Guid id, CancellationToken ct = default) => api.GetAsync<PayslipDto>($"{Root}/{id:D}/payslip", ct);

    public async Task<IReadOnlyList<EndOfServiceSettlementDto>> GetEndOfServiceAsync(Guid? employeeId = null, CancellationToken ct = default)
    {
        var uri = "api/employees/end-of-service" + (employeeId.HasValue ? $"?employeeId={employeeId:D}" : string.Empty);
        return await api.GetAsync<IReadOnlyList<EndOfServiceSettlementDto>>(uri, ct) ?? [];
    }
    public Task<EndOfServiceSettlementDto?> GetEndOfServiceAsync(Guid id, CancellationToken ct = default) => api.GetAsync<EndOfServiceSettlementDto>($"api/employees/end-of-service/{id:D}", ct);
    public Task<ApiCallResult<Guid>> CreateEndOfServiceAsync(CreateEndOfServiceSettlementRequest request, CancellationToken ct = default) => api.PostResultAsync<CreateEndOfServiceSettlementRequest, Guid>("api/employees/end-of-service", request, ct);
    public Task<ApiCallResult<Guid>> CalculateEndOfServiceAsync(Guid id, CalculateEndOfServiceRequest request, CancellationToken ct = default) => api.PostResultAsync<CalculateEndOfServiceRequest, Guid>($"api/employees/end-of-service/{id:D}/calculate", request, ct);
    public Task<ApiCallResult<Guid>> ReviewEndOfServiceAsync(Guid id, EndOfServiceTransitionRequest request, CancellationToken ct = default) => EosTransition(id, "review", request, ct);
    public Task<ApiCallResult<Guid>> ApproveEndOfServiceAsync(Guid id, EndOfServiceTransitionRequest request, CancellationToken ct = default) => EosTransition(id, "approve", request, ct);
    public Task<ApiCallResult<Guid>> PostEndOfServiceAsync(Guid id, EndOfServiceTransitionRequest request, CancellationToken ct = default) => EosTransition(id, "post", request, ct);
    public Task<ApiCallResult<Guid>> PayEndOfServiceAsync(Guid id, EndOfServicePaymentRequest request, CancellationToken ct = default) => api.PostResultAsync<EndOfServicePaymentRequest, Guid>($"api/employees/end-of-service/{id:D}/payment", request, ct);
    public Task<ApiCallResult<Guid>> CompleteEndOfServiceAsync(Guid id, EndOfServiceTransitionRequest request, CancellationToken ct = default) => EosTransition(id, "complete", request, ct);

    private Task<ApiCallResult<Guid>> TransitionRun(Guid id, string action, PayrollLifecycleRequest request, CancellationToken ct) => api.PostResultAsync<PayrollLifecycleRequest, Guid>($"{Root}/runs/{id:D}/{action}", request, ct);
    private Task<ApiCallResult<Guid>> EosTransition(Guid id, string action, EndOfServiceTransitionRequest request, CancellationToken ct) => api.PostResultAsync<EndOfServiceTransitionRequest, Guid>($"api/employees/end-of-service/{id:D}/{action}", request, ct);
}
