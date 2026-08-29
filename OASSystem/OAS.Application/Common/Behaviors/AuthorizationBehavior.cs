using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;

namespace OAS.Application.Common.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>(ICurrentUser currentUser, IPermissionChecker permissionChecker)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IAuthorizedRequest authorized || authorized.RequiredPermissions.Count == 0)
            return await next(cancellationToken);

        if (!currentUser.IsAuthenticated)
            return ExpectedForbiddenOrThrow(request);

        foreach (var permission in authorized.RequiredPermissions)
        {
            if (!await permissionChecker.HasPermissionAsync(permission, cancellationToken))
                return ExpectedForbiddenOrThrow(request);
        }
        return await next(cancellationToken);
    }

    private static TResponse ExpectedForbiddenOrThrow(TRequest request)
    {
        if (request is IExpectedFailureRequest<TResponse> expected)
            return expected.Forbidden("forbidden");
        throw new ForbiddenException();
    }
}
