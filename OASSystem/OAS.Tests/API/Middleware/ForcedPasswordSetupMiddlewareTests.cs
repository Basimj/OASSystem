using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using OAS.API.Middleware;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Identity.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Identity.Entities;

namespace OAS.Tests.API.Middleware;

[TestFixture]
public sealed class ForcedPasswordSetupMiddlewareTests
{
    private static readonly Guid AuthenticatedUserId = Guid.Parse("8F4A57C8-40A6-45B7-8C7B-C2264FA07999");

    [TestCase("/api/database/bootstrap")]
    [TestCase("/api/database/bootstrap/profiles")]
    [TestCase("/api/database/bootstrap/status")]
    [TestCase("/api/database/bootstrap/update")]
    public async Task DatabaseBootstrapPaths_BypassIdentityRepository(string path)
    {
        var repository = new ThrowingIdentityRepository();
        var nextWasCalled = false;
        var middleware = new ForcedPasswordSetupMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        var context = CreateAuthenticatedContext(path);

        await middleware.InvokeAsync(context, repository);

        Assert.Multiple(() =>
        {
            Assert.That(nextWasCalled, Is.True);
            Assert.That(repository.GetUserCallCount, Is.Zero);
        });
    }

    [Test]
    public void OrdinaryAuthenticatedApiPath_StillRevalidatesIdentity()
    {
        var repository = new ThrowingIdentityRepository();
        var middleware = new ForcedPasswordSetupMiddleware(_ => Task.CompletedTask);
        var context = CreateAuthenticatedContext("/api/identity/auth/me");

        var exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await middleware.InvokeAsync(context, repository));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo(ThrowingIdentityRepository.ExpectedExceptionMessage));
            Assert.That(repository.GetUserCallCount, Is.EqualTo(1));
        });
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, AuthenticatedUserId.ToString()),
            new Claim(ClaimTypes.Name, "test-admin")
        ], "Test"));
        return context;
    }

    private sealed class ThrowingIdentityRepository : IIdentityRepository
    {
        public const string ExpectedExceptionMessage = "Identity repository should only be called for non-bootstrap API requests.";

        public int GetUserCallCount { get; private set; }

        public Task<IdentityUserRecord?> GetUserAsync(
            Guid userId,
            bool forUpdate,
            CancellationToken cancellationToken = default)
        {
            GetUserCallCount++;
            throw new InvalidOperationException(ExpectedExceptionMessage);
        }

        public Task<IdentityUserRecord?> FindByLoginAsync(string login, bool forUpdate, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PagedData<IdentityUserRecord>> GetUsersPageAsync(
            PageRequest request,
            bool? isActive = null,
            Guid? roleId = null,
            bool includeSuperAdmin = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Role>> ListRolesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Role?> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Role>> GetRolesByIdsAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> UserNameExistsAsync(
            string normalizedUserName,
            Guid? excludingUserId = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> EmailExistsAsync(
            string normalizedEmail,
            Guid? excludingUserId = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasAnyUsersAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> RoleExistsAsync(string normalizedRoleName, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UserPasswordHistory>> GetPasswordHistoryAsync(
            Guid userId,
            int take,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddUserAsync(UserAccount user, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddRoleAsync(Role role, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ReplaceUserRolesAsync(
            Guid userId,
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task AddPasswordHistoryAsync(UserPasswordHistory history, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void UpdateUser(UserAccount user) => throw new NotSupportedException();

        public void UpdateRole(Role role) => throw new NotSupportedException();
    }
}
