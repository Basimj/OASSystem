using Microsoft.AspNetCore.Components.Web;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Features.Employees.Adjustments;
using OAS.Contracts.Features.Employees.Leave;
using OAS.Contracts.Features.Employees.Loans;
using OAS.Contracts.Features.Employees.Overtime;
using OAS.Contracts.Features.Employees.Time;

namespace OAS.Client.Features.Employees.Pages.HrOperations;

public partial class HrOperations
{
    private async Task SubmitAsync(MouseEventArgs _)
    {
        if (!CanSubmit) return;
        _saving = true;
        try
        {
            var ok = _section switch
            {
                "attendance" => Handle(await Hr.SubmitAttendanceAsync(_selectedAttendance!.Id, new AttendanceTransitionRequest(_selectedAttendance.RowVersion))),
                "leave-requests" => Handle(await Hr.SubmitLeaveAsync(_selectedLeaveRequest!.Id, new LeaveTransitionRequest(_selectedLeaveRequest.RowVersion))),
                "overtime" => Handle(await Hr.SubmitOvertimeAsync(_selectedOvertime!.Id, new OvertimeTransitionRequest(_selectedOvertime.RowVersion))),
                "loans" => Handle(await Hr.SubmitLoanAsync(_selectedLoan!.Id, new EmployeeLoanTransitionRequest(_selectedLoan.RowVersion))),
                "adjustments" => Handle(await Hr.SubmitAdjustmentAsync(_selectedAdjustment!.Id, new EmployeeAdjustmentTransitionRequest(_selectedAdjustment.RowVersion))),
                _ => false
            };
            if (ok) { Snackbar.Success("تم إرسال السجل للاعتماد."); await LoadCurrentSectionAsync(); }
        }
        finally { _saving = false; }
    }

    private async Task ApproveAsync(MouseEventArgs _)
    {
        if (!CanApprove) return;
        _saving = true;
        try
        {
            var ok = _section switch
            {
                "attendance" => Handle(await Hr.ApproveAttendanceAsync(_selectedAttendance!.Id, new AttendanceTransitionRequest(_selectedAttendance.RowVersion))),
                "leave-requests" => Handle(await Hr.ApproveLeaveAsync(_selectedLeaveRequest!.Id, new LeaveTransitionRequest(_selectedLeaveRequest.RowVersion, ApprovedDays: _selectedLeaveRequest.RequestedDays))),
                "overtime" => Handle(await Hr.ApproveOvertimeAsync(_selectedOvertime!.Id, new OvertimeTransitionRequest(_selectedOvertime.RowVersion, ApprovedMinutes: _selectedOvertime.RequestedMinutes))),
                "loans" => Handle(await Hr.ApproveLoanAsync(_selectedLoan!.Id, new EmployeeLoanTransitionRequest(_selectedLoan.RowVersion))),
                "adjustments" => Handle(await Hr.ApproveAdjustmentAsync(_selectedAdjustment!.Id, new EmployeeAdjustmentTransitionRequest(_selectedAdjustment.RowVersion))),
                _ => false
            };
            if (ok) { Snackbar.Success("تم اعتماد السجل بنجاح."); await LoadCurrentSectionAsync(); }
        }
        finally { _saving = false; }
    }

    private async Task RejectAsync(MouseEventArgs _)
    {
        if (!CanReject) return;
        _saving = true;
        try
        {
            const string reason = "تم الرفض من شاشة شؤون الموظفين.";
            var ok = _section switch
            {
                "attendance" => Handle(await Hr.RejectAttendanceAsync(_selectedAttendance!.Id, new AttendanceTransitionRequest(_selectedAttendance.RowVersion, reason))),
                "leave-requests" => Handle(await Hr.RejectLeaveAsync(_selectedLeaveRequest!.Id, new LeaveTransitionRequest(_selectedLeaveRequest.RowVersion, reason))),
                "overtime" => Handle(await Hr.RejectOvertimeAsync(_selectedOvertime!.Id, new OvertimeTransitionRequest(_selectedOvertime.RowVersion, reason))),
                "loans" => Handle(await Hr.RejectLoanAsync(_selectedLoan!.Id, new EmployeeLoanTransitionRequest(_selectedLoan.RowVersion, reason))),
                "adjustments" => Handle(await Hr.RejectAdjustmentAsync(_selectedAdjustment!.Id, new EmployeeAdjustmentTransitionRequest(_selectedAdjustment.RowVersion, reason))),
                _ => false
            };
            if (ok) { Snackbar.Success("تم رفض السجل."); await LoadCurrentSectionAsync(); }
        }
        finally { _saving = false; }
    }

    private async Task CancelRecordAsync(MouseEventArgs _)
    {
        if (!CanCancelRecord) return;
        _saving = true;
        try
        {
            const string reason = "إلغاء من شاشة شؤون الموظفين.";
            var ok = _section switch
            {
                "leave-requests" => Handle(await Hr.CancelLeaveAsync(_selectedLeaveRequest!.Id, new LeaveTransitionRequest(_selectedLeaveRequest.RowVersion, reason))),
                "overtime" => Handle(await Hr.CancelOvertimeAsync(_selectedOvertime!.Id, new OvertimeTransitionRequest(_selectedOvertime.RowVersion, reason))),
                "loans" => Handle(await Hr.CancelLoanAsync(_selectedLoan!.Id, new EmployeeLoanTransitionRequest(_selectedLoan.RowVersion, reason))),
                "adjustments" => Handle(await Hr.CancelAdjustmentAsync(_selectedAdjustment!.Id, new EmployeeAdjustmentTransitionRequest(_selectedAdjustment.RowVersion, reason))),
                _ => false
            };
            if (ok) { Snackbar.Success("تم إلغاء السجل."); await LoadCurrentSectionAsync(); }
        }
        finally { _saving = false; }
    }

    private async Task GenerateAttendanceAsync(MouseEventArgs _)
    {
        if (!CanGenerateAttendance || _attendanceFrom is not DateOnly from || _attendanceTo is not DateOnly to) { Snackbar.Error("حدد فترة الحضور أولًا."); return; }
        _saving = true;
        try
        {
            var result = await Hr.GenerateAttendanceAsync(new GenerateAttendanceRequest(from, to));
            if (!Handle(result)) return;
            Snackbar.Success($"تمت تهيئة {result.Value} سجل حضور.");
            await LoadCurrentSectionAsync();
        }
        finally { _saving = false; }
    }

    private async Task DisburseLoanAsync(MouseEventArgs _)
    {
        if (!CanDisburseLoan || _selectedLoan is null) return;
        if (!TryPaymentSelection(out var method, out var cashId, out var bankId)) return;
        _saving = true;
        try
        {
            var result = await Hr.DisburseLoanAsync(_selectedLoan.Id, new DisburseEmployeeLoanRequest(
                DateOnly.FromDateTime(DateTime.Today), method, cashId, bankId, null, null, ExchangeRateType.Accounting,
                ReferenceNumber: _selectedLoan.LoanCode,
                Description: $"صرف سلفة الموظف {_selectedLoan.EmployeeName}",
                RowVersion: _selectedLoan.RowVersion));
            if (!Handle(result)) return;
            Snackbar.Success("تم صرف السلفة وترحيل سند الصرف بنجاح.");
            await LoadCurrentSectionAsync();
        }
        finally { _saving = false; }
    }

    private async Task PayNextLoanInstallmentAsync(MouseEventArgs _)
    {
        if (!CanPayNextLoanInstallment || _selectedLoan is null) return;
        var installment = _selectedLoan.Installments.Where(x => x.Status == 2).OrderBy(x => x.InstallmentSequence).FirstOrDefault();
        if (installment is null) return;
        if (!TryPaymentSelection(out var method, out var cashId, out var bankId)) return;
        _saving = true;
        try
        {
            var result = await Hr.PayInstallmentExternallyAsync(installment.Id, new PayLoanInstallmentExternallyRequest(
                DateOnly.FromDateTime(DateTime.Today), method, cashId, bankId, null, null, ExchangeRateType.Accounting,
                ReferenceNumber: $"{_selectedLoan.LoanCode}/{installment.InstallmentSequence}",
                Description: $"سداد قسط سلفة الموظف {_selectedLoan.EmployeeName}",
                RowVersion: installment.RowVersion));
            if (!Handle(result)) return;
            Snackbar.Success("تم سداد القسط وترحيل سند القبض بنجاح.");
            var refreshed = await Hr.GetLoanAsync(_selectedLoan.Id);
            if (refreshed is not null) { _selectedLoan = refreshed; LoadLoanForm(refreshed); }
            _loans = await Hr.GetLoansAsync();
        }
        finally { _saving = false; }
    }

    private bool TryPaymentSelection(out PaymentMethod method, out Guid? cashId, out Guid? bankId)
    {
        cashId = null; bankId = null;
        if (!byte.TryParse(_loanPaymentMethodText, out var raw) || !Enum.IsDefined(typeof(PaymentMethod), raw)) { method = default; Snackbar.Error("اختر طريقة الدفع."); return false; }
        method = (PaymentMethod)raw;
        if (method == PaymentMethod.Cash)
        {
            if (!Guid.TryParse(_loanCashAccountValue, out var id)) { Snackbar.Error("اختر حساب الصندوق."); return false; }
            cashId = id;
        }
        else
        {
            if (!Guid.TryParse(_loanBankAccountValue, out var id)) { Snackbar.Error("اختر الحساب البنكي."); return false; }
            bankId = id;
        }
        return true;
    }
}
