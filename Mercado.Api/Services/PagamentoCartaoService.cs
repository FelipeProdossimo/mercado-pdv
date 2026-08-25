using System.Globalization;
using MercadoPago.Client.Order;

namespace Mercado.Api.Services;

public record ResultadoPagamentoCartao(
    string Status, // "approved", "rejected", "pending" (vocabulário da Orders API para pagamentos)
    string? MotivoRecusa);

// Integração com o Mercado Pago (API de Orders) para cartão de débito/crédito
// real. Ao contrário do PIX, o resultado normalmente já vem na resposta da
// criação do pedido — não há polling.
public class PagamentoCartaoService
{
    private readonly string _emailPagadorPadrao;

    public PagamentoCartaoService(IConfiguration configuration)
    {
        _emailPagadorPadrao =
            configuration["MercadoPago:EmailPagadorPadrao"]
            ?? "cliente@mercado.local";
    }

    public async Task<ResultadoPagamentoCartao> ProcessarPagamento(
        decimal valor,
        string referenciaExterna,
        string cardToken,
        string paymentMethodId,
        string tipoCartao, // "credit_card" ou "debit_card"
        int parcelas,
        string? cpfPagador)
    {
        var valorFormatado = valor.ToString("F2", CultureInfo.InvariantCulture);

        var request = new OrderCreateRequest
        {
            Type = "online",
            TotalAmount = valorFormatado,
            ExternalReference = referenciaExterna,

            Payer = new OrderPayerRequest
            {
                Email = _emailPagadorPadrao,

                // CPF não é capturado no caixa físico por padrão — só é
                // enviado se o cliente quiser informar (ex.: para a nota).
                Identification = string.IsNullOrWhiteSpace(cpfPagador)
                    ? null
                    : new OrderIdentificationRequest
                    {
                        Type = "CPF",
                        Number = cpfPagador
                    }
            },

            Transactions = new OrderTransactionRequest
            {
                Payments =
                [
                    new OrderPaymentRequest
                    {
                        Amount = valorFormatado,
                        PaymentMethod = new OrderPaymentMethodRequest
                        {
                            Id = paymentMethodId,
                            Type = tipoCartao,
                            Token = cardToken,

                            // Débito não parcela; crédito à vista manda 1.
                            Installments = tipoCartao == "credit_card"
                                ? parcelas
                                : 1
                        }
                    }
                ]
            }
        };

        var client = new OrderClient();

        var pedido = await client.CreateAsync(request);

        var pagamento = pedido.Transactions?.Payments?.FirstOrDefault();

        if (pagamento == null)
        {
            throw new InvalidOperationException(
                "Mercado Pago não retornou os dados do pagamento.");
        }

        return new ResultadoPagamentoCartao(
            pagamento.Status ?? "rejected",
            pagamento.StatusDetail);
    }
}
