using Mercado.Api.Context;
using Mercado.Api.DTOs.Auth;
using Mercado.Api.Entities;
using Mercado.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mercado.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly RefreshTokenService _refreshTokenService;

    public AuthController(
    AppDbContext context,
    TokenService tokenService,
    RefreshTokenService refreshTokenService)
    {
        _context = context;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login(LoginDTO dto)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(x =>
                x.Email == dto.Email);

        if (usuario == null)
        {
            return Unauthorized("Usuário ou senha inválidos");
        }

        var senhaValida = BCrypt.Net.BCrypt.Verify(
            dto.Senha,
            usuario.Senha
        );

        if (!senhaValida)
        {
            return Unauthorized("Usuário ou senha inválidos");
        }

        var token = _tokenService.GerarToken(usuario);

        var refreshToken = await _refreshTokenService.CriarToken(usuario);

        return Ok(new
        {
            token,
            refreshToken
        });
    }

    [HttpPost("registrar")]
    public async Task<ActionResult> Registrar(RegistrarDTO dto)
    {
        var usuarioExiste = await _context.Usuarios
            .AnyAsync(x => x.Email == dto.Email);

        if (usuarioExiste)
        {
            return BadRequest("Email já cadastrado");
        }

        var senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);

        var usuario = new Usuario
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Senha = senhaHash,
            Role = dto.Role
        };

        await _context.Usuarios.AddAsync(usuario);

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("refresh")]
    public async Task<ActionResult> Refresh(
    RefreshTokenDTO dto)
    {
        var refreshToken = await _context
            .RefreshTokens
            .Include(x => x.Usuario)
            .FirstOrDefaultAsync(x =>
                x.Token == dto.RefreshToken);

        if (refreshToken == null)
        {
            return Unauthorized(
                "Refresh token inválido");
        }

        if (refreshToken.Revogado)
        {
            return Unauthorized(
                "Refresh token revogado");
        }

        if (refreshToken.Expiracao
            < DateTime.UtcNow)
        {
            return Unauthorized(
                "Refresh token expirado");
        }

        var novoJwt = _tokenService
            .GerarToken(refreshToken.Usuario);

        return Ok(new
        {
            token = novoJwt
        });
    }
}