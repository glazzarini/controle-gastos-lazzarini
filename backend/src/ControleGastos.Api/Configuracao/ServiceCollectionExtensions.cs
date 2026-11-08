using ControleGastos.Core.Interfaces;
using ControleGastos.Core.Repositories;
using ControleGastos.Core.Services;

namespace ControleGastos.Api.Configuracao;

public static class ServiceCollectionExtensions
{
    public const string NomeConnectionString = "Supabase";

    /// <summary>
    /// Registra repositórios e services. A connection string vem de <c>ConnectionStrings__Supabase</c>
    /// (variável de ambiente / Kubernetes Secret / user-secrets) — nunca do appsettings.
    /// </summary>
    public static IServiceCollection AddControleGastos(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory>(sp => new NpgsqlConnectionFactory(
            sp.GetRequiredService<IConfiguration>().GetConnectionString(NomeConnectionString) ?? string.Empty));

        services.AddScoped<ILancamentoRepository, LancamentoRepository>();
        services.AddScoped<ICategoriaRepository, CategoriaRepository>();
        services.AddScoped<IContaFixaRepository, ContaFixaRepository>();
        services.AddScoped<IConfigRepository, ConfigRepository>();

        services.AddScoped<ILancamentoService, LancamentoService>();
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IContaFixaService, ContaFixaService>();
        services.AddScoped<IConfigService, ConfigService>();
        return services;
    }
}
