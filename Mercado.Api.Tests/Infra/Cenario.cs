using System.Net.Http.Json;
using Mercado.Api.Entities;

namespace Mercado.Api.Tests.Infra;

// Atalhos para montar o cenário de cada teste.
public static class Cenario
{
    public static async Task<Produto> CriarProduto(
        this MercadoApiFactory api,
        string descricao = "Arroz 5kg",
        decimal valor = 25.90m,
        int estoque = 10)
    {
        var produto = new Produto
        {
            Descricao = descricao,
            Valor = valor,
            Estoque = estoque,
            CodigoBarras = Guid.NewGuid().ToString("N")[..13]
        };

        await api.ExecutarNoBanco(async context =>
        {
            context.Produtos.Add(produto);
            await context.SaveChangesAsync();
        });

        return produto;
    }

    public static async Task<int> EstoqueAtual(this MercadoApiFactory api, int produtoId) =>
        await api.ConsultarBanco(async context =>
            (await context.Produtos.FindAsync(produtoId))!.Estoque);

    public static async Task AbrirCaixa(this HttpClient client, decimal valorInicial = 100m)
    {
        var resposta = await client.PostAsJsonAsync("/api/caixa/abrir", new { valorInicial });
        resposta.EnsureSuccessStatusCode();
    }

    public static object Itens(params (int produtoId, int quantidade)[] itens) =>
        itens.Select(i => new { produtoId = i.produtoId, quantidade = i.quantidade }).ToArray();
}
