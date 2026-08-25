using Mercado.Api.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Mercado.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RelatoriosController : ControllerBase
{
    private readonly AppDbContext _context;

    public RelatoriosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("vendas-pdf")]
    public async Task<IActionResult> GerarRelatorio()
    {
        var vendas = await _context.Vendas
            .OrderByDescending(x => x.DataVenda)
            .ToListAsync();

        var total = vendas.Sum(x => x.ValorTotal);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Header()
                    .Text("Relatório de Vendas")
                    .FontSize(20)
                    .Bold();

                page.Content().Column(col =>
                {
                    col.Item().Text(
                        $"Data: {DateTime.Now:dd/MM/yyyy}");

                    col.Item().Text(
                        $"Total faturado: R$ {total}");

                    col.Item().PaddingTop(20);

                    foreach (var venda in vendas)
                    {
                        col.Item().Border(1).Padding(10)
                            .Column(item =>
                            {
                                item.Item().Text(
                                    $"Venda #{venda.Id}");

                                item.Item().Text(
                                    $"Data: {venda.DataVenda:dd/MM/yyyy HH:mm}");

                                item.Item().Text(
                                    $"Valor: R$ {venda.ValorTotal}");
                            });
                    }
                });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                    });
            });
        });

        var arquivo = pdf.GeneratePdf();

        return File(
            arquivo,
            "application/pdf",
            "relatorio-vendas.pdf");
    }

    [HttpGet("cupom/{vendaId}")]
    public async Task<IActionResult> GerarCupom(
    int vendaId)
    {
        var venda = await _context.Vendas
            .Include(x => x.Itens)
            .ThenInclude(x => x.Produto)
            .FirstOrDefaultAsync(x => x.Id == vendaId);

        if (venda == null)
        {
            return NotFound("Venda não encontrada");
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A7);

                page.Margin(10);

                page.DefaultTextStyle(x =>
                    x.FontSize(10));

                page.Content().Column(col =>
                {
                    col.Item()
                        .AlignCenter()
                        .Text("VAREJÃO FOLHA VERDE")
                        .Bold()
                        .FontSize(14);

                    col.Item()
                        .AlignCenter()
                        .Text("CUPOM FISCAL");

                    col.Item().PaddingVertical(10);

                    foreach (var item in venda.Itens)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(
                                item.Produto.Descricao);

                            row.ConstantItem(50).AlignRight()
                                .Text(
                                    $"R$ {item.Total:F2}");
                        });

                        col.Item().Text(
                            $"{item.Quantidade} x R$ {item.ValorUnitario:F2}");
                    }

                    col.Item()
                        .PaddingVertical(10)
                        .LineHorizontal(1);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem()
                            .Text("TOTAL")
                            .Bold();

                        row.ConstantItem(60)
                            .AlignRight()
                            .Text(
                                $"R$ {venda.ValorTotal:F2}")
                            .Bold();
                    });

                    col.Item().PaddingTop(20);

                    col.Item()
                        .AlignCenter()
                        .Text(
                            "Obrigado pela preferência!");

                    col.Item()
                        .AlignCenter()
                        .Text(
                            $"{DateTime.Now:dd/MM/yyyy HH:mm}");
                });
            });
        });

        var arquivo = pdf.GeneratePdf();

        return File(
            arquivo,
            "application/pdf",
            $"cupom-venda-{venda.Id}.pdf");
    }
}