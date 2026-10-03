using Microsoft.AspNetCore.Components;
using OAS.Client.Features.Employees.Services;
using OAS.Contracts.Features.Employees.Adjustments;
using OAS.Contracts.Features.Employees.Leave;
using OAS.Contracts.Features.Employees.Loans;
using OAS.Contracts.Features.Employees.Time;
using OAS.Contracts.Features.Employees.Payroll;

namespace OAS.Client.Features.Employees.Components;

public partial class EmployeeHrSummaryPanel
{
    [Inject] private IHrOperationsClientService Hr { get; set; } = default!;
    [Inject] private IPayrollClientService Payrolls { get; set; } = default!;
    [Parameter, EditorRequired] public Guid EmployeeId { get; set; }
    [Parameter, EditorRequired] public string Section { get; set; } = string.Empty;

    private bool _loading;
    private Guid _loadedEmployeeId;
    private string? _loadedSection;
    private IReadOnlyList<EmployeeShiftAssignmentDto> _assignments = [];
    private IReadOnlyList<AttendanceRecordDto> _attendance = [];
    private IReadOnlyList<EmployeeLeaveBalanceDto> _balances = [];
    private IReadOnlyList<LeaveRequestDto> _leaveRequests = [];
    private IReadOnlyList<EmployeeLoanDto> _loans = [];
    private IReadOnlyList<EmployeeAdjustmentDto> _adjustments = [];
    private IReadOnlyList<EmployeePayrollDto> _payrollHistory = [];

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedEmployeeId == EmployeeId && string.Equals(_loadedSection, Section, StringComparison.Ordinal)) return;
        _loadedEmployeeId = EmployeeId;
        _loadedSection = Section;
        await LoadAsync();
    }

    public async Task RefreshAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            switch (Section)
            {
                case "time":
                    _assignments = await Hr.GetShiftAssignmentsAsync(EmployeeId);
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    _attendance = await Hr.GetAttendanceAsync(today.AddDays(-45), today, EmployeeId);
                    break;
                case "leave":
                    _balances = await Hr.GetLeaveBalancesAsync(EmployeeId);
                    _leaveRequests = await Hr.GetLeaveRequestsAsync(EmployeeId);
                    break;
                case "loans":
                    _loans = await Hr.GetLoansAsync(EmployeeId);
                    break;
                case "adjustments":
                    _adjustments = await Hr.GetAdjustmentsAsync(EmployeeId);
                    break;
                case "payroll":
                    _payrollHistory = await Payrolls.GetEmployeeHistoryAsync(EmployeeId);
                    break;
            }
        }
        finally { _loading = false; }
    }

    private static string StatusName(byte status) => status switch { 1 => "مسودة", 2 => "بانتظار الاعتماد", 3 => "معتمد", 4 => "مرفوض", 5 => "مطبق", 6 => "ملغي", _ => "غير معروف" };
    private static string LoanStatusName(byte status) => status switch { 1 => "مسودة", 2 => "بانتظار الاعتماد", 3 => "معتمدة", 4 => "نشطة", 5 => "مكتملة", 6 => "مرفوضة", 7 => "ملغاة", _ => "غير معروف" };
    private static string PayrollStatusName(byte status) => status switch { 1 => "محتسب", 2 => "مراجع", 3 => "معتمد", 4 => "مرحل", 5 => "ملغي", _ => "غير معروف" };
    private static string PaymentStatusName(string status) => status switch { "Paid" => "مدفوع", "PartiallyPaid" => "مدفوع جزئيًا", _ => "غير مدفوع" };
}
