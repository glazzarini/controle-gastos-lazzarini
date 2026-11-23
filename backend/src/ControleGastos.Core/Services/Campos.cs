using System.Globalization;
using ControleGastos.Core.Dtos;

namespace ControleGastos.Core.Services;

/// <summary>Helpers que reproduzem a semântica JS das validações atuais (truthy, trim, parseFloat).</summary>
public static class Campos
{
    /// <summary>Texto com conteúdo (equivale a uma string truthy no JS).</summary>
    public static bool Preenchido(CampoOpcional<string> campo) =>
        campo.TemValor && !string.IsNullOrEmpty(campo.Valor);

    /// <summary>Texto com trim; ausente, nulo, vazio ou inválido viram <c>""</c> (equivale a <c>(x || '').trim()</c>).</summary>
    public static string TextoAparado(CampoOpcional<string> campo) =>
        campo.TemValor ? campo.Valor!.Trim() : string.Empty;

    /// <summary>Converte <c>DD/MM/YYYY</c> (dia e mês com 1 ou 2 dígitos).</summary>
    public static bool TryParseDataBr(string texto, out DateTime data) =>
        DateTime.TryParseExact(texto, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out data);

    /// <summary>Converte <c>DD/MM/YYYY</c> ou ISO <c>YYYY-MM-DD</c> (o PUT atual aceita os dois).</summary>
    public static bool TryParseDataBrOuIso(string texto, out DateTime data) =>
        texto.Contains('-')
            ? DateTime.TryParseExact(texto, "yyyy-M-d", CultureInfo.InvariantCulture, DateTimeStyles.None, out data)
            : TryParseDataBr(texto, out data);

    /// <summary>Remove zeros à direita da escala (<c>450.00m</c> → <c>450m</c>), como o <c>number</c> do JS.</summary>
    public static decimal Normalizar(decimal valor) => valor / 1.000000000000000000000000000000000m;

    /// <summary>Formata como <c>String(n)</c> do JS para gravar na tabela <c>config</c>.</summary>
    public static string ParaTexto(decimal valor) => Normalizar(valor).ToString(CultureInfo.InvariantCulture);

    /// <summary>Lê número gravado como texto; vazio ou inválido devolve <paramref name="padrao"/>.</summary>
    public static decimal NumeroOuPadrao(string? texto, decimal padrao) =>
        decimal.TryParse(texto, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : padrao;
}
