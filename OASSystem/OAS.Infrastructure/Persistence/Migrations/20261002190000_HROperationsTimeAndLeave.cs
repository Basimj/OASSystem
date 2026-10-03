using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OAS.Infrastructure.Persistence;

#nullable disable

namespace OAS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(OasDbContext))]
[Migration("20261002190000_HROperationsTimeAndLeave")]
public sealed class HROperationsTimeAndLeave : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF SCHEMA_ID(N'hr') IS NULL EXEC(N'CREATE SCHEMA [hr]');
IF NOT EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'WorkShiftCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) EXEC(N'CREATE SEQUENCE [hr].[WorkShiftCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'HolidayCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) EXEC(N'CREATE SEQUENCE [hr].[HolidayCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'LeaveTypeCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) EXEC(N'CREATE SEQUENCE [hr].[LeaveTypeCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'LeaveRequestCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) EXEC(N'CREATE SEQUENCE [hr].[LeaveRequestCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
IF NOT EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'OvertimeCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) EXEC(N'CREATE SEQUENCE [hr].[OvertimeCodeSequence] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');

IF OBJECT_ID(N'[hr].[HrSettings]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[HrSettings]([Id] uniqueidentifier NOT NULL,[TimeZoneId] nvarchar(128) NULL,[LeaveYearStartMonth] tinyint NOT NULL,[RequireAttendanceApproval] bit NOT NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_HrSettings] PRIMARY KEY([Id]),CONSTRAINT [CK_HrSettings_LeaveYearStartMonth] CHECK([LeaveYearStartMonth] BETWEEN 1 AND 12));
 INSERT INTO [hr].[HrSettings]([Id],[TimeZoneId],[LeaveYearStartMonth],[RequireAttendanceApproval],[CreatedAtUtc]) VALUES('6E599352-4DE1-4F62-B410-F903EEC1A601',NULL,1,1,SYSUTCDATETIME());
END;
IF OBJECT_ID(N'[hr].[WorkShifts]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[WorkShifts]([Id] uniqueidentifier NOT NULL,[ShiftCode] nvarchar(32) NOT NULL,[NameAr] nvarchar(150) NOT NULL,[NameEn] nvarchar(150) NULL,[StartTime] time NOT NULL,[EndTime] time NOT NULL,[BreakMinutes] int NOT NULL,[GraceLateMinutes] int NOT NULL,[GraceEarlyLeaveMinutes] int NOT NULL,[WorkingDaysMask] tinyint NOT NULL,[IsFlexible] bit NOT NULL,[IsActive] bit NOT NULL,[Notes] nvarchar(500) NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_WorkShifts] PRIMARY KEY([Id]),CONSTRAINT [CK_WorkShifts_Times] CHECK([StartTime]<>[EndTime]),CONSTRAINT [CK_WorkShifts_Minutes] CHECK([BreakMinutes]>=0 AND [GraceLateMinutes]>=0 AND [GraceEarlyLeaveMinutes]>=0),CONSTRAINT [CK_WorkShifts_WorkingDaysMask] CHECK([WorkingDaysMask] BETWEEN 1 AND 127));
 CREATE UNIQUE INDEX [UX_WorkShifts_ShiftCode] ON [hr].[WorkShifts]([ShiftCode]);CREATE INDEX [IX_WorkShifts_IsActive] ON [hr].[WorkShifts]([IsActive]);
END;
IF OBJECT_ID(N'[hr].[EmployeeShiftAssignments]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[EmployeeShiftAssignments]([Id] uniqueidentifier NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[WorkShiftId] uniqueidentifier NOT NULL,[EffectiveFrom] date NOT NULL,[EffectiveTo] date NULL,[IsActive] bit NOT NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_EmployeeShiftAssignments] PRIMARY KEY([Id]),CONSTRAINT [CK_EmployeeShiftAssignments_Dates] CHECK([EffectiveTo] IS NULL OR [EffectiveTo]>=[EffectiveFrom]),CONSTRAINT [FK_EmployeeShiftAssignments_Employees] FOREIGN KEY([EmployeeId]) REFERENCES [hr].[Employees]([Id]),CONSTRAINT [FK_EmployeeShiftAssignments_WorkShifts] FOREIGN KEY([WorkShiftId]) REFERENCES [hr].[WorkShifts]([Id]));
 CREATE INDEX [IX_ShiftAssignments_Employee_Dates] ON [hr].[EmployeeShiftAssignments]([EmployeeId],[EffectiveFrom],[EffectiveTo]);CREATE INDEX [IX_ShiftAssignments_ShiftId] ON [hr].[EmployeeShiftAssignments]([WorkShiftId]);
END;
IF OBJECT_ID(N'[hr].[Holidays]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[Holidays]([Id] uniqueidentifier NOT NULL,[HolidayCode] nvarchar(32) NOT NULL,[NameAr] nvarchar(150) NOT NULL,[NameEn] nvarchar(150) NULL,[StartDate] date NOT NULL,[EndDate] date NOT NULL,[IsPaid] bit NOT NULL,[IsActive] bit NOT NULL,[Notes] nvarchar(500) NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_Holidays] PRIMARY KEY([Id]),CONSTRAINT [CK_Holidays_Dates] CHECK([EndDate]>=[StartDate]));
 CREATE UNIQUE INDEX [UX_Holidays_HolidayCode] ON [hr].[Holidays]([HolidayCode]);CREATE INDEX [IX_Holidays_Dates] ON [hr].[Holidays]([StartDate],[EndDate],[IsActive]);
END;
IF OBJECT_ID(N'[hr].[LeaveTypes]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[LeaveTypes]([Id] uniqueidentifier NOT NULL,[LeaveTypeCode] nvarchar(32) NOT NULL,[NameAr] nvarchar(150) NOT NULL,[NameEn] nvarchar(150) NULL,[IsPaid] bit NOT NULL,[RequiresBalance] bit NOT NULL,[AccrualMethod] tinyint NOT NULL,[DayCountingMethod] tinyint NOT NULL,[AnnualEntitlementDays] decimal(8,2) NOT NULL,[MaximumCarryForwardDays] decimal(8,2) NOT NULL,[ProrateOnHire] bit NOT NULL,[IsActive] bit NOT NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_LeaveTypes] PRIMARY KEY([Id]),CONSTRAINT [CK_LeaveTypes_Amounts] CHECK([AnnualEntitlementDays]>=0 AND [MaximumCarryForwardDays]>=0));
 CREATE UNIQUE INDEX [UX_LeaveTypes_Code] ON [hr].[LeaveTypes]([LeaveTypeCode]);CREATE INDEX [IX_LeaveTypes_IsActive] ON [hr].[LeaveTypes]([IsActive]);
END;
IF OBJECT_ID(N'[hr].[EmployeeLeaveBalances]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[EmployeeLeaveBalances]([Id] uniqueidentifier NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[LeaveTypeId] uniqueidentifier NOT NULL,[LeaveYear] smallint NOT NULL,[OpeningBalanceDays] decimal(8,2) NOT NULL,[AccruedDays] decimal(8,2) NOT NULL,[UsedDays] decimal(8,2) NOT NULL,[AdjustmentDays] decimal(8,2) NOT NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_EmployeeLeaveBalances] PRIMARY KEY([Id]),CONSTRAINT [CK_LeaveBalances_NonNegative] CHECK([OpeningBalanceDays]>=0 AND [AccruedDays]>=0 AND [UsedDays]>=0),CONSTRAINT [FK_LeaveBalances_Employees] FOREIGN KEY([EmployeeId]) REFERENCES [hr].[Employees]([Id]),CONSTRAINT [FK_LeaveBalances_Types] FOREIGN KEY([LeaveTypeId]) REFERENCES [hr].[LeaveTypes]([Id]));
 CREATE UNIQUE INDEX [UX_LeaveBalances_Employee_Type_Year] ON [hr].[EmployeeLeaveBalances]([EmployeeId],[LeaveTypeId],[LeaveYear]);CREATE INDEX [IX_LeaveBalances_LeaveTypeId] ON [hr].[EmployeeLeaveBalances]([LeaveTypeId]);
END;
IF OBJECT_ID(N'[hr].[LeaveRequests]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[LeaveRequests]([Id] uniqueidentifier NOT NULL,[LeaveRequestCode] nvarchar(40) NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[LeaveTypeId] uniqueidentifier NOT NULL,[LeaveTypeCodeSnapshot] nvarchar(32) NOT NULL,[LeaveTypeNameSnapshot] nvarchar(150) NOT NULL,[IsPaidSnapshot] bit NOT NULL,[RequiresBalanceSnapshot] bit NOT NULL,[DayCountingMethodSnapshot] tinyint NOT NULL,[StartDate] date NOT NULL,[EndDate] date NOT NULL,[RequestedDays] decimal(8,2) NOT NULL,[ApprovedDays] decimal(8,2) NULL,[Status] tinyint NOT NULL,[Reason] nvarchar(500) NULL,[BalanceOverrideUsed] bit NOT NULL,[BalanceOverrideReason] nvarchar(500) NULL,[SubmittedBy] nvarchar(64) NULL,[SubmittedAtUtc] datetimeoffset NULL,[ApprovedBy] nvarchar(64) NULL,[ApprovedAtUtc] datetimeoffset NULL,[RejectedBy] nvarchar(64) NULL,[RejectedAtUtc] datetimeoffset NULL,[RejectionReason] nvarchar(500) NULL,[CancelledBy] nvarchar(64) NULL,[CancelledAtUtc] datetimeoffset NULL,[CancellationReason] nvarchar(500) NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_LeaveRequests] PRIMARY KEY([Id]),CONSTRAINT [CK_LeaveRequests_Dates] CHECK([EndDate]>=[StartDate]),CONSTRAINT [CK_LeaveRequests_Days] CHECK([RequestedDays]>0 AND ([ApprovedDays] IS NULL OR [ApprovedDays]>=0)),CONSTRAINT [FK_LeaveRequests_Employees] FOREIGN KEY([EmployeeId]) REFERENCES [hr].[Employees]([Id]),CONSTRAINT [FK_LeaveRequests_Types] FOREIGN KEY([LeaveTypeId]) REFERENCES [hr].[LeaveTypes]([Id]));
 CREATE UNIQUE INDEX [UX_LeaveRequests_Code] ON [hr].[LeaveRequests]([LeaveRequestCode]);CREATE INDEX [IX_LeaveRequests_Employee] ON [hr].[LeaveRequests]([EmployeeId]);CREATE INDEX [IX_LeaveRequests_Status] ON [hr].[LeaveRequests]([Status]);CREATE INDEX [IX_LeaveRequests_Dates] ON [hr].[LeaveRequests]([StartDate],[EndDate]);CREATE INDEX [IX_LeaveRequests_LeaveTypeId] ON [hr].[LeaveRequests]([LeaveTypeId]);
END;
IF OBJECT_ID(N'[hr].[AttendanceRecords]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[AttendanceRecords]([Id] uniqueidentifier NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[AttendanceDate] date NOT NULL,[WorkShiftId] uniqueidentifier NULL,[ShiftCodeSnapshot] nvarchar(32) NULL,[ShiftNameSnapshot] nvarchar(150) NULL,[TimeZoneIdSnapshot] nvarchar(128) NULL,[ScheduledStartAtUtc] datetimeoffset NULL,[ScheduledEndAtUtc] datetimeoffset NULL,[BreakMinutesSnapshot] int NOT NULL,[GraceLateMinutesSnapshot] int NOT NULL,[GraceEarlyLeaveMinutesSnapshot] int NOT NULL,[IsFlexibleSnapshot] bit NOT NULL,[CheckInAtUtc] datetimeoffset NULL,[CheckOutAtUtc] datetimeoffset NULL,[Source] tinyint NOT NULL,[AttendanceStatus] tinyint NOT NULL,[ApprovalStatus] tinyint NOT NULL,[ScheduledMinutes] int NOT NULL,[WorkedMinutes] int NOT NULL,[LateMinutes] int NOT NULL,[EarlyLeaveMinutes] int NOT NULL,[OvertimeMinutes] int NOT NULL,[SourceLeaveRequestId] uniqueidentifier NULL,[SourceHolidayId] uniqueidentifier NULL,[SubmittedBy] nvarchar(64) NULL,[SubmittedAtUtc] datetimeoffset NULL,[ApprovedBy] nvarchar(64) NULL,[ApprovedAtUtc] datetimeoffset NULL,[RejectedBy] nvarchar(64) NULL,[RejectedAtUtc] datetimeoffset NULL,[RejectionReason] nvarchar(500) NULL,[LastCorrectedBy] nvarchar(64) NULL,[LastCorrectedAtUtc] datetimeoffset NULL,[LastCorrectionReason] nvarchar(500) NULL,[Notes] nvarchar(500) NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_AttendanceRecords] PRIMARY KEY([Id]),CONSTRAINT [CK_Attendance_CheckTimes] CHECK([CheckOutAtUtc] IS NULL OR [CheckInAtUtc] IS NULL OR [CheckOutAtUtc]>=[CheckInAtUtc]),CONSTRAINT [CK_Attendance_Minutes] CHECK([ScheduledMinutes]>=0 AND [WorkedMinutes]>=0 AND [LateMinutes]>=0 AND [EarlyLeaveMinutes]>=0 AND [OvertimeMinutes]>=0 AND [BreakMinutesSnapshot]>=0 AND [GraceLateMinutesSnapshot]>=0 AND [GraceEarlyLeaveMinutesSnapshot]>=0),CONSTRAINT [FK_Attendance_Employees] FOREIGN KEY([EmployeeId]) REFERENCES [hr].[Employees]([Id]),CONSTRAINT [FK_Attendance_WorkShifts] FOREIGN KEY([WorkShiftId]) REFERENCES [hr].[WorkShifts]([Id]),CONSTRAINT [FK_Attendance_LeaveRequests] FOREIGN KEY([SourceLeaveRequestId]) REFERENCES [hr].[LeaveRequests]([Id]),CONSTRAINT [FK_Attendance_Holidays] FOREIGN KEY([SourceHolidayId]) REFERENCES [hr].[Holidays]([Id]));
 CREATE UNIQUE INDEX [UX_Attendance_Employee_Date] ON [hr].[AttendanceRecords]([EmployeeId],[AttendanceDate]);CREATE INDEX [IX_Attendance_Date] ON [hr].[AttendanceRecords]([AttendanceDate]);CREATE INDEX [IX_Attendance_Status] ON [hr].[AttendanceRecords]([AttendanceStatus]);CREATE INDEX [IX_Attendance_ApprovalStatus] ON [hr].[AttendanceRecords]([ApprovalStatus]);CREATE INDEX [IX_Attendance_WorkShiftId] ON [hr].[AttendanceRecords]([WorkShiftId]);CREATE INDEX [IX_Attendance_SourceLeaveRequestId] ON [hr].[AttendanceRecords]([SourceLeaveRequestId]);CREATE INDEX [IX_Attendance_SourceHolidayId] ON [hr].[AttendanceRecords]([SourceHolidayId]);
END;
IF OBJECT_ID(N'[hr].[OvertimeRecords]',N'U') IS NULL
BEGIN
 CREATE TABLE [hr].[OvertimeRecords]([Id] uniqueidentifier NOT NULL,[OvertimeCode] nvarchar(40) NOT NULL,[EmployeeId] uniqueidentifier NOT NULL,[AttendanceRecordId] uniqueidentifier NULL,[WorkDate] date NOT NULL,[RequestedMinutes] int NOT NULL,[ApprovedMinutes] int NOT NULL,[RateMultiplier] decimal(9,4) NOT NULL,[Status] tinyint NOT NULL,[Reason] nvarchar(500) NULL,[SubmittedBy] nvarchar(64) NULL,[SubmittedAtUtc] datetimeoffset NULL,[ApprovedBy] nvarchar(64) NULL,[ApprovedAtUtc] datetimeoffset NULL,[RejectedBy] nvarchar(64) NULL,[RejectedAtUtc] datetimeoffset NULL,[RejectionReason] nvarchar(500) NULL,[CancelledBy] nvarchar(64) NULL,[CancelledAtUtc] datetimeoffset NULL,[CancellationReason] nvarchar(500) NULL,[CreatedAtUtc] datetimeoffset NOT NULL,[CreatedBy] nvarchar(64) NULL,[LastModifiedAtUtc] datetimeoffset NULL,[LastModifiedBy] nvarchar(64) NULL,[CreatedFromDevice] nvarchar(256) NULL,[UpdatedFromDevice] nvarchar(256) NULL,[RowVersion] rowversion NOT NULL,CONSTRAINT [PK_OvertimeRecords] PRIMARY KEY([Id]),CONSTRAINT [CK_Overtime_Minutes] CHECK([RequestedMinutes]>0 AND [ApprovedMinutes]>=0 AND [ApprovedMinutes]<=[RequestedMinutes]),CONSTRAINT [CK_Overtime_Multiplier] CHECK([RateMultiplier]>0),CONSTRAINT [FK_Overtime_Employees] FOREIGN KEY([EmployeeId]) REFERENCES [hr].[Employees]([Id]),CONSTRAINT [FK_Overtime_Attendance] FOREIGN KEY([AttendanceRecordId]) REFERENCES [hr].[AttendanceRecords]([Id]));
 CREATE UNIQUE INDEX [UX_Overtime_Code] ON [hr].[OvertimeRecords]([OvertimeCode]);CREATE INDEX [IX_Overtime_Employee_Date] ON [hr].[OvertimeRecords]([EmployeeId],[WorkDate]);CREATE INDEX [IX_Overtime_Status] ON [hr].[OvertimeRecords]([Status]);CREATE UNIQUE INDEX [UX_Overtime_AttendanceRecord] ON [hr].[OvertimeRecords]([AttendanceRecordId]) WHERE [AttendanceRecordId] IS NOT NULL;
END;
""");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
SET XACT_ABORT ON;
IF OBJECT_ID(N'[hr].[OvertimeRecords]',N'U') IS NOT NULL DROP TABLE [hr].[OvertimeRecords];
IF OBJECT_ID(N'[hr].[AttendanceRecords]',N'U') IS NOT NULL DROP TABLE [hr].[AttendanceRecords];
IF OBJECT_ID(N'[hr].[LeaveRequests]',N'U') IS NOT NULL DROP TABLE [hr].[LeaveRequests];
IF OBJECT_ID(N'[hr].[EmployeeLeaveBalances]',N'U') IS NOT NULL DROP TABLE [hr].[EmployeeLeaveBalances];
IF OBJECT_ID(N'[hr].[LeaveTypes]',N'U') IS NOT NULL DROP TABLE [hr].[LeaveTypes];
IF OBJECT_ID(N'[hr].[Holidays]',N'U') IS NOT NULL DROP TABLE [hr].[Holidays];
IF OBJECT_ID(N'[hr].[EmployeeShiftAssignments]',N'U') IS NOT NULL DROP TABLE [hr].[EmployeeShiftAssignments];
IF OBJECT_ID(N'[hr].[WorkShifts]',N'U') IS NOT NULL DROP TABLE [hr].[WorkShifts];
IF OBJECT_ID(N'[hr].[HrSettings]',N'U') IS NOT NULL DROP TABLE [hr].[HrSettings];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'OvertimeCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[OvertimeCodeSequence];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'LeaveRequestCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[LeaveRequestCodeSequence];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'LeaveTypeCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[LeaveTypeCodeSequence];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'HolidayCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[HolidayCodeSequence];
IF EXISTS(SELECT 1 FROM sys.sequences WHERE name=N'WorkShiftCodeSequence' AND schema_id=SCHEMA_ID(N'hr')) DROP SEQUENCE [hr].[WorkShiftCodeSequence];
""");
}
