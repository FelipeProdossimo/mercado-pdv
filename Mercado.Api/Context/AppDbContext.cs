using Mercado.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercado.Api.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }

    public DbSet<Produto> Produtos { get; set; }

    public DbSet<Venda> Vendas { get; set; }

    public DbSet<ItemVenda> ItensVenda { get; set; }

    public DbSet<Caixa> Caixas { get; set; }

    public DbSet<Auditoria> Auditorias { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
}