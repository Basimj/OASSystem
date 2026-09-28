using OAS.Domain.Exceptions;
using OAS.Domain.Sales.ValueObjects;

namespace OAS.Domain.Sales.Rules;

public static class PrescriptionOpticalRules
{
    public static void ValidateStructural(
        decimal? sphere,
        decimal? cylinder,
        short? axis,
        decimal? add,
        decimal? prism,
        decimal? pd,
        decimal? monocularPd,
        decimal? fittingHeight)
    {
        if (axis is < 0 or > 180)
            throw new DomainException("Axis must be between 0 and 180.");
        if (add is < 0)
            throw new DomainException("ADD cannot be negative.");
        if (prism is < 0)
            throw new DomainException("Prism cannot be negative.");
        if (pd.HasValue && pd.Value <= 0)
            throw new DomainException("PD must be greater than zero.");
        if (monocularPd.HasValue && monocularPd.Value <= 0)
            throw new DomainException("Monocular PD must be greater than zero.");
        if (fittingHeight.HasValue && fittingHeight.Value <= 0)
            throw new DomainException("Fitting height must be greater than zero.");
    }

    public static void ValidateConfiguredRanges(
        decimal? sphere,
        decimal? cylinder,
        decimal? add,
        OpticalPrescriptionRangePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (sphere.HasValue && !policy.Sphere.Contains(sphere.Value))
            throw new DomainException("SPH is outside the configured optical range.");
        if (cylinder.HasValue && !policy.Cylinder.Contains(cylinder.Value))
            throw new DomainException("CYL is outside the configured optical range.");
        if (add.HasValue && !policy.Add.Contains(add.Value))
            throw new DomainException("ADD is outside the configured optical range.");
    }
}
