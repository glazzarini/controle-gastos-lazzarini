using ControleGastos.Core.Dtos.Requests;
using ControleGastos.Core.Dtos.Responses;

namespace ControleGastos.Core.Interfaces;

public interface ILancamentoService
{
    Task<IReadOnlyList<LancamentoResponse>> ListarAsync(string? mes, CancellationToken ct);
    Task<LancamentosCriadosResponse> CriarAsync(LancamentoCriarRequest request, CancellationToken ct);
    Task<LancamentoAtualizadoResponse> AtualizarAsync(string id, LancamentoAtualizarRequest request, CancellationToken ct);
    Task<DeletedResponse> ExcluirAsync(string id, CancellationToken ct);
}

public interface ICategoriaService
{
    Task<IReadOnlyList<CategoriaResponse>> ListarAsync(CancellationToken ct);
    Task<CategoriaResponse> CriarAsync(CategoriaCriarRequest request, CancellationToken ct);
    Task<CategoriaAtualizadaResponse> AtualizarAsync(string id, CategoriaAtualizarRequest request, CancellationToken ct);
    Task<DeletedResponse> ExcluirAsync(string id, CancellationToken ct);
}

public interface IContaFixaService
{
    Task<IReadOnlyList<ContaFixaResponse>> ListarAsync(CancellationToken ct);
    Task<ContaFixaResponse> CriarAsync(ContaFixaCriarRequest request, CancellationToken ct);
    Task<ContaFixaAtualizadaResponse> AtualizarAsync(string id, ContaFixaAtualizarRequest request, CancellationToken ct);
    Task<DeletedResponse> ExcluirAsync(string id, CancellationToken ct);
}

public interface IConfigService
{
    Task<ConfigResponse> ObterAsync(CancellationToken ct);
    Task<UpdatedResponse> AtualizarRendaAsync(RendaAtualizarRequest request, CancellationToken ct);
}
