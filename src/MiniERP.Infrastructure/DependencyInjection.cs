using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniERP.Infrastructure.Persistence;

namespace MiniERP.Infrastructure;

/// <summary>
/// Punto de entrada de la capa de infraestructura. La capa de presentacion la invoca
/// sin conocer Entity Framework ni el motor de base de datos que hay detras.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se encontro la cadena de conexion 'DefaultConnection' en la configuracion.");

        services.AddDbContext<MiniErpDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
            })
            .AddEntityFrameworkStores<MiniErpDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }
}
