namespace Mercado.Api.Services;

// Abstração do gateway de cartão: permite simular aprovação/recusa nos testes.
public interface IPagamentoCartaoService
{
    Task<ResultadoPagamentoCartao> ProcessarPagamento(
        decimal valor,
        string referenciaExterna,
        string cardToken,
        string paymentMethodId,
        string tipoCartao,
        int parcelas,
        string? cpfPagador);
}
