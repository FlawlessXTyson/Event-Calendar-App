import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { WalletDto, WalletTransactionDto, AddMoneyRequest, PaymentResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class WalletService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/Wallet`;

  getWallet()          { return this.http.get<WalletDto>(`${this.base}`); }
  addMoney(req: AddMoneyRequest) { return this.http.post<WalletDto>(`${this.base}/add`, req); }
  getTransactions()    { return this.http.get<WalletTransactionDto[]>(`${this.base}/transactions`); }
  payWithWallet(req: { eventId: number }) { return this.http.post<PaymentResponse>(`${this.base}/pay`, req); }
}
