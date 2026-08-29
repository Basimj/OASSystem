using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Database.Abstractions;
using OAS.Application.Identity.Abstractions;
using OAS.Infrastructure.Database.Configuration;
using OAS.Infrastructure.Database.Services;
using OAS.Infrastructure.Identity.Persistence;
using OAS.Infrastructure.Identity.Security;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Persistence.Interceptors;
using OAS.Infrastructure.Persistence.Repositories.Generic;

namespace OAS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseProfilesOptions>(configuration.GetSection(DatabaseProfilesOptions.SectionName));
        services.AddSingleton<DatabaseProfileCatalog>();
        services.AddScoped<DatabaseMaintenanceService>();

        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<OasDbContext>((sp, options) =>
        {
            var catalog = sp.GetRequiredService<DatabaseProfileCatalog>();
            var selection = sp.GetRequiredService<IDatabaseProfileSelection>();
            options.UseSqlServer(catalog.ResolveConnectionString(selection.ProfileKey))
                   .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped(typeof(IReadRepository<,>), typeof(EfRepository<,>));
        services.AddScoped(typeof(IRepository<,>), typeof(EfRepository<,>));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped<IPasswordService, AspNetPasswordService>();
        return services;
    }
}
