using Mercado.Api.Enums;

namespace Mercado.Api.DTOs.Venda;

public class PagamentoCartaoRespostaDTO
{
    public int VendaId { get; set; }

    public decimal ValorTotal { get; set; }

    public StatusPagamentoEnum StatusPagamento { get; set; }

    public string? MotivoRecusa { get; set; }
}
