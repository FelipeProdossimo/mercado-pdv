import {
  AfterViewInit,
  Component,
  ElementRef,
  HostListener,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
} from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProdutoService } from '../../../core/services/produto';
import { VendaService } from '../../../core/services/venda';
import { Produto } from '../../../core/models/produto.model';
import { FormaPagamento } from '../../../core/enums/forma-pagamento.model';
import { StatusPagamento } from '../../../core/enums/status-pagamento.model';
import { PagamentoPixResposta } from '../../../core/models/pagamento-pix.model';
import { ProcessarPagamentoCartao } from '../../../core/models/pagamento-cartao.model';
import { environment } from '../../../../environments/environment';

// Carregado via <script> em index.html (SDK oficial do Mercado Pago).
declare const MercadoPago: any;

interface ItemCarrinho {
  produtoId: number;
  descricao: string;
  codigoBarras: string;
  quantidade: number;
  valorUnitario: number;
  total: number;
}

@Component({
  selector: 'app-nova-venda',
  imports: [CommonModule, FormsModule, CurrencyPipe],
  templateUrl: './nova-venda.html',
  styleUrl: './nova-venda.scss',
})
export class NovaVenda implements OnInit, AfterViewInit, OnDestroy {
  private produtoService = inject(ProdutoService);
  private vendaService = inject(VendaService);

  @ViewChild('pesquisa') private pesquisaInput?: ElementRef<HTMLInputElement>;

  public readonly FormaPagamento = FormaPagamento;

  public produtos: Produto[] = [];
  public produtoSelecionadoId = 0;
  public quantidade = 1;
  public popupFormaPagamento = false;
  public formaPagamento = FormaPagamento.Dinheiro;
  public textoPesquisa = '';
  public resultadosPesquisa: Produto[] = [];
  public enviando = false;
  public mensagem: { tipo: 'sucesso' | 'erro'; texto: string } | null = null;
  public itemDestacadoId: number | null = null;
  public itens: ItemCarrinho[] = [];

  public mostrandoQrPix = false;
  public pagamentoPix: PagamentoPixResposta | null = null;
  public segundosRestantesPix = 0;

  public mostrandoFormCartao = false;

  private timeoutMensagem?: ReturnType<typeof setTimeout>;
  private timeoutDestaque?: ReturnType<typeof setTimeout>;
  private intervalPollingPix?: ReturnType<typeof setInterval>;
  private intervalContagemPix?: ReturnType<typeof setInterval>;
  private mp?: any;
  private cardForm?: any;

  private readonly teclasPagamento: Record<string, number> = {
    F1: FormaPagamento.Dinheiro,
    F2: FormaPagamento.Pix,
    F3: FormaPagamento.CartaoDebito,
    F4: FormaPagamento.CartaoCredito,
  };

  ngOnInit(): void {
    this.produtoService.obterTodosSemPaginacao().subscribe({
      next: (response) => {
        this.produtos = response.dados;
      },
    });
  }

  ngAfterViewInit(): void {
    this.focarPesquisa();
  }

  ngOnDestroy(): void {
    this.pararPollingPix();
    this.pararContagemPix();
    clearTimeout(this.timeoutMensagem);
    clearTimeout(this.timeoutDestaque);
  }

  @HostListener('window:keydown', ['$event'])
  aoPressionarTecla(event: KeyboardEvent): void {
    if (this.mostrandoQrPix) {
      if (event.key === 'Escape') {
        event.preventDefault();
        this.cancelarPix();
      }

      return;
    }

    if (this.mostrandoFormCartao) {
      if (event.key === 'Escape') {
        event.preventDefault();
        this.fecharFormCartao();
      }

      return;
    }

    if (this.popupFormaPagamento) {
      if (event.key in this.teclasPagamento) {
        event.preventDefault();
        this.selecionarPagamento(this.teclasPagamento[event.key]);
        return;
      }

      if (event.key === 'Escape') {
        event.preventDefault();
        this.popupFormaPagamento = false;
        this.focarPesquisa();
      }

      return;
    }

    if (event.key === 'Escape' && this.textoPesquisa) {
      event.preventDefault();
      this.limparBusca();
    }
  }

  pesquisarProdutos(): void {
    const texto = this.textoPesquisa.trim().toLowerCase();

    if (!texto) {
      this.resultadosPesquisa = [];
      return;
    }

    // Leitor de código de barras: se houver correspondência exata, ela é o
    // único resultado — evita pegar um item errado por match parcial no nome.
    const exato = this.produtos.find((x) => x.codigoBarras.toLowerCase() === texto);

    if (exato) {
      this.resultadosPesquisa = [exato];
      return;
    }

    this.resultadosPesquisa = this.produtos
      .filter((x) => x.descricao.toLowerCase().includes(texto) || x.codigoBarras.includes(texto))
      .slice(0, 8);
  }

  selecionarProduto(produto: Produto): void {
    this.produtoSelecionadoId = produto.id;

    this.adicionarItem();
  }

  confirmarBusca(): void {
    if (this.resultadosPesquisa.length) {
      this.selecionarProduto(this.resultadosPesquisa[0]);
      return;
    }

    if (this.textoPesquisa.trim()) {
      this.mostrarMensagem('erro', `Nenhum produto encontrado para "${this.textoPesquisa.trim()}"`);
      this.tocarSom(false);
    }
  }

  adicionarItem(): void {
    const produto = this.produtos.find((x) => x.id === Number(this.produtoSelecionadoId));

    if (!produto) {
      return;
    }

    const quantidadeDesejada = this.quantidade > 0 ? this.quantidade : 1;
    const itemExistente = this.itens.find((x) => x.produtoId === produto.id);
    const quantidadeJaNoCarrinho = itemExistente?.quantidade ?? 0;

    if (quantidadeJaNoCarrinho + quantidadeDesejada > produto.estoque) {
      this.mostrarMensagem(
        'erro',
        `Estoque insuficiente para ${produto.descricao} (disponível: ${produto.estoque})`,
      );
      this.tocarSom(false);
      this.limparBusca();
      return;
    }

    if (itemExistente) {
      itemExistente.quantidade += quantidadeDesejada;
      itemExistente.total = itemExistente.quantidade * itemExistente.valorUnitario;
    } else {
      this.itens = [
        ...this.itens,
        {
          produtoId: produto.id,
          descricao: produto.descricao,
          codigoBarras: produto.codigoBarras,
          quantidade: quantidadeDesejada,
          valorUnitario: produto.valor,
          total: produto.valor * quantidadeDesejada,
        },
      ];
    }

    this.destacarItem(produto.id);
    this.tocarSom(true);
    this.limparBusca();
  }

  alterarQuantidade(item: ItemCarrinho, delta: number): void {
    const novaQuantidade = item.quantidade + delta;

    if (novaQuantidade <= 0) {
      this.removerItem(item);
      return;
    }

    const produto = this.produtos.find((x) => x.id === item.produtoId);

    if (produto && novaQuantidade > produto.estoque) {
      this.mostrarMensagem('erro', `Estoque insuficiente (disponível: ${produto.estoque})`);
      this.tocarSom(false);
      return;
    }

    item.quantidade = novaQuantidade;
    item.total = item.quantidade * item.valorUnitario;
  }

  estoqueDisponivel(item: ItemCarrinho): number {
    return this.produtos.find((x) => x.id === item.produtoId)?.estoque ?? item.quantidade;
  }

  get valorTotal(): number {
    return this.itens.reduce((soma, item) => soma + item.total, 0);
  }

  get totalPecas(): number {
    return this.itens.reduce((soma, item) => soma + item.quantidade, 0);
  }

  get contagemPixFormatada(): string {
    const minutos = Math.floor(this.segundosRestantesPix / 60);
    const segundos = this.segundosRestantesPix % 60;
    return `${minutos}:${segundos.toString().padStart(2, '0')}`;
  }

  removerItem(item: ItemCarrinho): void {
    this.itens = this.itens.filter((x) => x !== item);
    this.focarPesquisa();
  }

  selecionarPagamento(id: number): void {
    this.formaPagamento = id;
    this.popupFormaPagamento = false;

    if (id === FormaPagamento.Pix) {
      this.iniciarPagamentoPix();
      return;
    }

    if (id === FormaPagamento.CartaoDebito || id === FormaPagamento.CartaoCredito) {
      this.abrirFormCartao();
      return;
    }

    this.finalizarVendaConfirmada();
  }

  finalizarVendaConfirmada(): void {
    if (this.itens.length === 0) {
      this.mostrarMensagem('erro', 'Adicione itens à venda');

      return;
    }

    if (this.enviando) {
      return;
    }

    const venda = {
      itens: this.itens.map((item) => ({
        produtoId: item.produtoId,
        quantidade: item.quantidade,
      })),
      formaPagamento: this.formaPagamento,
    };

    this.enviando = true;

    this.vendaService.criar(venda).subscribe({
      next: () => {
        this.enviando = false;
        this.mostrarMensagem('sucesso', 'Venda realizada com sucesso');
        this.itens = [];
        this.focarPesquisa();
      },
      error: (erro) => {
        this.enviando = false;

        const texto =
          typeof erro?.error === 'string'
            ? erro.error
            : 'Não foi possível concluir a venda. Tente novamente.';

        this.mostrarMensagem('erro', texto);
        this.tocarSom(false);
      },
    });
  }

  // --- PIX real (Mercado Pago) -------------------------------------------

  iniciarPagamentoPix(): void {
    if (this.itens.length === 0) {
      this.mostrarMensagem('erro', 'Adicione itens à venda');
      return;
    }

    if (this.enviando) {
      return;
    }

    this.enviando = true;

    const dto = {
      itens: this.itens.map((item) => ({
        produtoId: item.produtoId,
        quantidade: item.quantidade,
      })),
    };

    this.vendaService.iniciarPagamentoPix(dto).subscribe({
      next: (resposta) => {
        this.enviando = false;
        this.pagamentoPix = resposta;
        this.mostrandoQrPix = true;
        this.iniciarContagemPix(resposta.expiraEm);
        this.iniciarPollingPix(resposta.vendaId);
      },
      error: (erro) => {
        this.enviando = false;

        const texto =
          typeof erro?.error === 'string'
            ? erro.error
            : 'Não foi possível gerar a cobrança PIX. Tente novamente.';

        this.mostrarMensagem('erro', texto);
        this.tocarSom(false);
      },
    });
  }

  cancelarPix(): void {
    const pagamento = this.pagamentoPix;

    this.encerrarTelaPix();

    if (pagamento) {
      this.vendaService.cancelarPagamentoPix(pagamento.vendaId).subscribe();
    }

    this.focarPesquisa();
  }

  copiarCodigoPix(): void {
    if (!this.pagamentoPix) {
      return;
    }

    navigator.clipboard
      ?.writeText(this.pagamentoPix.qrCode)
      .then(() => this.mostrarMensagem('sucesso', 'Código PIX copiado'))
      .catch(() => undefined);
  }

  private iniciarPollingPix(vendaId: number): void {
    this.pararPollingPix();

    this.intervalPollingPix = setInterval(() => {
      this.vendaService.consultarStatusPix(vendaId).subscribe({
        next: (resposta) => this.tratarStatusPix(resposta.status),
        error: () => undefined,
      });
    }, 3000);
  }

  private tratarStatusPix(status: StatusPagamento): void {
    if (status === StatusPagamento.Pendente) {
      return;
    }

    this.encerrarTelaPix();

    if (status === StatusPagamento.Aprovado) {
      this.tocarSom(true);
      this.mostrarMensagem('sucesso', 'Pagamento PIX aprovado! Venda concluída.');
      this.itens = [];
      this.focarPesquisa();
      return;
    }

    this.tocarSom(false);
    this.mostrarMensagem(
      'erro',
      status === StatusPagamento.Recusado ? 'Pagamento PIX recusado.' : 'Pagamento PIX cancelado.',
    );
  }

  private iniciarContagemPix(expiraEm: string): void {
    this.pararContagemPix();

    const atualizar = () => {
      const restante = Math.max(0, Math.floor((new Date(expiraEm).getTime() - Date.now()) / 1000));

      this.segundosRestantesPix = restante;

      if (restante === 0) {
        const vendaId = this.pagamentoPix?.vendaId;

        this.encerrarTelaPix();
        this.mostrarMensagem('erro', 'Tempo para pagamento PIX expirado.');
        this.tocarSom(false);

        if (vendaId) {
          this.vendaService.cancelarPagamentoPix(vendaId).subscribe();
        }
      }
    };

    atualizar();

    this.intervalContagemPix = setInterval(atualizar, 1000);
  }

  private encerrarTelaPix(): void {
    this.pararPollingPix();
    this.pararContagemPix();
    this.mostrandoQrPix = false;
    this.pagamentoPix = null;
  }

  private pararPollingPix(): void {
    if (this.intervalPollingPix) {
      clearInterval(this.intervalPollingPix);
      this.intervalPollingPix = undefined;
    }
  }

  private pararContagemPix(): void {
    if (this.intervalContagemPix) {
      clearInterval(this.intervalContagemPix);
      this.intervalContagemPix = undefined;
    }
  }

  // --- Cartão real (Mercado Pago) -----------------------------------------

  abrirFormCartao(): void {
    if (this.itens.length === 0) {
      this.mostrarMensagem('erro', 'Adicione itens à venda');
      return;
    }

    if (!environment.mercadoPagoPublicKey) {
      this.mostrarMensagem(
        'erro',
        'Chave pública do Mercado Pago não configurada (environment.mercadoPagoPublicKey).',
      );
      return;
    }

    this.mostrandoFormCartao = true;

    // O formulário só existe no DOM depois do próximo ciclo de detecção de
    // mudanças do Angular (o *ngIf acabou de virar true) — o SDK do Mercado
    // Pago precisa dos elementos já montados para se anexar a eles.
    setTimeout(() => this.iniciarCardForm(), 0);
  }

  fecharFormCartao(): void {
    this.mostrandoFormCartao = false;
    this.cardForm = undefined;
    this.focarPesquisa();
  }

  get exibeParcelasCartao(): boolean {
    return this.formaPagamento === FormaPagamento.CartaoCredito;
  }

  private iniciarCardForm(): void {
    if (!this.mp) {
      this.mp = new MercadoPago(environment.mercadoPagoPublicKey);
    }

    this.cardForm = this.mp.cardForm({
      amount: String(this.valorTotal),
      iframe: true,
      form: {
        id: 'form-cartao',
        cardNumber: { id: 'form-cartao__cardNumber', placeholder: 'Número do cartão' },
        expirationDate: { id: 'form-cartao__expirationDate', placeholder: 'MM/AA' },
        securityCode: { id: 'form-cartao__securityCode', placeholder: 'CVV' },
        cardholderName: { id: 'form-cartao__cardholderName', placeholder: 'Nome impresso no cartão' },
        issuer: { id: 'form-cartao__issuer', placeholder: 'Banco emissor' },
        installments: { id: 'form-cartao__installments', placeholder: 'Parcelas' },
        identificationType: { id: 'form-cartao__identificationType', placeholder: 'Tipo de documento' },
        identificationNumber: { id: 'form-cartao__identificationNumber', placeholder: 'CPF (opcional)' },
        cardholderEmail: { id: 'form-cartao__cardholderEmail', placeholder: 'E-mail' },
      },
      callbacks: {
        onFormMounted: (error: unknown) => {
          if (error) {
            this.mostrarMensagem('erro', 'Não foi possível carregar o formulário do cartão.');
            this.fecharFormCartao();
          }
        },
        onSubmit: (event: Event) => {
          event.preventDefault();
          this.confirmarPagamentoCartao();
        },
      },
    });
  }

  confirmarPagamentoCartao(): void {
    if (this.enviando || !this.cardForm) {
      return;
    }

    const dados = this.cardForm.getCardFormData();

    if (!dados?.token) {
      this.mostrarMensagem('erro', 'Preencha os dados do cartão corretamente.');
      return;
    }

    this.enviando = true;

    const dto: ProcessarPagamentoCartao = {
      itens: this.itens.map((item) => ({
        produtoId: item.produtoId,
        quantidade: item.quantidade,
      })),
      formaPagamento: this.formaPagamento,
      cardToken: dados.token,
      paymentMethodId: dados.paymentMethodId,
      parcelas: this.exibeParcelasCartao ? Number(dados.installments) : 1,
      cpfCliente: dados.identificationNumber || undefined,
    };

    this.vendaService.processarPagamentoCartao(dto).subscribe({
      next: (resposta) => {
        this.enviando = false;
        this.fecharFormCartao();

        if (resposta.statusPagamento === StatusPagamento.Aprovado) {
          this.tocarSom(true);
          this.mostrarMensagem('sucesso', 'Pagamento aprovado! Venda concluída.');
          this.itens = [];
          this.focarPesquisa();
          return;
        }

        this.tocarSom(false);
        this.mostrarMensagem(
          'erro',
          resposta.motivoRecusa
            ? `Pagamento recusado: ${resposta.motivoRecusa}`
            : 'Pagamento recusado.',
        );
      },
      error: (erro) => {
        this.enviando = false;

        const texto =
          typeof erro?.error === 'string'
            ? erro.error
            : 'Não foi possível processar o pagamento. Tente novamente.';

        this.mostrarMensagem('erro', texto);
        this.tocarSom(false);
      },
    });
  }

  // -------------------------------------------------------------------------

  private limparBusca(): void {
    this.textoPesquisa = '';
    this.resultadosPesquisa = [];
    this.quantidade = 1;
    this.produtoSelecionadoId = 0;
    this.focarPesquisa();
  }

  private focarPesquisa(): void {
    this.pesquisaInput?.nativeElement.focus();
  }

  private destacarItem(produtoId: number): void {
    this.itemDestacadoId = produtoId;

    clearTimeout(this.timeoutDestaque);

    this.timeoutDestaque = setTimeout(() => {
      this.itemDestacadoId = null;
    }, 700);
  }

  private mostrarMensagem(tipo: 'sucesso' | 'erro', texto: string): void {
    this.mensagem = { tipo, texto };

    clearTimeout(this.timeoutMensagem);

    this.timeoutMensagem = setTimeout(() => {
      this.mensagem = null;
    }, 4000);
  }

  // Bipe curto (sucesso: agudo / erro: grave) para o operador confirmar a
  // leitura do código de barras sem precisar olhar para a tela.
  private tocarSom(sucesso: boolean): void {
    try {
      const AudioContextRef = window.AudioContext ?? (window as any).webkitAudioContext;
      const contexto = new AudioContextRef();
      const oscilador = contexto.createOscillator();
      const ganho = contexto.createGain();

      oscilador.type = 'sine';
      oscilador.frequency.value = sucesso ? 880 : 220;
      ganho.gain.setValueAtTime(0.12, contexto.currentTime);

      oscilador.connect(ganho);
      ganho.connect(contexto.destination);

      const duracao = sucesso ? 0.08 : 0.2;

      oscilador.start();
      oscilador.stop(contexto.currentTime + duracao);
      oscilador.onended = () => contexto.close();
    } catch {
      // Web Audio indisponível no navegador — segue sem som.
    }
  }
}
