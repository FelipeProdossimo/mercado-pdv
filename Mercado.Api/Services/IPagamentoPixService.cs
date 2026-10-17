namespace Mercado.Api.Services;

// Abstração do gateway de PIX: permite trocar o Mercado Pago por um falso
// nos testes automatizados, sem fazer chamada real.
public interface IPagamentoPixService
{
    Task<CobrancaPixResultado> CriarCobranca(decimal valor, string referenciaExterna);

    Task<string> ConsultarStatus(string pedidoId);
}
