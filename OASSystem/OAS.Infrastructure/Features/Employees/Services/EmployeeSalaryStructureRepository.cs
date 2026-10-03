using Microsoft.EntityFrameworkCore;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Features.Employees.Enums;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Features.Employees.Services;

public sealed class EmployeeSalaryStructureRepository(OasDbContext dbContext) : IEmployeeSalaryStructureRepository
{
    public async Task<EmployeeSalaryStructure?> GetByIdAsync(Guid id, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmployeeSalaryStructures.Include(x => x.Lines).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeSalaryStructure>> ListForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default) =>
        await dbContext.EmployeeSalaryStructures.AsNoTracking().Include(x => x.Lines).Where(x => x.EmployeeId == employeeId).OrderByDescending(x => x.EffectiveFrom).ToListAsync(cancellationToken);

    public async Task<EmployeeSalaryStructure?> GetActiveForEmployeeAsync(Guid employeeId, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = dbContext.EmployeeSalaryStructures.Include(x => x.Lines).Where(x => x.EmployeeId == employeeId && x.Status == SalaryStructureStatus.Active);
        if (!tracking) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(EmployeeSalaryStructure entity, CancellationToken cancellationToken = default) => dbContext.EmployeeSalaryStructures.AddAsync(entity, cancellationToken).AsTask();
    public void Update(EmployeeSalaryStructure entity) => dbContext.EmployeeSalaryStructures.Update(entity);
}
