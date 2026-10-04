namespace OAS.Domain.Features.Employees.Enums;

public enum AttendanceSource : byte { Manual = 1, Device = 2, Import = 3, System = 4 }
public enum AttendanceDayStatus : byte { Present = 1, Absent = 2, Leave = 3, Holiday = 4, Weekend = 5, Incomplete = 6, Unscheduled = 7 }
public enum AttendanceApprovalStatus : byte { Draft = 1, PendingApproval = 2, Approved = 3, Rejected = 4 }
public enum LeaveAccrualMethod : byte { AnnualGrant = 1, MonthlyAccrual = 2, Manual = 3 }
public enum LeaveDayCountingMethod : byte { WorkingDays = 1, CalendarDays = 2 }
public enum LeaveRequestStatus : byte { Draft = 1, PendingApproval = 2, Approved = 3, Rejected = 4, Cancelled = 5 }
public enum OvertimeStatus : byte { Draft = 1, PendingApproval = 2, Approved = 3, Rejected = 4, AppliedToPayroll = 5, Cancelled = 6 }
public enum EmployeeLoanStatus : byte { Draft = 1, PendingApproval = 2, Approved = 3, Active = 4, Completed = 5, Rejected = 6, Cancelled = 7 }
public enum LoanInstallmentStatus : byte { Pending = 1, Scheduled = 2, Deducted = 3, PaidExternally = 4, Cancelled = 5, DeductedAtEndOfService = 6 }
public enum LoanRepaymentMode : byte { Payroll = 1, External = 2, Mixed = 3 }
public enum EmployeeAdjustmentType : byte { Earning = 1, Deduction = 2 }
public enum EmployeeAdjustmentStatus : byte { Draft = 1, PendingApproval = 2, Approved = 3, Rejected = 4, AppliedToPayroll = 5, Cancelled = 6 }
