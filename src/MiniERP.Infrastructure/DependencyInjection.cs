using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniERP.Infrastructure.Identity;
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

                // Bloqueo por intentos fallidos. El login lo activa con lockoutOnFailure: true
                // (Login.razor); aqui se fija cuanto aguanta y cuanto dura.
                //
                // Cinco intentos toleran al cajero que se equivoca de tecla sin regalarle
                // nada a quien prueba claves: con quince minutos de espera, un atacante
                // consigue a lo sumo 480 intentos por dia contra una cuenta, que frente a
                // una clave de seis caracteres con cuatro clases no le alcanza para nada.
                // Quince y no mas, porque el que se bloquea es casi siempre el propio
                // empleado con la fila esperando; si no puede esperar, el administrador
                // lo desbloquea desde /usuarios con "Activar", que borra el LockoutEnd.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<MiniErpDbContext>()
            .AddSignInManager()
            .AddErrorDescriber<MensajesDeIdentity>()
            .AddDefaultTokenProviders();

        services.Configure<SeedSettings>(configuration.GetSection(SeedSettings.SectionName));
        services.AddScoped<DatabaseInitializer>();

        // Cada modulo registra lo suyo en su archivo parcial. Anade tus servicios alli, no aqui.
        return services
            .AddInventario()
            .AddClientes()
            .AddVentas()
            .AddCompras()
            .AddFinanzas()
            .AddSeguridad();
    }
}
