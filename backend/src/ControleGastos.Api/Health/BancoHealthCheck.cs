using ControleGastos.Core.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ControleGastos.Api.Health;

/// <summary>Readiness: executa <c>SELECT 1</c> no Postgres (timeout configurado no registro do check).</summary>
public sealed class BancoHealthCheck(IDbConnectionFactory factory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await ConsultarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
#pragma warning disable CA1031 // qualquer falha de conexão/consulta deixa o pod "não pronto"
        catch (Exception e)
#pragma warning restore CA1031
        {
            return HealthCheckResult.Unhealthy("Banco indisponível", e);
        }
    }

    private async Task ConsultarAsync(CancellationToken cancellationToken)
    {
        await using var conexao = await factory.AbrirAsync(cancellationToken);
        await using var comando = conexao.CreateCommand();
        comando.CommandText = "SELECT 1";
        await comando.ExecuteScalarAsync(cancellationToken);
    }
}
