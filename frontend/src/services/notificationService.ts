import * as signalR from '@microsoft/signalr';
import api from './api';
import { ApiResponse } from '../types/common.types';
import { SIGNALR_BASE_URL, STORAGE_KEYS } from '../utils/constants';

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
  private connectionPromise: Promise<void> | null = null;
  private shouldStayConnected = false;
  private listeners: ((notification: NotificationItem) => void)[] = [];

  /**
   * Khởi tạo kết nối SignalR Hub cho thông báo thời gian thực
   */
  public async startConnection(): Promise<void> {
    if (!localStorage.getItem(STORAGE_KEYS.TOKEN)) return;
    this.shouldStayConnected = true;

    if (this.hubConnection) {
      if (this.hubConnection.state === signalR.HubConnectionState.Connected) return;
      if (this.hubConnection.state === signalR.HubConnectionState.Connecting ||
          this.hubConnection.state === signalR.HubConnectionState.Reconnecting) {
        return this.connectionPromise ?? Promise.resolve();
      }
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${SIGNALR_BASE_URL}/hubs/notifications`, {
        accessTokenFactory: () => localStorage.getItem(STORAGE_KEYS.TOKEN) || '',
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: NotificationItem) => {
      this.listeners.forEach((listener) => listener(notification));
    });

    const connection = this.hubConnection;
    this.connectionPromise = connection.start()
      .catch(() => {
        if (this.hubConnection === connection) this.hubConnection = null;
      })
      .finally(() => {
        this.connectionPromise = null;
      });
    await this.connectionPromise;
  }

  public async stopConnection(): Promise<void> {
    this.shouldStayConnected = false;
    const connection = this.hubConnection;
    const pendingStart = this.connectionPromise;
    if (!connection) return;

    if (pendingStart) {
      await pendingStart;
    }

    if (this.shouldStayConnected) return;

    if (connection.state !== signalR.HubConnectionState.Disconnected) {
      await connection.stop();
    }

    if (this.hubConnection === connection) {
      this.hubConnection = null;
      this.connectionPromise = null;
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
