import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { QueryParams } from '../models/common.model';

export interface Transaction {
  id: number;
  customerId: number;
  customerName: string;
  type: 'membership' | 'personal_training' | 'product' | 'penalty' | 'refund';
  amount: number;
  paymentMethod: 'cash' | 'card' | 'bank_transfer' | 'mobile_payment';
  status: 'completed' | 'pending' | 'failed' | 'cancelled';
  description: string;
  notes?: string;
  transactionDate: Date;
  createdBy: number;
  createdAt: Date;
}

export interface FinanceStats {
  todayRevenue: number;
  monthRevenue: number;
  yearRevenue: number;
  pendingPayments: number;
  overduePayments: number;
  totalOutstanding: number;
  averageTransactionAmount: number;
  topPaymentMethod: string;
}

export interface RevenueChart {
  period: string;
  revenue: number;
  transactions: number;
}

@Injectable({
  providedIn: 'root'
})
export class FinanceApiService extends BaseApiService {

  // Get all transactions
  getTransactions(params?: QueryParams): Observable<Transaction[]> {
    return this.get<Transaction[]>('finance/transactions', params);
  }

  // Get transaction by ID
  getTransactionById(id: number): Observable<Transaction> {
    return this.get<Transaction>(`finance/transactions/${id}`);
  }

  // Create new transaction
  createTransaction(transaction: Partial<Transaction>): Observable<Transaction> {
    return this.post<Transaction>('finance/transactions', transaction);
  }

  // Update transaction
  updateTransaction(id: number, transaction: Partial<Transaction>): Observable<Transaction> {
    return this.put<Transaction>(`finance/transactions/${id}`, transaction);
  }

  // Cancel transaction
  cancelTransaction(id: number, reason: string): Observable<Transaction> {
    return this.post<Transaction>(`finance/transactions/${id}/cancel`, { reason });
  }

  // Get customer transactions
  getCustomerTransactions(customerId: number, params?: QueryParams): Observable<Transaction[]> {
    return this.get<Transaction[]>(`finance/customers/${customerId}/transactions`, params);
  }

  // Get finance statistics
  getFinanceStats(dateFrom?: Date, dateTo?: Date): Observable<FinanceStats> {
    return this.get<FinanceStats>('finance/stats', { dateFrom, dateTo });
  }

  // Get revenue chart data
  getRevenueChart(period: 'daily' | 'weekly' | 'monthly', dateFrom?: Date, dateTo?: Date): Observable<RevenueChart[]> {
    return this.get<RevenueChart[]>('finance/revenue-chart', { period, dateFrom, dateTo });
  }

  // Get pending payments
  getPendingPayments(): Observable<Transaction[]> {
    return this.get<Transaction[]>('finance/pending-payments');
  }

  // Get overdue payments
  getOverduePayments(): Observable<Transaction[]> {
    return this.get<Transaction[]>('finance/overdue-payments');
  }

  // Process payment
  processPayment(customerId: number, amount: number, paymentMethod: string, description: string): Observable<Transaction> {
    return this.post<Transaction>('finance/process-payment', {
      customerId,
      amount,
      paymentMethod,
      description
    });
  }

  // Generate invoice
  generateInvoice(customerId: number, items: any[]): Observable<any> {
    return this.post<any>('finance/generate-invoice', { customerId, items });
  }

  // Export financial report
  exportFinanceReport(params: QueryParams): Observable<any> {
    return this.get<any>('finance/export-report', params);
  }
}
