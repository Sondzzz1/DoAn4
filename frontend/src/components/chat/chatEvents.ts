export interface DirectChatPayload {
  partnerId: number;
  partnerName: string;
  postId?: number;
  postTitle?: string;
  postPrice?: number;
  postImage?: string;
}

export const openDirectChat = (payload: DirectChatPayload) => {
  window.dispatchEvent(new CustomEvent<DirectChatPayload>('open-direct-chat', { detail: payload }));
};
