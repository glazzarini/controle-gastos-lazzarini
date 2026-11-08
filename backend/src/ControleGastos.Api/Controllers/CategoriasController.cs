using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ControleGastos.Api.Controllers;

[ApiController]
[Route("api/categorias")]
public sealed class CategoriasController(ICategoriaService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct) => Ok(await service.ListarAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CategoriaCriarRequest? request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await service.CriarAsync(request ?? new CategoriaCriarRequest(), ct));

    /// <summary>PUT /api/categorias/:id — renomear propaga para o histórico de lançamentos.</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(
        string id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CategoriaAtualizarRequest? request,
        CancellationToken ct) =>
        Ok(await service.AtualizarAsync(id, request ?? new CategoriaAtualizarRequest(), ct));

    /// <summary>DELETE /api/categorias/:id — 409 se houver lançamentos usando a categoria.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(string id, CancellationToken ct) => Ok(await service.ExcluirAsync(id, ct));
}
