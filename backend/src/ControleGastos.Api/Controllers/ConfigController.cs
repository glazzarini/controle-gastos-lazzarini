using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ControleGastos.Api.Controllers;

[ApiController]
[Route("api/config")]
public sealed class ConfigController(IConfigService service) : ControllerBase
{
    /// <summary>GET /api/config — agrega config, tetos por categoria e contas fixas (total_fixos calculado).</summary>
    [HttpGet]
    public async Task<IActionResult> Obter(CancellationToken ct) => Ok(await service.ObterAsync(ct));

    /// <summary>PUT /api/config — atualiza renda_guilherme e/ou renda_esposa.</summary>
    [HttpPut]
    public async Task<IActionResult> AtualizarRenda(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RendaAtualizarRequest? request, CancellationToken ct) =>
        Ok(await service.AtualizarRendaAsync(request ?? new RendaAtualizarRequest(), ct));
}
