using Mercado.Api.Enums;

namespace Mercado.Api.DTOs.Venda;

public class IniciarPagamentoCartaoDTO
{
    public List<ItemVendaDTO> Itens { get; set; } = [];

    // Débito ou Crédito — determina o "type" enviado ao Mercado Pago.
    public FormaPagamentoEnum FormaPagamento { get; set; }

    // Token gerado no navegador pelo SDK do Mercado Pago (Card Form) — o
    // número do cartão nunca chega até a nossa API.
    public string CardToken { get; set; } = string.Empty;

    // Bandeira identificada pelo SDK (ex.: "master", "visa").
    public string PaymentMethodId { get; set; } = string.Empty;

    // Só relevante para crédito; débito sempre processa como 1x.
    public int Parcelas { get; set; } = 1;

    // Opcional — não é capturado por padrão no caixa físico.
    public string? CpfCliente { get; set; }
}
