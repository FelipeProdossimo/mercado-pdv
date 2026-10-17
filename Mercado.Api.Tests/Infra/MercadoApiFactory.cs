using System.Net.Http.Headers;
using Mercado.Api.Context;
using Mercado.Api.Entities;
using Mercado.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mercado.Api.Tests.Infra;

// Sobe a API inteira em memória (mesmo Program.cs de produção) apontando para
// um banco PostgreSQL de teste. O Mercado Pago é trocado por gateways falsos,
// então nenhum teste faz cobrança real.
//
// Banco usado: variável de ambiente MERCADO_TEST_CONNECTION ou, se não existir,
// o PostgreSQL local padrão (postgres/postgres) com o banco "mercado_test".
// O banco de teste é apagado e recriado a cada execução — nunca aponte para o
// banco de desenvolvimento.
public class MercadoApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtKey = "chave-de-teste-com-pelo-menos-32-caracteres!!";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("MERCADO_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=mercado_test;Username=postgres;Password=postgres";

    public PixFalso Pix { get; } = new();

    public CartaoFalso Cartao { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Ambiente "Testing": não carrega os User Secrets nem o
        // appsettings.Development.json da sua máquina.
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IPagamentoPixService>(Pix);
            services.AddSingleton<IPagamentoCartaoService>(Cartao);
        });
    }

    public async Task InitializeAsync()
    {
        // Variáveis de ambiente têm prioridade sobre o appsettings.json e já
        // estão valendo quando o Program.cs lê a configuração.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "Mercado.Api");
        Environment.SetEnvironmentVariable("Jwt__Audience", "Mercado.Api");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new AppDbContext(options);

        // Garante um banco limpo; as migrations rodam no startup da API.
        await context.Database.EnsureDeletedAsync();

        _ = Server;
    }

    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();

    public async Task ExecutarNoBanco(Func<AppDbContext, Task> acao)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await acao(context);
    }

    public async Task<T> ConsultarBanco<T>(Func<AppDbContext, Task<T>> consulta)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await consulta(context);
    }

    public async Task LimparBanco()
    {
        await ExecutarNoBanco(context => context.Database.ExecuteSqlRawAsync("""
            TRUNCATE "ItensVenda", "Vendas", "Caixas", "Produtos",
                     "Auditorias", "RefreshTokens", "Usuarios"
            RESTART IDENTITY CASCADE;
            """));

        Pix.Reiniciar();
        Cartao.Reiniciar();
    }

    // Cria um usuário e devolve um HttpClient já autenticado com JWT válido.
    public async Task<HttpClient> CriarClienteAutenticado(string role = "Admin")
    {
        var usuario = new Usuario
        {
            Nome = "Operador de Teste",
            Email = $"operador-{Guid.NewGuid():N}@teste.local",
            Senha = BCrypt.Net.BCrypt.HashPassword("senha-teste"),
            Role = role
        };

        await ExecutarNoBanco(async context =>
        {
            context.Usuarios.Add(usuario);
            await context.SaveChangesAsync();
        });

        string token;
        using (var scope = Services.CreateScope())
        {
            token = scope.ServiceProvider
                .GetRequiredService<TokenService>()
                .GerarToken(usuario);
        }

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}
