using System.Globalization;
using MercadoPago.Client.Order;

namespace Mercado.Api.Services;

public record CobrancaPixResultado(
    string PagamentoId,
    string QrCode,
    string QrCodeBase64,
    DateTime ExpiraEm);

public class PagamentoPixService
{
    private readonly string _emailPagadorPadrao;
    private readonly string? _nomePagadorTeste;
    private readonly int _minutosExpiracao;

    public PagamentoPixService(IConfiguration configuration)
    {
        _emailPagadorPadrao =
            configuration["MercadoPago:EmailPagadorPadrao"]
            ?? "cliente@mercado.local";

        // Só usado em homologação: a API de Orders exige o valor mágico
        // "APRO" no nome do pagador para simular a aprovação automática do
        // PIX com credenciais de teste. Deixar vazio/nulo em produção (não
        // configurar essa chave).
        _nomePagadorTeste = configuration["MercadoPago:NomePagadorTeste"];

        _minutosExpiracao =
            int.TryParse(
                configuration["MercadoPago:MinutosExpiracaoPix"],
                out var minutos)
                ? minutos
                : 15;
    }

    public async Task<CobrancaPixResultado> CriarCobranca(
        decimal valor,
        string referenciaExterna)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_minutosExpiracao);

        var valorFormatado = valor.ToString("F2", CultureInfo.InvariantCulture);

        var request = new OrderCreateRequest
        {
            Type = "online",
            TotalAmount = valorFormatado,
            ExternalReference = referenciaExterna,

            // Formato ISO 8601 de duração (ex.: "PT15M"), não uma data.
            ExpirationTime = $"PT{_minutosExpiracao}M",

            Payer = new OrderPayerRequest
            {
                Email = _emailPagadorPadrao,
                FirstName = string.IsNullOrWhiteSpace(_nomePagadorTeste)
                    ? null
                    : _nomePagadorTeste
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
                            Id = "pix",
                            Type = "bank_transfer"
                        }
                    }
                ]
            }
        };

        var client = new OrderClient();

        var pedido = await client.CreateAsync(request);

        var pagamento = pedido.Transactions?.Payments?.FirstOrDefault();

        var dadosPix = pagamento?.PaymentMethod;

        if (pagamento == null || dadosPix == null || string.IsNullOrEmpty(dadosPix.QrCode))
        {
            throw new InvalidOperationException(
                "Mercado Pago não retornou os dados do QR Code PIX.");
        }

        return new CobrancaPixResultado(
            pedido.Id!,
            dadosPix.QrCode,
            dadosPix.QrCodeBase64 ?? string.Empty,
            expiraEm);
    }

    public async Task<string> ConsultarStatus(string pedidoId)
    {
        var client = new OrderClient();

        var pedido = await client.GetAsync(pedidoId);

        // Status geral do pedido ("processed", "action_required", "expired",
        // "canceled") é o que reflete se o PIX foi efetivamente pago.
        return pedido.Status ?? "action_required";
    }
}
