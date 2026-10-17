using Mercado.Api.Services;

namespace Mercado.Api.Tests.Infra;

// Substitui o Mercado Pago nos testes: o teste decide qual status o "banco"
// devolve e conta quantas vezes a API consultou.
public class PixFalso : IPagamentoPixService
{
    public string StatusAtual { get; set; } = "action_required";

    public int Consultas { get; private set; }

    public void Reiniciar()
    {
        StatusAtual = "action_required";
        Consultas = 0;
    }

    public Task<CobrancaPixResultado> CriarCobranca(decimal valor, string referenciaExterna) =>
        Task.FromResult(new CobrancaPixResultado(
            PagamentoId: $"ORD-{referenciaExterna}",
            QrCode: "00020126580014br.gov.bcb.pix-teste",
            QrCodeBase64: "iVBORw0KGgo=",
            ExpiraEm: DateTime.UtcNow.AddMinutes(15)));

    public Task<string> ConsultarStatus(string pedidoId)
    {
        Consultas++;
        return Task.FromResult(StatusAtual);
    }
}

public class CartaoFalso : IPagamentoCartaoService
{
    public string StatusResposta { get; set; } = "approved";

    public void Reiniciar() => StatusResposta = "approved";

    public Task<ResultadoPagamentoCartao> ProcessarPagamento(
        decimal valor,
        string referenciaExterna,
        string cardToken,
        string paymentMethodId,
        string tipoCartao,
        int parcelas,
        string? cpfPagador) =>
        Task.FromResult(new ResultadoPagamentoCartao(
            StatusResposta,
            StatusResposta == "approved" ? null : "cc_rejected_insufficient_amount"));
}
