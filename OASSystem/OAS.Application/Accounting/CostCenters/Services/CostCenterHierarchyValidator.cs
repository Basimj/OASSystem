using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CostCenters.Services;

internal static class CostCenterHierarchyValidator
{
    public static async Task ValidateAsync(
        IRepository<CostCenter, Guid> repository,
        Guid? parentId,
        Guid? currentId,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid>();
        var cursor = parentId;
        while (cursor.HasValue)
        {
            if (cursor.Value == currentId || !visited.Add(cursor.Value))
                throw new ConflictException("cost_center_hierarchy_cycle",
                    "لا يمكن أن يكون مركز التكلفة أبًا لنفسه أو لأحد المراكز الأعلى في شجرته.");

            var parent = await repository.GetByIdAsync(cursor.Value, cancellationToken);
            if (parent is null)
                throw new ConflictException("cost_center_parent_not_found",
                    "مركز التكلفة الأب غير موجود. اختر مركزًا صالحًا.");
            cursor = parent.ParentCostCenterId;
        }
    }
}
