# Mercado.Api

API do sistema de PDV (ponto de venda) do Mercado — .NET 9 + PostgreSQL + Angular (`mercado-web`).

Este README documenta a integração de **pagamento PIX real via Mercado Pago**: como foi implementada, como configurar o ambiente de homologação (teste) e como gerar/"pagar" uma cobrança sem sair do seu computador.

---

## Como funciona a integração PIX

Dinheiro, Débito e Crédito continuam **simulados** (o caixa escolhe a forma e a venda é registrada na hora, sem gateway nenhum). **Só o PIX processa de verdade**, via [Mercado Pago](https://www.mercadopago.com.br/developers), usando a **API de Orders** (`/v1/orders`) — não a API de Pagamentos legada (`/v1/payments`), que **não simula PIX em sandbox** (mais detalhes em [Por que API de Orders e não API de Pagamentos?](#por-que-api-de-orders-e-não-api-de-pagamentos)).

### Fluxo

```
Caixa aperta F2 (PIX)
        │
        ▼
POST /api/vendas/pix/iniciar          Mercado.Api → Mercado Pago
        │                              cria a Venda (StatusPagamento = Pendente,
        │                              estoque NÃO é baixado ainda) e pede um
        │                              QR Code PIX ao Mercado Pago
        ▼
Frontend mostra o QR Code + código copia-e-cola
        │
        ▼
A cada 3s: GET /api/vendas/pix/{vendaId}/status
        │                              Mercado.Api consulta o pedido no
        │                              Mercado Pago
        ▼
   status = "processed"?
        │
   ┌────┴────┐
  sim        não (continua "action_required")
   │           → segue tentando a cada 3s até aprovar,
   ▼             expirar (15 min) ou o caixa cancelar
Estoque é baixado (atômico, por produto)
StatusPagamento = Aprovado
Carrinho limpo, venda concluída
```

Pontos importantes de design:

- **O estoque só é baixado quando o pagamento é confirmado** — evita tirar produto do estoque por uma cobrança PIX que o cliente nunca pagou.
- **Vendas PIX pendentes/recusadas não entram no fechamento de caixa** (`CaixaController.Fechar` filtra por `StatusPagamento == Aprovado`).
- Se o Mercado Pago falhar ao gerar a cobrança (rede, credencial inválida, etc.), a `Venda` criada é **revertida via transação** — não fica lixo pendurado no banco.

### Arquivos principais

| Arquivo | O que faz |
|---|---|
| [`Services/PagamentoPixService.cs`](Services/PagamentoPixService.cs) | Encapsula o SDK do Mercado Pago (`OrderClient`) — cria a cobrança e consulta status |
| [`Controllers/VendasController.cs`](Controllers/VendasController.cs) | Endpoints `pix/iniciar`, `pix/{id}/status`, `pix/{id}/cancelar` |
| [`Entities/Venda.cs`](Entities/Venda.cs) | Campos `StatusPagamento` e `PagamentoExternoId` (id do pedido no Mercado Pago) |
| [`Enums/StatusPagamentoEnum.cs`](Enums/StatusPagamentoEnum.cs) | `Pendente`, `Aprovado`, `Recusado`, `Cancelado` |
| `mercado-web/.../nova-venda/` | Modal do QR Code, polling a cada 3s, contagem regressiva de 15 min |
| `mercado-web/.../venda.ts` (service) | `iniciarPagamentoPix`, `consultarStatusPix`, `cancelarPagamentoPix` |

---

## Configuração (homologação/sandbox)

Todas as credenciais ficam em **User Secrets** (não vão pro `appsettings.json`, não vão pro git). Rode a partir da pasta `Mercado.Api`:

```bash
dotnet user-secrets set "MercadoPago:AccessToken" "APP_USR-xxxxxxxx"
dotnet user-secrets set "MercadoPago:EmailPagadorPadrao" "test_user_XXXXXXXXX@testuser.com"
dotnet user-secrets set "MercadoPago:NomePagadorTeste" "APRO"
```

| Chave | Para que serve | Em produção |
|---|---|---|
| `MercadoPago:AccessToken` | Credencial da API | Variável de ambiente `MercadoPago__AccessToken` com o token real de produção |
| `MercadoPago:EmailPagadorPadrao` | E-mail do pagador exigido pela API (não existe captura de e-mail do cliente no caixa físico) | Trocar para um e-mail genérico da própria loja |
| `MercadoPago:NomePagadorTeste` | Valor mágico `"APRO"` que faz o Mercado Pago **aprovar automaticamente** o PIX em sandbox | **Não configurar esta chave** — deixá-la ausente faz o código não enviar `first_name`, que é o comportamento correto em produção |

Depois de configurar, reinicie a API (`dotnet run` ou reinicie pela Visual Studio) para carregar os secrets.

### Como conseguir o Access Token de teste

O token da **sua conta normal** não funciona para simular PIX (dá `401 Unauthorized use of live credentials`) — precisa ser o token de uma **conta de teste**. Passo a passo:

1. Acesse [mercadopago.com.br/developers/panel](https://www.mercadopago.com.br/developers/panel) com sua conta normal.
2. Vá em **"Contas de teste"** → crie um **vendedor de teste** e um **comprador de teste** (se ainda não tiver). A Mercado Pago gera usuário/senha fake pra cada um.
3. Abra uma **janela anônima** do navegador e faça login em mercadopago.com.br **com o vendedor de teste**.
4. Nessa sessão, crie (ou abra) uma aplicação do tipo **"Pagamentos online"** → **"Checkout Transparente"** (não escolha "Pagos offline"/"QR Code" — esse tipo é para maquininha/Point físico e não funciona com o código deste projeto).
5. Na aplicação, clique em **"Credenciais de produção"** (sim, "de produção" — mesmo sendo tudo teste, porque você já está *dentro* de uma conta de teste; é assim que a Mercado Pago funciona). Copie o Access Token (`APP_USR-...`).
6. Para o `EmailPagadorPadrao`, use o padrão `test_user_<User ID do comprador de teste>@testuser.com` (o User ID aparece na tela "Contas de teste").

---

## Como testar (gerar e "pagar" o PIX)

Não existe app de banco de verdade escaneando nada em homologação — o Mercado Pago **simula a aprovação sozinho** quando reconhece que é um teste.

1. Suba a API (`dotnet run`) e o front (`ng serve` dentro de `mercado-web`).
2. Abra um caixa (`POST /api/caixa/abrir` ou pela tela, se existir).
3. Vá em **Vendas → Nova Venda**, escaneie/adicione um produto.
4. Clique em **Finalizar Venda** → **PIX** (ou tecla **F2**).
5. O QR Code real aparece na tela (gerado pelo Mercado Pago).
6. **Espere 3–10 segundos.** O frontend consulta o status a cada 3s; como `NomePagadorTeste=APRO` está configurado, o Mercado Pago aprova sozinho — o carrinho limpa automaticamente e a venda aparece como **"Aprovado"** em `/vendas`.

Não precisa escanear o QR Code com celular nenhum para isso funcionar — a aprovação automática do sandbox já cobre o teste do fluxo completo (geração do QR, polling, baixa de estoque, atualização de status).

### Testando cancelamento/expiração

Como a simulação do Mercado Pago só cobre o caminho de sucesso (não documentam um valor mágico equivalente a "recusado" para PIX), os outros dois estados são testados pelo **nosso próprio código**, sem depender do Mercado Pago:

- **Cancelar manualmente**: com o QR Code na tela, clique em "Cancelar" (ou tecla `ESC`) → chama `POST /api/vendas/pix/{id}/cancelar`, a venda vira `Cancelado`.
- **Expirar**: espere os 15 minutos da contagem regressiva (ou reduza `MercadoPago:MinutosExpiracaoPix` temporariamente para testar mais rápido) → o frontend cancela automaticamente ao chegar a zero.

### Verificando direto pela API do Mercado Pago (debug)

Se quiser inspecionar o pedido bruto que o Mercado Pago está vendo:

```bash
# Rode dentro da pasta Mercado.Api
TOKEN=$(dotnet user-secrets list | grep "^MercadoPago:AccessToken" | sed 's/.*= //')
curl -s "https://api.mercadopago.com/v1/orders/{PagamentoExternoId}" \
  -H "Authorization: Bearer $TOKEN"
```

O `PagamentoExternoId` de uma venda fica salvo na tabela `Vendas` (coluna do mesmo nome).

---

## Por que API de Orders e não API de Pagamentos?

Na primeira tentativa, a integração usava a API de Pagamentos clássica (`PaymentClient`, endpoint `/v1/payments`), que é mais simples e mais documentada por aí. **Ela não funciona para simular PIX em sandbox** — toda tentativa de criar uma cobrança PIX de teste retorna `401 Unauthorized use of live credentials`, não importa a credencial usada (testamos conta pessoal, conta de teste tipo "QR Code"/Point, e conta de teste tipo "Pagamentos online" — todas com o mesmo erro).

A documentação oficial de teste de PIX da Mercado Pago ([`checkout-api-orders/integration-test/pix`](https://www.mercadopago.com.br/developers/pt/docs/checkout-api-orders/integration-test/pix)) só existe para a **API de Orders**, e usa o valor mágico `payer.first_name = "APRO"` para disparar a aprovação automática — exatamente o mecanismo implementado aqui. Por isso o código usa `OrderClient` (`Client.Order`) em vez de `PaymentClient` (`Client.Payment`).

Detalhe técnico: o campo `expiration_time` da API de Orders espera uma **duração ISO 8601** (ex.: `"PT15M"`), não uma data — diferente do `date_of_expiration` da API de Pagamentos, que é uma data absoluta.

---

## Vocabulário de status (API de Orders)

Diferente da API de Pagamentos (`approved`/`rejected`/`cancelled`), a API de Orders usa outros nomes. O mapeamento está em `VendasController.ConsultarStatusPix`:

| Status do pedido (Mercado Pago) | `StatusPagamentoEnum` (nosso) |
|---|---|
| `processed` | `Aprovado` |
| `action_required` | continua `Pendente` (aguardando pagamento) |
| `expired` | `Cancelado` |
| `canceled` / `cancelled` | `Cancelado` |

---

## Checklist para produção

- [ ] `MercadoPago__AccessToken` (variável de ambiente do servidor) com o token de produção real (`APP_USR-...` da conta de verdade, não de teste)
- [ ] `MercadoPago:NomePagadorTeste` **ausente** da configuração (não copiar o `dotnet user-secrets` de dev)
- [ ] `MercadoPago:EmailPagadorPadrao` ajustado para um e-mail real da loja
- [ ] Testar uma venda PIX de valor baixo (ex.: R$ 1,00) com dinheiro de verdade antes de liberar pro caixa usar no dia a dia

---

## Cartão de débito/crédito real (Mercado Pago) — status: implementado, ainda não testado ponta a ponta

Assim como o PIX, usa a API de Orders do Mercado Pago — mas o resultado (aprovado/recusado) já vem na própria resposta da criação do pedido, sem polling.

### O que já existe

| Arquivo | O que faz |
|---|---|
| [`Services/PagamentoCartaoService.cs`](Services/PagamentoCartaoService.cs) | Monta o payload do pedido com o token do cartão e lê o resultado (`approved`/`rejected`) |
| [`Controllers/VendasController.cs`](Controllers/VendasController.cs) | Endpoint `POST /api/vendas/cartao/processar` |
| `mercado-web/.../nova-venda/` | Formulário de cartão usando o **SDK JS do Mercado Pago** (`cardForm`) — número, validade e CVV ficam em campos seguros (iframe) do próprio Mercado Pago, o número do cartão nunca passa pelo nosso frontend/backend |

O número do cartão **nunca chega na Mercado.Api** — o frontend tokeniza via SDK deles e só envia o `cardToken` (mais bandeira, parcelas e CPF opcional). Isso é exigido pela Mercado Pago (não dá pra mandar PAN cru pra API deles de outro jeito) e também é o que mantém o projeto fora do escopo de PCI-DSS mais pesado.

### Configuração necessária

Diferente do Access Token (secreto), a **Public Key** do Mercado Pago é destinada a ficar no frontend — não é segredo. Pegue no mesmo lugar de onde tirou o Access Token do PIX (aplicação de teste → "Credenciais de produção") e configure em [`mercado-web/src/environments/environment.ts`](../mercado-web/src/environments/environment.ts):

```ts
mercadoPagoPublicKey: 'TEST-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'
```

Sem isso configurado, o formulário de cartão mostra um erro ao abrir em vez de tentar carregar o SDK sem chave.

### Diferença de fluxo em relação ao Dinheiro/PIX

- **Dinheiro**: sempre nasce `Aprovado`, nunca fala com gateway nenhum.
- **PIX**: nasce `Pendente`, só vira `Aprovado` depois de um polling assíncrono.
- **Cartão**: nasce `Pendente`, mas normalmente já sai `Aprovado` ou `Recusado` na mesma requisição — não deveria ficar pendente por muito tempo (a Order API de cartão é síncrona).

O estoque só é baixado se aprovado, reaproveitando o mesmo método (`AprovarPagamento`, ex-`AprovarPagamentoPix`) usado pelo PIX.

### Pendências conhecidas

- **Nunca foi testado contra a API de verdade** (nem em sandbox) — foi escrito com base na documentação/XML do SDK oficial (`mercadopago-sdk` 3.5.0), mas o formato exato da resposta para cartão (nomes de `StatusDetail`, se `payment_method_id`/`issuer_id` são obrigatórios) só se confirma testando.
- O campo de CPF no formulário está como opcional no backend, mas o SDK do Mercado Pago pode exigir esse campo preenchido para gerar o token (não confirmado ainda).
- Sem tratamento de 3DS (autenticação adicional do banco emissor) — se algum emissor exigir, o fluxo atual provavelmente falha sem uma mensagem clara.
- Sem estorno/cancelamento de cartão.

---

## Emissão de NFC-e (Focus NFe) — status: modelo pronto, integração ainda não testada

O projeto ainda **não tem CNPJ próprio** (está em fase de desenvolvimento), então esta parte só pode ser testada contra o **ambiente de homologação** da [Focus NFe](https://focusnfe.com.br), usando uma empresa fictícia. Não dá pra emitir NFC-e de verdade sem CNPJ ativo e credenciamento na SEFAZ do estado — isso fica para quando o mercado sair do papel.

### O que já existe no código

| Arquivo | O que faz |
|---|---|
| [`Services/NfceService.cs`](Services/NfceService.cs) | Encapsula a API REST da Focus NFe — monta o payload da nota e consulta status |
| [`Controllers/VendasController.cs`](Controllers/VendasController.cs) | Endpoints `{vendaId}/nfce/emitir`, `{vendaId}/nfce/status` |
| [`Entities/Venda.cs`](Entities/Venda.cs) | Campos `StatusEmissaoNfce`, `ReferenciaNfce`, `ChaveAcessoNfce`, `LinkDanfeNfce`, `MotivoRejeicaoNfce`, `CpfCnpjCliente` |
| [`Entities/Produto.cs`](Entities/Produto.cs) | Campos fiscais `Ncm`, `Cfop`, `Cest`, `UnidadeMedida`, `OrigemMercadoria` — **precisam ser preenchidos produto a produto** antes de qualquer emissão, mesmo em homologação |
| [`Enums/StatusEmissaoNfceEnum.cs`](Enums/StatusEmissaoNfceEnum.cs) | `NaoEmitida`, `Processando`, `Autorizada`, `Rejeitada`, `Cancelada` |

O fluxo espelha o do PIX: `emitir` cria a referência e chama a Focus NFe; `status` consulta e atualiza a venda. Diferença importante: **uma nota autorizada não pode ser editada**, só cancelada (e o prazo de cancelamento é curto, geralmente ~30 min — varia por estado). Isso ainda não está implementado (não existe endpoint de cancelamento).

### O que falta para sequer testar em homologação (passos manuais, fora do código)

1. Criar conta em [focusnfe.com.br](https://focusnfe.com.br) (plano gratuito de homologação existe).
2. No painel deles, cadastrar uma **empresa de teste** — a própria Focus NFe orienta como preencher CNPJ/IE fictícios válidos para homologação (isso muda conforme a política deles, então siga a documentação atual no painel, não este README).
3. Verificar se a empresa de teste exige **certificado digital A1** carregado no painel para assinar a NFC-e em homologação (na dúvida, checar a documentação oficial deles — não foi validado neste projeto ainda).
4. Copiar o **token de homologação** gerado e configurar via User Secrets:

```bash
dotnet user-secrets set "Nfce:FocusNfeToken" "seu-token-de-homologacao"
dotnet user-secrets set "Nfce:EmpresaCnpj" "cnpj-da-empresa-de-teste"
dotnet user-secrets set "Nfce:EmpresaRazaoSocial" "razão social da empresa de teste"
dotnet user-secrets set "Nfce:EmpresaInscricaoEstadual" "IE da empresa de teste"
```

5. Preencher `Ncm`, `Cfop`, `Cest` (se aplicável) de pelo menos um produto de teste — sem isso a Focus NFe rejeita a nota.
6. Testar `POST /api/vendas/{id}/nfce/emitir` numa venda aprovada e acompanhar via `GET /api/vendas/{id}/nfce/status`.

### Pendências conhecidas (não faz sentido resolver agora, sem empresa real)

- Endpoint de cancelamento de NFC-e.
- CSOSN fixo em `102` no `NfceService` — é só um ponto de partida para Simples Nacional; a regra real por produto precisa vir de configuração/contador antes de produção.
- Sem entidade de configuração da empresa emitente no banco — hoje é tudo via `IConfiguration` (`Nfce:Empresa*`), o que é suficiente para uma única loja, mas precisaria revisão se o sistema um dia atender mais de uma empresa/filial.
- Nenhuma emissão foi de fato testada contra a Focus NFe ainda — o código foi escrito com base na documentação pública da API deles, não validado end-to-end.
