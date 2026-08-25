using Mercado.Api.Context;
using Mercado.Api.Entities;

namespace Mercado.Api.Services;

public class AuditoriaService
{
    private readonly AppDbContext _context;

    public AuditoriaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task Registrar(
        string usuario,
        string acao,
        string detalhes)
    {
        var auditoria = new Auditoria
        {
            Usuario = usuario,
            Acao = acao,
            Data = DateTime.UtcNow,
            Detalhes = detalhes
        };

        await _context.Auditorias.AddAsync(auditoria);

        await _context.SaveChangesAsync();
    }
}