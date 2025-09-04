import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';

export interface Notification {
  id: number;
  type: 'membership_expiry' | 'payment_due' | 'birthday' | 'system' | 'custom';
  title: string;
  message: string;
  priority: 'low' | 'medium' | 'high' | 'urgent';
  isRead: boolean;
  customerId?: number;
  scheduledFor?: Date;
  createdAt: Date;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationApiService extends BaseApiService {

  // Get notifications
  getNotifications(unreadOnly: boolean = false): Observable<Notification[]> {
    return this.get<Notification[]>('notifications', { unreadOnly });
  }

  // Mark notification as read
  markAsRead(id: number): Observable<void> {
    return this.post<void>(`notifications/${id}/read`, {});
  }

  // Mark all as read
  markAllAsRead(): Observable<void> {
    return this.post<void>('notifications/read-all', {});
  }

  // Create custom notification
  createNotification(notification: Partial<Notification>): Observable<Notification> {
    return this.post<Notification>('notifications', notification);
  }

  // Delete notification
  deleteNotification(id: number): Observable<void> {
    return this.delete<void>(`notifications/${id}`);
  }

  // Get notification settings
  getNotificationSettings(): Observable<any> {
    return this.get<any>('notifications/settings');
  }

  // Update notification settings
  updateNotificationSettings(settings: any): Observable<any> {
    return this.put<any>('notifications/settings', settings);
  }
}
