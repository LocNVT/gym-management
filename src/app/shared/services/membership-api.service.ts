import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { QueryParams } from '../models/common.model';

export interface MembershipPlan {
  id: number;
  name: string;
  type: 'basic' | 'premium' | 'vip';
  duration: number; // months
  price: number;
  features: string[];
  description: string;
  isActive: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface MembershipSubscription {
  id: number;
  customerId: number;
  planId: number;
  startDate: Date;
  endDate: Date;
  status: 'active' | 'inactive' | 'expired' | 'suspended';
  paymentStatus: 'paid' | 'pending' | 'overdue';
  autoRenewal: boolean;
  price: number;
  discount?: number;
  createdAt: Date;
  updatedAt: Date;
}

@Injectable({
  providedIn: 'root'
})
export class MembershipApiService extends BaseApiService {

  // Membership Plans
  getPlans(params?: QueryParams): Observable<MembershipPlan[]> {
    return this.get<MembershipPlan[]>('membership/plans', params);
  }

  getPlanById(id: number): Observable<MembershipPlan> {
    return this.get<MembershipPlan>(`membership/plans/${id}`);
  }

  createPlan(plan: Partial<MembershipPlan>): Observable<MembershipPlan> {
    return this.post<MembershipPlan>('membership/plans', plan);
  }

  updatePlan(id: number, plan: Partial<MembershipPlan>): Observable<MembershipPlan> {
    return this.put<MembershipPlan>(`membership/plans/${id}`, plan);
  }

  deletePlan(id: number): Observable<void> {
    return this.delete<void>(`membership/plans/${id}`);
  }

  // Membership Subscriptions
  getSubscriptions(params?: QueryParams): Observable<MembershipSubscription[]> {
    return this.get<MembershipSubscription[]>('membership/subscriptions', params);
  }

  getSubscriptionById(id: number): Observable<MembershipSubscription> {
    return this.get<MembershipSubscription>(`membership/subscriptions/${id}`);
  }

  createSubscription(subscription: Partial<MembershipSubscription>): Observable<MembershipSubscription> {
    return this.post<MembershipSubscription>('membership/subscriptions', subscription);
  }

  updateSubscription(id: number, subscription: Partial<MembershipSubscription>): Observable<MembershipSubscription> {
    return this.put<MembershipSubscription>(`membership/subscriptions/${id}`, subscription);
  }

  renewSubscription(id: number, planId: number): Observable<MembershipSubscription> {
    return this.post<MembershipSubscription>(`membership/subscriptions/${id}/renew`, { planId });
  }

  cancelSubscription(id: number, reason?: string): Observable<MembershipSubscription> {
    return this.post<MembershipSubscription>(`membership/subscriptions/${id}/cancel`, { reason });
  }

  // Get subscription statistics
  getSubscriptionStats(): Observable<any> {
    return this.get<any>('membership/stats');
  }
}
