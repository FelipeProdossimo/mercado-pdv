namespace Mercado.Api.DTOs.Produto;

public class ProdutoDTO
{
    public int Id { get; set; }

    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public int Estoque { get; set; }

    public string CodigoBarras { get; set; } = string.Empty;

    public string? ImagemUrl { get; set; }
}