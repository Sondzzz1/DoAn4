import * as signalR from '@microsoft/signalr';
import api from './api';
import { ApiResponse } from '../types/common.types';
import { STORAGE_KEYS } from '../utils/constants';

export interface ChatMessage {
  id: number;
  senderId: number;
  senderName: string;
  senderAvatar?: string;
  receiverId: number;
  receiverName: string;
  receiverAvatar?: string;
  postId?: number;
  postTitle?: string;
  postPrice?: number;
  postImage?: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface Conversation {
  partnerId: number;
  partnerName: string;
  partnerAvatar?: string;
  partnerRole: string;
  lastMessage: string;
  lastMessageTime: string;
  unreadCount: number;
  postId?: number;
  postTitle?: string;
}

export interface SendMessagePayload {
  receiverId: number;
  postId?: number;
  message: string;
}

class ChatService {
  private hubConnection: signalR.HubConnection | null = null;
  private connectionPromise: Promise<void> | null = null;
  private shouldStayConnected = false;
  private messageListeners: ((msg: ChatMessage) => void)[] = [];

  public async startConnection(): Promise<void> {
    const token = localStorage.getItem(STORAGE_KEYS.TOKEN);
    if (!token) return;
    this.shouldStayConnected = true;

    if (this.hubConnection) {
      if (this.hubConnection.state === signalR.HubConnectionState.Connected) return;
      if (this.hubConnection.state === signalR.HubConnectionState.Connecting ||
          this.hubConnection.state === signalR.HubConnectionState.Reconnecting) {
        return this.connectionPromise ?? Promise.resolve();
      }
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/chat', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveMessage', (msg: ChatMessage) => {
      this.messageListeners.forEach((listener) => listener(msg));
    });

    this.hubConnection.on('MessageSent', (msg: ChatMessage) => {
      this.messageListeners.forEach((listener) => listener(msg));
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

  public onReceiveMessage(callback: (msg: ChatMessage) => void): () => void {
    this.messageListeners.push(callback);
    return () => {
      this.messageListeners = this.messageListeners.filter((l) => l !== callback);
    };
  }

  // REST APIs
  public async getConversations(): Promise<ApiResponse<Conversation[]>> {
    const response = await api.get<ApiResponse<Conversation[]>>('/tin-nhan/hoi-thoai');
    return response.data;
  }

  public async getMessages(partnerId: number): Promise<ApiResponse<ChatMessage[]>> {
    const response = await api.get<ApiResponse<ChatMessage[]>>(`/tin-nhan/hoi-thoai/${partnerId}`);
    return response.data;
  }

  public async sendMessage(data: SendMessagePayload): Promise<ApiResponse<ChatMessage>> {
    const response = await api.post<ApiResponse<ChatMessage>>('/tin-nhan/gui', data);
    return response.data;
  }

  public async markAsRead(partnerId: number): Promise<ApiResponse<boolean>> {
    const response = await api.put<ApiResponse<boolean>>(`/tin-nhan/doc/${partnerId}`);
    return response.data;
  }

  public async getUnreadCount(): Promise<ApiResponse<number>> {
    const response = await api.get<ApiResponse<number>>('/tin-nhan/chua-doc');
    return response.data;
  }
}

export const chatService = new ChatService();
