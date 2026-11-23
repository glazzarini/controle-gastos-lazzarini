using ControleGastos.Api.Json;
using ControleGastos.Core.Dtos.Responses;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace ControleGastos.Api.Middleware;

/// <summary>Garante <c>{ error }</c> em respostas de erro sem corpo (ex.: 405 do roteamento).</summary>
public static class RespostaStatusCode
{
    public const string MensagemMetodoNaoPermitido = "Method not allowed";

    public static Task EscreverAsync(StatusCodeContext contexto)
    {
        var resposta = contexto.HttpContext.Response;
        var mensagem = resposta.StatusCode == StatusCodes.Status405MethodNotAllowed
            ? MensagemMetodoNaoPermitido
            : ReasonPhrases.GetReasonPhrase(resposta.StatusCode);
        return resposta.WriteAsJsonAsync(new ErroResponse(mensagem), JsonConfig.Padrao);
    }
}
