using ControleGastos.Api.Health;
using ControleGastos.Core.Interfaces;
using ControleGastos.Test.Fixtures;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using NSubstitute;

namespace ControleGastos.Test.Integration.Repositories;

public sealed class BancoHealthCheckTests(PostgresFixture db) : IntegracaoBase(db)
{
    [Fact]
    public async Task BancoDisponivel_Healthy()
    {
        var check = new BancoHealthCheck(new ControleGastos.Core.Repositories.NpgsqlConnectionFactory(Db.ConnectionString));
        Assert.Equal(HealthStatus.Healthy, (await check.CheckHealthAsync(new HealthCheckContext(), Ct)).Status);
    }

    [Fact]
    public async Task ConexaoAbreMasConsultaFalha_Unhealthy()
    {
        var conexao = new NpgsqlConnection(Db.ConnectionString);
        await conexao.OpenAsync(Ct);
        var factory = Substitute.For<IDbConnectionFactory>();
        factory.AbrirAsync(Arg.Any<CancellationToken>()).Returns(conexao);
        using var cancelado = new CancellationTokenSource();
        await cancelado.CancelAsync();

        var resultado = await new BancoHealthCheck(factory).CheckHealthAsync(new HealthCheckContext(), cancelado.Token);

        Assert.Equal(HealthStatus.Unhealthy, resultado.Status);
        Assert.Equal("Banco indisponível", resultado.Description);
        Assert.Equal(System.Data.ConnectionState.Closed, conexao.State); // descartada mesmo com erro
    }
}
