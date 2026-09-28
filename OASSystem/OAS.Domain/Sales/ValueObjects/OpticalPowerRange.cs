using OAS.Domain.Exceptions;

namespace OAS.Domain.Sales.ValueObjects;

public readonly record struct OpticalPowerRange
{
    public OpticalPowerRange(decimal minimum, decimal maximum)
    {
        if (minimum > maximum)
            throw new DomainException("Optical range minimum cannot be greater than maximum.");

        Minimum = minimum;
        Maximum = maximum;
    }

    public decimal Minimum { get; }
    public decimal Maximum { get; }

    public bool Contains(decimal value) => value >= Minimum && value <= Maximum;
}
