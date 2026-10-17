using System.Net;
using System.Net.Http.Json;
using Mercado.Api.Enums;
using Mercado.Api.Tests.Infra;

namespace Mercado.Api.Tests;

[Collection(ApiCollection.Nome)]
public class PagamentoCartaoTests(MercadoApiFactory api) : TesteDeIntegracao(api)
{
    private Task<HttpResponseMessage> PagarComCartao(int produtoId, int quantidade) =>
        Client.PostAsJsonAsync("/api/vendas/cartao/processar", new
        {
            itens = Cenario.Itens((produtoId, quantidade)),
            formaPagamento = FormaPagamentoEnum.CartaoCredito,
            cardToken = "token-gerado-no-navegador",
            paymentMethodId = "visa",
            parcelas = 1
        });

    [Fact]
    public async Task Cartao_aprovado_baixa_o_estoque()
    {
        var arroz = await Api.CriarProduto(estoque: 10);
        await Client.AbrirCaixa();

        var resposta = await PagarComCartao(arroz.Id, 4);
        var resultado = await resposta.Content.ReadFromJsonAsync<ResultadoCartao>();

        Assert.Equal(StatusPagamentoEnum.Aprovado, resultado!.StatusPagamento);
        Assert.Equal(6, await Api.EstoqueAtual(arroz.Id));
    }

    [Fact]
    public async Task Cartao_recusado_nao_baixa_o_estoque()
    {
        var arroz = await Api.CriarProduto(estoque: 10);
        await Client.AbrirCaixa();
        Api.Cartao.StatusResposta = "rejected";

        var resposta = await PagarComCartao(arroz.Id, 4);
        var resultado = await resposta.Content.ReadFromJsonAsync<ResultadoCartao>();

        Assert.Equal(StatusPagamentoEnum.Recusado, resultado!.StatusPagamento);
        Assert.NotNull(resultado.MotivoRecusa);
        Assert.Equal(10, await Api.EstoqueAtual(arroz.Id));
    }

    [Fact]
    public async Task Sem_token_do_cartao_a_requisicao_e_rejeitada()
    {
        var arroz = await Api.CriarProduto(estoque: 10);
        await Client.AbrirCaixa();

        var resposta = await Client.PostAsJsonAsync("/api/vendas/cartao/processar", new
        {
            itens = Cenario.Itens((arroz.Id, 1)),
            formaPagamento = FormaPagamentoEnum.CartaoCredito,
            cardToken = "",
            paymentMethodId = "visa"
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    private record ResultadoCartao(int VendaId, StatusPagamentoEnum StatusPagamento, string? MotivoRecusa);
}
