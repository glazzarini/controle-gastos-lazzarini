using System.Net;
using System.Text;
using System.Text.Json;

namespace ControleGastos.Test.Fixtures;

public sealed record Resposta(HttpStatusCode Status, JsonElement Corpo, HttpResponseMessage Mensagem, string Texto)
{
    public int Codigo => (int)Status;
    public string? Erro => Corpo.ValueKind == JsonValueKind.Object && Corpo.TryGetProperty("error", out var e) ? e.GetString() : null;
}

/// <summary>Helpers HTTP para os testes de contrato.</summary>
public abstract class ApiTesteBase(PostgresFixture db) : IntegracaoBase(db)
{
    protected Task<Resposta> Get(string url) => Enviar(HttpMethod.Get, url);
    protected Task<Resposta> Post(string url, string json) => Enviar(HttpMethod.Post, url, json);
    protected Task<Resposta> Put(string url, string json) => Enviar(HttpMethod.Put, url, json);
    protected Task<Resposta> Delete(string url) => Enviar(HttpMethod.Delete, url);

    protected async Task<Resposta> Enviar(
        HttpMethod metodo, string url, string? json = null, bool semBanco = false, string contentType = "application/json")
    {
        using var client = (semBanco ? Db.ApiSemBanco : Db.Api).CreateClient();
        using var request = new HttpRequestMessage(metodo, url);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, contentType);
        }
        var resposta = await client.SendAsync(request, Ct);
        var texto = await resposta.Content.ReadAsStringAsync(Ct);
        var corpo = texto.Length > 0 && texto[0] is '{' or '['
            ? JsonDocument.Parse(texto).RootElement.Clone()
            : default;
        return new Resposta(resposta.StatusCode, corpo, resposta, texto);
    }

    protected static void AssertNumero(decimal esperado, JsonElement elemento)
    {
        Assert.Equal(JsonValueKind.Number, elemento.ValueKind); // dinheiro sempre number, nunca string
        Assert.Equal(esperado, elemento.GetDecimal());
    }
}
