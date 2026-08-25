namespace Mercado.Api.DTOs;

public class PaginacaoRespostaDTO<T>
{
    public List<T> Dados { get; set; } = [];

    public int TotalRegistros { get; set; }

    public int Pagina { get; set; }

    public int TamanhoPagina { get; set; }
}