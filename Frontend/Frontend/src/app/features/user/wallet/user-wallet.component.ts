import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WalletService } from '../../../core/services/wallet.service';
import { ToastService } from '../../../core/services/toast.service';
import { WalletDto, WalletTransactionDto, AddMoneyRequest } from '../../../core/models/models';

type PayMethod = 'upi' | 'card' | 'netbanking' | 'wallet';
const PAY_METHODS = [
  { key: 'upi'        as PayMethod, label: 'UPI',           icon: '⚡', color: '#6366F1' },
  { key: 'netbanking' as PayMethod, label: 'Net Banking',    icon: '🏦', color: '#0EA5E9' },
  { key: 'card'       as PayMethod, label: 'Credit / Debit', icon: '💳', color: '#0D9488' },
];

@Component({
  selector: 'app-user-wallet',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './user-wallet.component.html',
  styleUrl: './user-wallet.component.css'
})
export class UserWalletComponent implements OnInit {
  private walletSvc = inject(WalletService);
  private toast     = inject(ToastService);

  wallet       = signal<WalletDto | null>(null);
  transactions = signal<WalletTransactionDto[]>([]);
  loading      = signal(true);
  txLoading    = signal(true);
  adding       = signal(false);
  showAddModal = signal(false);

  addAmount    = 500;
  selectedMethod: PayMethod = 'upi';
  payMethods   = PAY_METHODS;
  quickAmounts = [100, 500, 1000, 2000, 5000];

  ngOnInit() {
    this.walletSvc.getWallet().subscribe({
      next: w => { this.wallet.set(w); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
    this.walletSvc.getTransactions().subscribe({
      next: txs => { this.transactions.set(txs); this.txLoading.set(false); },
      error: () => this.txLoading.set(false)
    });
  }

  closeAddModal() { this.showAddModal.set(false); this.addAmount = 500; this.selectedMethod = 'upi'; }

  confirmAdd() {
    if (!this.addAmount || this.addAmount <= 0 || !this.selectedMethod) return;
    this.adding.set(true);
    const req: AddMoneyRequest = { amount: this.addAmount, paymentMethod: this.selectedMethod };
    this.walletSvc.addMoney(req).subscribe({
      next: w => {
        this.wallet.set(w);
        this.toast.success(`₹${this.addAmount} added to your wallet!`, 'Money Added');
        this.adding.set(false);
        this.closeAddModal();
        // Refresh transactions
        this.walletSvc.getTransactions().subscribe({ next: txs => this.transactions.set(txs), error: () => {} });
      },
      error: () => this.adding.set(false)
    });
  }
}
