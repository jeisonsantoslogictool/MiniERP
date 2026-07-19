using Microsoft.Extensions.DependencyInjection;
using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Services;
using MiniERP.Infrastructure.Persistence.Repositories;

namespace MiniERP.Infrastructure;

public static partial class DependencyInjection
{
    /// <summary>Registros del modulo de Finanzas. Dueno: Samuel.</summary>
    public static IServiceCollection AddFinanzas(this IServiceCollection services)
    {
        services.AddScoped<IEgresoRepositorio, EgresoRepositorio>();
        services.AddScoped<ICategoriaEgresoRepositorio, CategoriaEgresoRepositorio>();
        services.AddScoped<IReporteIngresosRepositorio, ReporteIngresosRepositorio>();
        services.AddScoped<IEgresoService, EgresoService>();
        services.AddScoped<ICategoriaEgresoService, CategoriaEgresoService>();
        services.AddScoped<IReporteIngresosService, ReporteIngresosService>();

        return services;
    }
}
