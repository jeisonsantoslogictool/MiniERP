using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniERP.Application.Inventario.Contracts;
using MiniERP.Application.Inventario.Services;
using MiniERP.Infrastructure.Persistence;
using MiniERP.Infrastructure.Persistence.Repositories;
using MiniERP.Infrastructure.Settings;

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
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<MiniErpDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.Configure<SeedSettings>(configuration.GetSection(SeedSettings.SectionName));
        services.AddScoped<DatabaseInitializer>();

        // Inventario
        services.AddScoped<IProductoRepositorio, ProductoRepositorio>();
        services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<ICategoriaService, CategoriaService>();

        return services;
    }
}
