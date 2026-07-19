using Microsoft.Extensions.DependencyInjection;
using MiniERP.Application.Inventario.Contracts;
using MiniERP.Application.Inventario.Services;
using MiniERP.Infrastructure.Persistence.Repositories;

namespace MiniERP.Infrastructure;

public static partial class DependencyInjection
{
    /// <summary>Registros del modulo de Inventario. Dueno: Jeison.</summary>
    public static IServiceCollection AddInventario(this IServiceCollection services)
    {
        services.AddScoped<IProductoRepositorio, ProductoRepositorio>();
        services.AddScoped<ICategoriaRepositorio, CategoriaRepositorio>();
        services.AddScoped<IProductoService, ProductoService>();
        services.AddScoped<ICategoriaService, CategoriaService>();

        return services;
    }
}
