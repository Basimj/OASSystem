namespace OAS.Domain.Features.Employees.Enums;

public enum PayrollPolicyStatus : byte { Draft = 1, Active = 2, Superseded = 3, Cancelled = 4 }
public enum PayrollProrationMethod : byte { None = 1, CalendarDays = 2, ScheduledWorkingDays = 3 }
public enum PayrollDailyRateMethod : byte { Fixed30Days = 1, CalendarDaysInPeriod = 2, ScheduledWorkingDaysInPeriod = 3 }
public enum PayrollHourlyRateMethod : byte { DailyRateByContractHours = 1, BasicByScheduledMinutes = 2 }
public enum PayrollPeriodStatus : byte { Open = 1, Locked = 2, Closed = 3 }
public enum PayrollRunType : byte { Regular = 1, Final = 2 }
public enum PayrollRunStatus : byte { Draft = 1, Calculated = 2, Reviewed = 3, Approved = 4, Posted = 5, Closed = 6, Cancelled = 7 }
public enum EmployeePayrollStatus : byte { Calculated = 1, Reviewed = 2, Approved = 3, Posted = 4, Cancelled = 5 }
public enum PayrollLineSourceType : byte { SalaryStructure = 1, Attendance = 2, Leave = 3, Overtime = 4, LoanInstallment = 5, Commission = 6, Adjustment = 7, Manual = 8, EmployerContribution = 9 }
public enum PayrollIssueSeverity : byte { Info = 1, Warning = 2, Blocking = 3 }
public enum EndOfServiceStatus : byte { Draft = 1, Calculated = 2, Reviewed = 3, Approved = 4, Posted = 5, Paid = 6, Cancelled = 7 }
public enum EndOfServiceLineType : byte { LeaveSettlement = 1, EndOfServiceBenefit = 2, OtherEarning = 3, LoanDeduction = 4, OtherDeduction = 5 }
