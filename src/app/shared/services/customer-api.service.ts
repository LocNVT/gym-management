import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { Customer } from '../../modules/customers/models/customer.model';
import { QueryParams } from '../models/common.model';

export interface CustomerListResponse {
  customers: Customer[];
  total: number;
}

export interface CustomerStats {
  totalCustomers: number;
  activeCustomers: number;
  inactiveCustomers: number;
  expiredCustomers: number;
  newCustomersThisMonth: number;
  totalRevenue: number;
}

@Injectable({
  providedIn: 'root'
})
export class CustomerApiService extends BaseApiService {

  // Get all customers with pagination and filtering
  getCustomers(params?: QueryParams): Observable<CustomerListResponse> {
    return this.get<CustomerListResponse>('customers', params);
  }

  // Get customer by ID
  getCustomerById(id: number): Observable<Customer> {
    return this.get<Customer>(`customers/${id}`);
  }

  // Create new customer
  createCustomer(customer: Partial<Customer>): Observable<Customer> {
    return this.post<Customer>('customers', customer);
  }

  // Update customer
  updateCustomer(id: number, customer: Partial<Customer>): Observable<Customer> {
    return this.put<Customer>(`customers/${id}`, customer);
  }

  // Delete customer
  deleteCustomer(id: number): Observable<void> {
    return this.delete<void>(`customers/${id}`);
  }

  // Search customers by phone or email
  searchCustomers(query: string): Observable<Customer[]> {
    return this.get<Customer[]>('customers/search', { search: query });
  }

  // Get customer statistics
  getCustomerStats(): Observable<CustomerStats> {
    return this.get<CustomerStats>('customers/stats');
  }

  // Get customers with expiring memberships
  getExpiringMemberships(days: number = 30): Observable<Customer[]> {
    return this.get<Customer[]>('customers/expiring', { days });
  }

  // Suspend customer membership
  suspendMembership(customerId: number, reason: string): Observable<Customer> {
    return this.post<Customer>(`customers/${customerId}/suspend`, { reason });
  }

  // Reactivate customer membership
  reactivateMembership(customerId: number): Observable<Customer> {
    return this.post<Customer>(`customers/${customerId}/reactivate`, {});
  }

  // Get customer attendance history
  getCustomerAttendance(customerId: number, params?: QueryParams): Observable<any[]> {
    return this.get<any[]>(`customers/${customerId}/attendance`, params);
  }

  // Get customer payment history
  getCustomerPayments(customerId: number, params?: QueryParams): Observable<any[]> {
    return this.get<any[]>(`customers/${customerId}/payments`, params);
  }

  // Bulk operations
  bulkUpdateCustomers(customerIds: number[], updates: Partial<Customer>): Observable<void> {
    return this.post<void>('customers/bulk-update', { ids: customerIds, updates });
  }

  bulkDeleteCustomers(customerIds: number[]): Observable<void> {
    return this.post<void>('customers/bulk-delete', { ids: customerIds });
  }
}
