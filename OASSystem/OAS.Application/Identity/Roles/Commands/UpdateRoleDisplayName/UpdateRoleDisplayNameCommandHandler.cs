using MediatR;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Roles.Commands.UpdateRoleDisplayName;

public sealed class UpdateRoleDisplayNameCommandHandler(IIdentityRepository repository)
    : IRequestHandler<UpdateRoleDisplayNameCommand, Guid>
{
    public async Task<Guid> Handle(UpdateRoleDisplayNameCommand request, CancellationToken cancellationToken)
    {
        var role = await repository.GetRoleAsync(request.RoleId, cancellationToken)
            ?? throw new NotFoundException(nameof(Role), request.RoleId);

        role.UpdateDisplayName(request.Request.DisplayName);
        repository.UpdateRole(role);
        return role.Id;
    }
}
