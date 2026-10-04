using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OAS.Application.Features.Employees.Payroll;
using OAS.Contracts.Features.Employees.Payroll;

namespace OAS.API.Features.Employees.Controllers;

[ApiController]
[Authorize]
[Route("api/employees/payroll")]
public sealed class PayrollController(ISender sender) : ControllerBase
{
    [HttpGet("policies")] public async Task<ActionResult<IReadOnlyList<PayrollPolicyDto>>> Policies(CancellationToken ct)=>Ok(await sender.Send(new GetPayrollPoliciesQuery(),ct));
    [HttpPost("policies")] public async Task<ActionResult<Guid>> CreatePolicy(CreatePayrollPolicyRequest r,CancellationToken ct)=>Ok(await sender.Send(new CreatePayrollPolicyCommand(r),ct));
    [HttpPut("policies/{id:guid}")] public async Task<ActionResult<Guid>> UpdatePolicy(Guid id,UpdatePayrollPolicyRequest r,CancellationToken ct)=>Ok(await sender.Send(new UpdatePayrollPolicyCommand(id,r),ct));
    [HttpPost("policies/{id:guid}/activate")] public async Task<ActionResult<Guid>> ActivatePolicy(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new ActivatePayrollPolicyCommand(id,r),ct));
    [HttpPost("policies/{id:guid}/cancel")] public async Task<ActionResult<Guid>> CancelPolicy(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new CancelPayrollPolicyCommand(id,r),ct));

    [HttpGet("periods")] public async Task<ActionResult<IReadOnlyList<PayrollPeriodDto>>> Periods(CancellationToken ct)=>Ok(await sender.Send(new GetPayrollPeriodsQuery(),ct));
    [HttpPost("periods")] public async Task<ActionResult<Guid>> CreatePeriod(CreatePayrollPeriodRequest r,CancellationToken ct)=>Ok(await sender.Send(new CreatePayrollPeriodCommand(r),ct));
    [HttpPost("periods/{id:guid}/lock")] public async Task<ActionResult<Guid>> LockPeriod(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new LockPayrollPeriodCommand(id,r),ct));
    [HttpPost("periods/{id:guid}/reopen")] public async Task<ActionResult<Guid>> ReopenPeriod(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new ReopenPayrollPeriodCommand(id,r),ct));
    [HttpPost("periods/{id:guid}/close")] public async Task<ActionResult<Guid>> ClosePeriod(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new ClosePayrollPeriodCommand(id,r),ct));

    [HttpGet("runs")] public async Task<ActionResult<IReadOnlyList<PayrollRunDto>>> Runs([FromQuery]Guid? periodId,[FromQuery]byte? status,CancellationToken ct)=>Ok(await sender.Send(new GetPayrollRunsQuery(periodId,status),ct));
    [HttpPost("runs")] public async Task<ActionResult<Guid>> CreateRun(CreatePayrollRunRequest r,CancellationToken ct)=>Ok(await sender.Send(new CreatePayrollRunCommand(r),ct));
    [HttpGet("runs/{id:guid}/prevalidation")] public async Task<ActionResult<PayrollPrevalidationDto>> Prevalidation(Guid id,CancellationToken ct)=>Ok(await sender.Send(new GetPayrollPrevalidationQuery(id),ct));
    [HttpPost("runs/{id:guid}/calculate")] public async Task<ActionResult<Guid>> Calculate(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new CalculatePayrollRunCommand(id,r),ct));
    [HttpPost("runs/{id:guid}/review")] public async Task<ActionResult<Guid>> ReviewRun(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new ReviewPayrollRunCommand(id,r),ct));
    [HttpPost("runs/{id:guid}/approve")] public async Task<ActionResult<Guid>> ApproveRun(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new ApprovePayrollRunCommand(id,r),ct));
    [HttpPost("runs/{id:guid}/reopen")] public async Task<ActionResult<Guid>> ReopenRun(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new ReopenPayrollRunCommand(id,r),ct));
    [HttpPost("runs/{id:guid}/post")] public async Task<ActionResult<Guid>> PostRun(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new PostPayrollRunCommand(id,r),ct));
    [HttpPost("runs/{id:guid}/close")] public async Task<ActionResult<Guid>> CloseRun(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new ClosePayrollRunCommand(id,r),ct));
    [HttpPost("runs/{id:guid}/cancel")] public async Task<ActionResult<Guid>> CancelRun(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new CancelPayrollRunCommand(id,r),ct));
    [HttpGet("runs/{id:guid}/employees")] public async Task<ActionResult<IReadOnlyList<EmployeePayrollDto>>> Employees(Guid id,CancellationToken ct)=>Ok(await sender.Send(new GetEmployeePayrollsQuery(id,null),ct));

    [HttpGet("{id:guid}")] public async Task<ActionResult<EmployeePayrollDto>> EmployeePayroll(Guid id,CancellationToken ct)=>Ok(await sender.Send(new GetEmployeePayrollByIdQuery(id),ct));
    [HttpPost("{id:guid}/review")] public async Task<ActionResult<Guid>> ReviewEmployee(Guid id,PayrollLifecycleRequest r,CancellationToken ct)=>Ok(await sender.Send(new ReviewEmployeePayrollCommand(id,r),ct));
    [HttpPost("{id:guid}/payments")] public async Task<ActionResult<SalaryPaymentResultDto>> Pay(Guid id,SalaryPaymentRequest r,CancellationToken ct)=>Ok(await sender.Send(new PayEmployeePayrollCommand(id,r),ct));
    [HttpGet("{id:guid}/payslip")] public async Task<ActionResult<PayslipDto>> Payslip(Guid id,CancellationToken ct)=>Ok(await sender.Send(new GetPayslipQuery(id),ct));
    [HttpGet("employee/{employeeId:guid}")] public async Task<ActionResult<IReadOnlyList<EmployeePayrollDto>>> EmployeeHistory(Guid employeeId,CancellationToken ct)=>Ok(await sender.Send(new GetEmployeePayrollsQuery(null,employeeId),ct));
}
