import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root'
})
export class RealtimeService {
  private hubConnection?: signalR.HubConnection;

  iniciarConexao(): void {
    this.hubConnection =
      new signalR.HubConnectionBuilder()
      .withUrl(
        'https://localhost:7256/notificacoes'
      )
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log(
          'SignalR conectado'
        );
      })
      .catch(error => {
        console.error(error);
      });
  }

  escutarVendaCriada(
    callback: () => void
  ): void {
    this.hubConnection?.on(
      'VendaCriada',
      () => {
        callback();
      });
  }
}