using System.Net;
using System.Net.Http.Json;
using Mercado.Api.Enums;
using Mercado.Api.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace Mercado.Api.Tests;

[Collection(ApiCollection.Nome)]
public class VendaEmDinheiroTests(MercadoApiFactory api) : TesteDeIntegracao(api)
{
    [Fact]
    public async Task Venda_aprovada_baixa_o_estoque_e_calcula_o_total()
    {
        var arroz = await Api.CriarProduto("Arroz 5kg", 25.90m, estoque: 10);
        var feijao = await Api.CriarProduto("Feijão 1kg", 8.50m, estoque: 5);
        await Client.AbrirCaixa();

        var resposta = await Client.PostAsJsonAsync("/api/vendas", new
        {
            formaPagamento = FormaPagamentoEnum.Dinheiro,
            itens = Cenario.Itens((arroz.Id, 2), (feijao.Id, 3))
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var venda = await resposta.Content.ReadFromJsonAsync<VendaCriada>();
        Assert.Equal(25.90m * 2 + 8.50m * 3, venda!.ValorTotal);

        Assert.Equal(8, await Api.EstoqueAtual(arroz.Id));
        Assert.Equal(2, await Api.EstoqueAtual(feijao.Id));
    }

    [Fact]
    public async Task Venda_e_recusada_quando_nao_ha_caixa_aberto()
    {
        var arroz = await Api.CriarProduto(estoque: 10);

        var resposta = await Client.PostAsJsonAsync("/api/vendas", new
        {
            formaPagamento = FormaPagamentoEnum.Dinheiro,
            itens = Cenario.Itens((arroz.Id, 1))
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(10, await Api.EstoqueAtual(arroz.Id));
    }

    [Fact]
    public async Task Estoque_insuficiente_desfaz_a_venda_inteira()
    {
        // O primeiro item tem estoque, o segundo não: a baixa do primeiro
        // precisa ser desfeita (transação), senão o estoque fica errado.
        var arroz = await Api.CriarProduto("Arroz 5kg", estoque: 10);
        var feijao = await Api.CriarProduto("Feijão 1kg", estoque: 1);
        await Client.AbrirCaixa();

        var resposta = await Client.PostAsJsonAsync("/api/vendas", new
        {
            formaPagamento = FormaPagamentoEnum.Dinheiro,
            itens = Cenario.Itens((arroz.Id, 2), (feijao.Id, 5))
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(10, await Api.EstoqueAtual(arroz.Id));
        Assert.Equal(1, await Api.EstoqueAtual(feijao.Id));
        Assert.Equal(0, await Api.ConsultarBanco(c => c.Vendas.CountAsync()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Quantidade_zero_ou_negativa_e_rejeitada(int quantidade)
    {
        var arroz = await Api.CriarProduto(estoque: 10);
        await Client.AbrirCaixa();

        var resposta = await Client.PostAsJsonAsync("/api/vendas", new
        {
            formaPagamento = FormaPagamentoEnum.Dinheiro,
            itens = Cenario.Itens((arroz.Id, quantidade))
        });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(10, await Api.EstoqueAtual(arroz.Id));
    }

    [Fact]
    public async Task Endpoints_de_venda_exigem_login()
    {
        var anonimo = Api.CreateClient();

        var resposta = await anonimo.GetAsync("/api/vendas");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    private record VendaCriada(int Id, decimal ValorTotal);
}
