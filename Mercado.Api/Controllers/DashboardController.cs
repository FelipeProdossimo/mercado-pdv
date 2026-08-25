using Mercado.Api.Context;
using Mercado.Api.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mercado.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> ObterDados()
    {
        var hoje = DateTime.UtcNow.Date;

        var vendasHoje = await _context.Vendas
            .Where(x => x.DataVenda.Date == hoje)
            .ToListAsync();

        var totalVendidoHoje =
            vendasHoje.Sum(x => x.ValorTotal);

        var quantidadeVendasHoje =
            vendasHoje.Count;

        var faturamentoTotal =
            await _context.Vendas
                .SumAsync(x => x.ValorTotal);

        var produtoMaisVendido =
            await _context.ItensVenda
                .GroupBy(x => x.Produto.Descricao)
                .Select(x => new
                {
                    Produto = x.Key,
                    Quantidade = x.Sum(y => y.Quantidade)
                })
                .OrderByDescending(x => x.Quantidade)
                .FirstOrDefaultAsync();

        var estoqueBaixo =
            await _context.Produtos
                .Where(w => w.Ativo)
                .CountAsync(x => x.Estoque < 10);

        var dto = new DashboardDTO
        {
            TotalVendidoHoje = totalVendidoHoje,

            QuantidadeVendasHoje =
                quantidadeVendasHoje,

            FaturamentoTotal =
                faturamentoTotal,

            ProdutoMaisVendido =
                produtoMaisVendido?.Produto
                    ?? "Sem vendas",

            ProdutosEstoqueBaixo =
                estoqueBaixo
        };

        return Ok(dto);
    }

    [HttpGet("grafico-vendas")]
    public async Task<ActionResult> ObterGraficoVendas()
    {
        var vendas = await _context.Vendas
            .GroupBy(x => new
            {
                x.DataVenda.Year,
                x.DataVenda.Month
            })
            .Select(x => new
            {
                x.Key.Year,
                x.Key.Month,
                Total = x.Sum(v => v.ValorTotal)
            })
            .ToListAsync();

        var resultado = vendas
            .Select(x => new GraficoVendaMensalDTO
            {
                Mes = $"{x.Month}/{x.Year}",
                Total = x.Total
            })
            .OrderBy(x => x.Mes)
            .ToList();

        return Ok(resultado);
    }

    [HttpGet("mais-vendidos")]
    public async Task<ActionResult> ObterMaisVendidos()
    {
        var dados = await _context.ItensVenda
            .Include(x => x.Produto)
            .GroupBy(x => x.Produto.Descricao)
            .Select(x => new ProdutoMaisVendidoDTO
            {
                Produto = x.Key,
                QuantidadeVendida =
                    x.Sum(i => i.Quantidade)
            })
            .OrderByDescending(x => x.QuantidadeVendida)
            .Take(5)
            .ToListAsync();

        return Ok(dados);
    }

    [HttpGet("estoque-baixo")]
    public async Task<ActionResult> ObterEstoqueBaixo()
    {
        var produtos = await _context.Produtos
            .Where(x => x.Ativo && x.Estoque <= 5)
            .OrderBy(x => x.Estoque)
            .Select(x => new
            {
                x.Id,
                x.Descricao,
                x.Estoque
            })
            .ToListAsync();

        return Ok(produtos);
    }
}