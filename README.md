# Mercado — Sistema de PDV (Ponto de Venda)

Sistema full-stack de ponto de venda para mercado/loja física: controle de produtos e estoque, abertura/fechamento de caixa, vendas com pagamento em dinheiro, cartão ou PIX (integração real com Mercado Pago), dashboard, relatórios e emissão de NFC-e.

- **API**: [`Mercado.Api`](Mercado.Api) — .NET 9, PostgreSQL, JWT
- **Front-end**: [`mercado-web`](mercado-web) — Angular 20, Angular Material, Tailwind CSS

## Funcionalidades

- **Autenticação** com JWT + refresh token e controle de papéis (roles) de usuário
- **Produtos**: cadastro, edição, upload de imagem, soft delete, controle de estoque
- **Caixa**: abertura e fechamento, com conferência de valores por forma de pagamento
- **Vendas**: carrinho, múltiplas formas de pagamento
  - **Dinheiro/Débito/Crédito simples**: registrado direto, sem gateway
  - **PIX real**: gera QR Code via [Mercado Pago](https://www.mercadopago.com.br/developers) (API de Orders), com polling de status, expiração em 15 min e cancelamento — estoque só é baixado quando o pagamento é confirmado
  - **Cartão via Mercado Pago**: tokenização segura no front (o número do cartão nunca passa pelo back-end)
- **Notificações em tempo real** via SignalR (ex.: atualização de status de pagamento)
- **Dashboard** com gráficos (ApexCharts) de vendas e produtos mais vendidos
- **Relatórios** em PDF (QuestPDF)
- **Emissão de NFC-e** via [Focus NFe](https://focusnfe.com.br) (modelo implementado, aguardando CNPJ/homologação para teste ponta a ponta)

## Stack

| Camada | Tecnologias |
|---|---|
| Back-end | .NET 9, ASP.NET Core Web API, Entity Framework Core, PostgreSQL (Npgsql), SignalR, JWT Bearer, BCrypt, QuestPDF, Swagger/OpenAPI |
| Front-end | Angular 20, Angular Material, Angular CDK, Tailwind CSS, ApexCharts, SignalR client, RxJS |
| Integrações | Mercado Pago (PIX e cartão via API de Orders), Focus NFe (NFC-e) |

## Como rodar localmente

### Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20+ e [Angular CLI](https://angular.dev/tools/cli) (`npm install -g @angular/cli`)
- PostgreSQL rodando localmente

### 1. API (`Mercado.Api`)

```bash
cd Mercado.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=mercado_db;Username=SEU_USUARIO;Password=SUA_SENHA"
dotnet user-secrets set "Jwt:Key" "uma-chave-secreta-longa-qualquer"
dotnet run
```

`appsettings.json` no repositório traz apenas placeholders — configure os valores reais via **User Secrets** (comandos acima) ou variáveis de ambiente, nunca direto no arquivo versionado.

Para configurar a integração real de PIX/cartão via Mercado Pago (opcional, só necessário para testar pagamentos), veja o passo a passo detalhado em [`Mercado.Api/README.md`](Mercado.Api/README.md).

A API sobe com Swagger habilitado em `/swagger` e aplica as migrations do banco automaticamente ao iniciar.

### 2. Front-end (`mercado-web`)

```bash
cd mercado-web
npm install
ng serve
```

Abra `http://localhost:4200`. O front já está configurado para consumir a API em `http://localhost:5xxx` (ver `src/environments/environment.ts`) e o CORS da API libera `http://localhost:4200`.

## Estrutura do repositório

```
Mercado/
├── Mercado.Api/     # API .NET (controllers, services, entities, migrations)
└── mercado-web/     # Front Angular (features, core, layout)
```

## Documentação adicional

- [`Mercado.Api/README.md`](Mercado.Api/README.md) — detalhes da integração de pagamento PIX/cartão com Mercado Pago (fluxo, configuração de sandbox, status de produção) e da emissão de NFC-e via Focus NFe.
