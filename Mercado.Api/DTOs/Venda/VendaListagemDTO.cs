using Mercado.Api.Enums;

namespace Mercado.Api.DTOs.Venda;

public class VendaListagemDTO
{
    public int Id { get; set; }

    public DateTime DataVenda { get; set; }

    public decimal ValorTotal { get; set; }

    public StatusPagamentoEnum StatusPagamento { get; set; }
}