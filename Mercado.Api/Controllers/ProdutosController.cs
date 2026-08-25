using Mercado.Api.Context;
using Mercado.Api.DTOs;
using Mercado.Api.DTOs.Produto;
using Mercado.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mercado.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProdutosController : ControllerBase
{
    private readonly IProdutoService _service;
    private readonly AppDbContext _context;
    private readonly AuditoriaService _auditoriaService;

    public ProdutosController(
    IProdutoService service,
    AppDbContext context,
    AuditoriaService auditoriaService)
    {
        _service = service;
        _context = context;
        _auditoriaService = auditoriaService;
    }

    [HttpGet]
    public async Task<ActionResult> ObterTodos([FromQuery] PaginacaoDTO dto)
    {
        var produtos = await _service.ObterTodos(dto);

        return Ok(produtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> ObterPorId(int id)
    {
        var produto = await _context.Produtos
            .Where(x => x.Id == id)
            .Select(x => new ProdutoDTO
            {
                Id = x.Id,
                Descricao = x.Descricao,
                Valor = x.Valor,
                Estoque = x.Estoque,
                CodigoBarras = x.CodigoBarras,
                ImagemUrl = x.ImagemUrl
            })
            .FirstOrDefaultAsync();

        if (produto == null)
        {
            return NotFound();
        }

        return Ok(produto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Adicionar(ProdutoCriarDTO dto)
    {
        var id = await _service.Adicionar(dto);

        return Ok(id);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Editar(int id, ProdutoCriarDTO dto)
    {
        var produto = await _context.Produtos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (produto == null)
        {
            return NotFound();
        }

        produto.Descricao = dto.Descricao;
        produto.Valor = dto.Valor;
        produto.Estoque = dto.Estoque;
        produto.CodigoBarras = dto.CodigoBarras;

        await _context.SaveChangesAsync();

        await _auditoriaService.Registrar(
            User.Identity?.Name ?? "Desconhecido",
            "EDITOU_PRODUTO",
            $"Produto {produto.Descricao} editado");

        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Remover(
    int id)
    {
        var produto = await _context.Produtos
            .FirstOrDefaultAsync(x => x.Id == id);

        if (produto == null)
        {
            return NotFound();
        }

        produto.Ativo = false;

        await _context.SaveChangesAsync();

        await _auditoriaService.Registrar(User.Identity?.Name ?? "Desconhecido", "REMOVEU_PRODUTO", $"Produto {produto.Descricao} removido");

        return Ok(new
        {
            mensagem = "Produto removido"
        });
    }

    [HttpPost("{id}/upload-imagem")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UploadImagem(int id, IFormFile arquivo)
    {
        if (arquivo == null || arquivo.Length == 0)
        {
            return BadRequest("Arquivo inválido");
        }

        var produto = await _context.Produtos.FirstOrDefaultAsync(x => x.Id == id);

        if (produto == null)
        {
            return NotFound("Produto não encontrado");
        }

        var extensao = Path.GetExtension(arquivo.FileName);

        var nomeArquivo = $"{Guid.NewGuid()}{extensao}";

        var caminhoPasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "produtos");

        if (!Directory.Exists(caminhoPasta))
        {
            Directory.CreateDirectory(caminhoPasta);
        }

        var caminhoCompleto = Path.Combine(caminhoPasta, nomeArquivo);

        using var stream = new FileStream(caminhoCompleto, FileMode.Create);

        await arquivo.CopyToAsync(stream);

        produto.ImagemUrl = $"/uploads/produtos/{nomeArquivo}";

        await _context.SaveChangesAsync();

        return Ok(new { imagemUrl = produto.ImagemUrl });
    }
}