using OAS.Domain.Exceptions;

namespace OAS.Domain.Sales.ValueObjects;

public sealed record OpticalPrescriptionRangePolicy
{
    public OpticalPrescriptionRangePolicy(
        OpticalPowerRange sphere,
        OpticalPowerRange cylinder,
        OpticalPowerRange add)
    {
        if (add.Minimum < 0)
            throw new DomainException("ADD range cannot allow negative values.");

        Sphere = sphere;
        Cylinder = cylinder;
        Add = add;
    }

    public OpticalPowerRange Sphere { get; }
    public OpticalPowerRange Cylinder { get; }
    public OpticalPowerRange Add { get; }
}
