using Microsoft.Extensions.DependencyInjection;
using MiniERP.Application.Clientes.Contracts;
using MiniERP.Application.Clientes.Services;
using MiniERP.Infrastructure.Persistence.Repositories;

namespace MiniERP.Infrastructure;

public static partial class DependencyInjection
{
    /// <summary>Registros del modulo de Clientes. Dueno: Dionis.</summary>
    public static IServiceCollection AddClientes(this IServiceCollection services)
    {
        services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<ICobrosRepositorio, CobrosRepositorio>();
        services.AddScoped<ICobrosService, CobrosService>();

        return services;
    }
}
