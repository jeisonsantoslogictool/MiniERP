using Microsoft.Extensions.DependencyInjection;
using MiniERP.Application.Compras.Contracts;
using MiniERP.Application.Compras.Services;
using MiniERP.Infrastructure.Persistence.Repositories;

namespace MiniERP.Infrastructure;

public static partial class DependencyInjection
{
    /// <summary>Registros del modulo de Compras. Dueno: Dionis.</summary>
    public static IServiceCollection AddCompras(this IServiceCollection services)
    {
        services.AddScoped<IProveedorRepositorio, ProveedorRepositorio>();
        services.AddScoped<ICompraRepositorio, CompraRepositorio>();
        services.AddScoped<IPagosRepositorio, PagosRepositorio>();
        services.AddScoped<IProveedorService, ProveedorService>();
        services.AddScoped<ICompraService, CompraService>();
        services.AddScoped<IPagosService, PagosService>();

        return services;
    }
}
