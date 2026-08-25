using Mercado.Api.Context;
using Mercado.Api.Extensions;
using Mercado.Api.Hubs;
using Mercado.Api.Repositories;
using Mercado.Api.Services;
using MercadoPago.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using System.Text;

namespace Mercado.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Angular",
                policy =>
                {
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithOrigins(
                            "http://localhost:4200");
                });
        });

        builder.Services.AddControllers();

        builder.Services.AddSignalR();

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer",
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Digite: Bearer {seu token}"
                });

            options.AddSecurityRequirement(
                new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
                {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference =
                        new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type =
                                Microsoft.OpenApi.Models.ReferenceType
                                    .SecurityScheme,
                            Id = "Bearer"
                        }
                },
                Array.Empty<string>()
            }
                });
        });

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection")
            );
        });

        builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();

        builder.Services.AddScoped<IProdutoService, ProdutoService>();

        builder.Services.AddScoped<AuditoriaService>();

        var key = Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:Key"]!
        );

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = builder.Configuration["Jwt:Issuer"],

                        ValidAudience = builder.Configuration["Jwt:Audience"],

                        IssuerSigningKey =
                            new SymmetricSecurityKey(key)
                    };
            });

        builder.Services.AddScoped<TokenService>();

        builder.Services.AddScoped<RefreshTokenService>();

        builder.Services.AddScoped<PagamentoPixService>();

        builder.Services.AddScoped<PagamentoCartaoService>();

        // Homologação: token da empresa fictícia cadastrada no painel de
        // testes da Focus NFe, via `dotnet user-secrets set "Nfce:FocusNfeToken"
        // "..."`. Sem CNPJ próprio ainda, então não há credencial de produção
        // configurada (ver README).
        builder.Services.AddHttpClient<NfceService>();

        // Homologação: token TEST-... via `dotnet user-secrets set
        // "MercadoPago:AccessToken" "..."`. Produção: variável de ambiente
        // MercadoPago__AccessToken com o token de produção (APP_USR-...).
        MercadoPagoConfig.AccessToken =
            builder.Configuration["MercadoPago:AccessToken"];

        QuestPDF.Settings.License = LicenseType.Community;

        var app = builder.Build();

        app.UseCors("Angular");

        app.UseSwagger();

        app.UseSwaggerUI();

        app.UseGlobalException();

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Database.Migrate();
        }

        app.MapHub<NotificacaoHub>("/notificacoes");

        app.Run();
    }
}