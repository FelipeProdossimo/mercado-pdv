using Mercado.Api.Context;
using Mercado.Api.Entities;

namespace Mercado.Api.Services;

public class RefreshTokenService
{
    private readonly AppDbContext _context;

    public RefreshTokenService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> CriarToken(
        Usuario usuario)
    {
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString(),

            Expiracao = DateTime.UtcNow.AddDays(7),

            UsuarioId = usuario.Id
        };

        await _context.RefreshTokens
            .AddAsync(refreshToken);

        await _context.SaveChangesAsync();

        return refreshToken.Token;
    }
}