using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Common.Exceptions;

namespace OAS.Application.Features.Employees.EndOfService;

public sealed class ManualEndOfServiceCalculationPolicy : IEndOfServiceCalculationPolicy
{
    public Task<decimal> CalculateBenefitAsync(EndOfServiceCalculationInput input, CancellationToken cancellationToken = default)
    {
        if (input.RequestedBenefitAmount < 0)
            throw new ConflictException("eos_benefit_invalid", "End-of-service benefit cannot be negative.");
        // V1 intentionally accepts an explicitly reviewed benefit amount. No country-specific
        // labor-law formula is embedded in the domain; a jurisdiction policy can replace this service.
        return Task.FromResult(input.RequestedBenefitAmount);
    }

    public Task<decimal> CalculateLeaveDailyRateAsync(EndOfServiceCalculationInput input, CancellationToken cancellationToken = default)
    {
        if (input.RequestedLeaveDailyRate is null or <= 0m)
            throw new ConflictException("eos_leave_daily_rate_required", "A reviewed leave-settlement daily rate is required when encashable leave is settled.");
        return Task.FromResult(input.RequestedLeaveDailyRate.Value);
    }
}
