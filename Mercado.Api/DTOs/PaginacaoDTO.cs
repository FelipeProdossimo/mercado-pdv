namespace Mercado.Api.DTOs;

public class PaginacaoDTO
{
    public int Pagina { get; set; } = 1;

    public int TamanhoPagina { get; set; } = 10;

    public string? Descricao { get; set; }
}