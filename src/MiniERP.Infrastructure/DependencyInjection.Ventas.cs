using Microsoft.Extensions.DependencyInjection;
using MiniERP.Application.Ventas.Contracts;
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

        return services;
    }
}
