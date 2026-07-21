using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniERP.Application.Ventas.Contracts;
using MiniERP.Application.Ventas.Dtos;
using MiniERP.Application.Ventas.Services;
using MiniERP.Infrastructure.Persistence.Repositories;

namespace MiniERP.Infrastructure;

public static partial class DependencyInjection
{
    /// <summary>Registros del modulo de Ventas (POS y comprobantes). Dueno: Jeison.</summary>
    public static IServiceCollection AddVentas(this IServiceCollection services)
    {
        services.AddScoped<ISecuenciaNcfRepositorio, SecuenciaNcfRepositorio>();
        services.AddScoped<IVentaRepositorio, VentaRepositorio>();
        services.AddScoped<IVentaService, VentaService>();
        services.AddScoped<ISecuenciaNcfService, SecuenciaNcfService>();

        // Los datos fiscales del comercio salen de configuracion, no de la base. Se leen
        // desde el proveedor para no cambiar la firma de AddVentas: asi el archivo raiz
        // DependencyInjection.cs no se toca y deja de ser fuente de conflictos.
        services.AddSingleton(sp =>
            sp.GetRequiredService<IConfiguration>()
                .GetSection(DatosEmisor.SectionName)
                .Get<DatosEmisor>() ?? new DatosEmisor());

        services.AddScoped<IEcfService, EcfService>();

        return services;
    }
}
