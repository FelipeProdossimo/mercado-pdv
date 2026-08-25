using Mercado.Api.Context;
using Mercado.Api.DTOs.Caixa;
using Mercado.Api.Entities;
using Mercado.Api.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mercado.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CaixaController : ControllerBase
{
    private readonly AppDbContext _context;

    public CaixaController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("abrir")]
    public async Task<ActionResult> Abrir(
        AbrirCaixaDTO dto)
    {
        var caixaAberto = await _context.Caixas
            .AnyAsync(x => x.Aberto);

        if (caixaAberto)
        {
            return BadRequest("Já existe caixa aberto");
        }

        var caixa = new Caixa
        {
            DataAbertura = DateTime.UtcNow,
            ValorInicial = dto.ValorInicial,
            Aberto = true
        };

        await _context.Caixas.AddAsync(caixa);

        await _context.SaveChangesAsync();

        return Ok(caixa);
    }

    [HttpPost("fechar")]
    public async Task<ActionResult> Fechar()
    {
        var caixa = await _context.Caixas
            .FirstOrDefaultAsync(x => x.Aberto);

        if (caixa == null)
        {
            return BadRequest("Nenhum caixa aberto");
        }

        var totalVendas = await _context.Vendas
            .Where(x =>
                x.CaixaId == caixa.Id &&
                x.StatusPagamento == StatusPagamentoEnum.Aprovado)
            .SumAsync(x => x.ValorTotal);

        caixa.DataFechamento = DateTime.UtcNow;

        caixa.ValorFinal =
            caixa.ValorInicial + totalVendas;

        caixa.Aberto = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            caixa.Id,
            caixa.ValorInicial,
            TotalVendas = totalVendas,
            caixa.ValorFinal
        });
    }
}