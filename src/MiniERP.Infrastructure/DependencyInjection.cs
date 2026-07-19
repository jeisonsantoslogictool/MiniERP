using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniERP.Infrastructure.Persistence;
using MiniERP.Infrastructure.Settings;

namespace MiniERP.Infrastructure;

/// <summary>
/// Punto de entrada de la capa de infraestructura. La capa de presentacion la invoca
/// sin conocer Entity Framework ni el motor de base de datos que hay detras.
/// </summary>
/// <remarks>
/// Cada modulo registra sus propios servicios en su archivo parcial
/// (DependencyInjection.Inventario.cs, .Clientes.cs, .Ventas.cs, .Compras.cs, .Finanzas.cs).
/// Este archivo solo arma la base comun y los encadena, para que dos personas no editen
/// la misma lista de registros y choquen en el merge.
/// </remarks>
public static partial class DependencyInjection
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

        // Cada modulo registra lo suyo en su archivo parcial. Anade tus servicios alli, no aqui.
        return services
            .AddInventario()
            .AddClientes()
            .AddVentas()
            .AddCompras()
            .AddFinanzas();
    }
}
