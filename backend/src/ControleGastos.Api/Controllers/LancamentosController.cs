using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ControleGastos.Api.Controllers;

[ApiController]
[Route("api/lancamentos")]
public sealed class LancamentosController(ILancamentoService service) : ControllerBase
{
    /// <summary>GET /api/lancamentos?mes=YYYY-MM</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? mes, CancellationToken ct) =>
        Ok(await service.ListarAsync(mes, ct));

    /// <summary>POST /api/lancamentos (com parcelamento)</summary>
    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LancamentoCriarRequest? request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CriarAsync(request ?? new LancamentoCriarRequest(), ct));

    /// <summary>PUT /api/lancamentos/:id</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(
        string id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LancamentoAtualizarRequest? request,
        CancellationToken ct) =>
        Ok(await service.AtualizarAsync(id, request ?? new LancamentoAtualizarRequest(), ct));

    /// <summary>DELETE /api/lancamentos/:id</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(string id, CancellationToken ct) =>
        Ok(await service.ExcluirAsync(id, ct));
}
