using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ControleGastos.Api.Controllers;

[ApiController]
[Route("api/contas-fixas")]
public sealed class ContasFixasController(IContaFixaService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await service.ListarAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ContaFixaCriarRequest? request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CriarAsync(request ?? new ContaFixaCriarRequest(), ct));

    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(
        string id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ContaFixaAtualizarRequest? request,
        CancellationToken ct) =>
        Ok(await service.AtualizarAsync(id, request ?? new ContaFixaAtualizarRequest(), ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(string id, CancellationToken ct) => Ok(await service.ExcluirAsync(id, ct));
}
