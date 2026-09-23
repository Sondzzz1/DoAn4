import * as signalR from '@microsoft/signalr';
import api from './api';
import { ApiResponse } from '../types/common.types';
import { STORAGE_KEYS } from '../utils/constants';

export interface NotificationItem {
  id: number;
  accountId: number;
  title: string;
  content: string;
  type?: number;
  link?: string;
  isRead: boolean;
  createdAt: string;
}

class NotificationService {
  private hubConnection: signalR.HubConnection | null = null;
  private listeners: ((notification: NotificationItem) => void)[] = [];

  /**
   * Khởi tạo kết nối SignalR Hub cho thông báo thời gian thực
   */
  public async startConnection(): Promise<void> {
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN);
    if (!token) return;

    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/notifications', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: NotificationItem) => {
      this.listeners.forEach((listener) => listener(notification));
    });

    try {
      await this.hubConnection.start();
    } catch (err) {
      console.warn('Không thể kết nối Notification SignalR Hub:', err);
    }
  }

  public stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
    }
  }

  public onReceiveNotification(callback: (notification: NotificationItem) => void): () => void {
    this.listeners.push(callback);
    return () => {
      this.listeners = this.listeners.filter((l) => l !== callback);
    };
  }

  // REST APIs
  public async getMyNotifications(): Promise<ApiResponse<NotificationItem[]>> {
    const response = await api.get<ApiResponse<NotificationItem[]>>('/thong-bao');
    return response.data;
  }

  public async getUnreadCount(): Promise<ApiResponse<number>> {
    const response = await api.get<ApiResponse<number>>('/thong-bao/chua-doc');
    return response.data;
  }

  public async markAsRead(id: number): Promise<ApiResponse<boolean>> {
    const response = await api.put<ApiResponse<boolean>>(`/thong-bao/${id}/da-doc`);
    return response.data;
  }

  public async markAllAsRead(): Promise<ApiResponse<boolean>> {
    const response = await api.put<ApiResponse<boolean>>('/thong-bao/doc-tat-ca');
    return response.data;
  }
}

export const notificationService = new NotificationService();
