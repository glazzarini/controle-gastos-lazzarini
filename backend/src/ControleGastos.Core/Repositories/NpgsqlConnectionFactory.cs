using System.Data.Common;
using ControleGastos.Core.Interfaces;
using Npgsql;

namespace ControleGastos.Core.Repositories;

/// <summary>Abre conexões Npgsql (pool controlado pela connection string).</summary>
public sealed class NpgsqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    private readonly string _connectionString = Normalizar(connectionString);

    /// <summary>
    /// Desliga a criptografia GSS/Kerberos (o Npgsql tenta por padrão). O app usa usuário/senha + SSL,
    /// e a tentativa só gerava o erro "Cannot load library libgssapi_krb5.so.2" no log da imagem Alpine.
    /// </summary>
    public static string Normalizar(string connectionString) =>
        new NpgsqlConnectionStringBuilder(connectionString) { GssEncryptionMode = GssEncryptionMode.Disable }
            .ConnectionString;

    public async Task<DbConnection> AbrirAsync(CancellationToken ct)
    {
        var conexao = new NpgsqlConnection(_connectionString);
        try
        {
            await conexao.OpenAsync(ct);
            return conexao;
        }
        catch
        {
            await conexao.DisposeAsync();
            throw;
        }
    }
}
