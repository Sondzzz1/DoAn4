import React, { useEffect, useState, useRef } from 'react';
import { FiMessageSquare, FiX, FiSend, FiUser, FiArrowLeft, FiCircle } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { chatService, ChatMessage, Conversation } from '../../services/chatService';
import { useAuth } from '../../hooks/useAuth';
import { formatPrice } from '../../utils/helpers';

interface ChatDrawerProps {
  directChatPartner?: {
    partnerId: number;
    partnerName: string;
    postId?: number;
    postTitle?: string;
    postPrice?: number;
    postImage?: string;
  } | null;
  onCloseDirectChat?: () => void;
}

const ChatDrawer: React.FC<ChatDrawerProps> = ({
  directChatPartner,
  onCloseDirectChat,
}) => {
  const { isAuthenticated, user } = useAuth();

  const [isOpen, setIsOpen] = useState(false);
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [activePartner, setActivePartner] = useState<{ id: number; name: string; avatar?: string } | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputMessage, setInputMessage] = useState('');
  const [totalUnread, setTotalUnread] = useState(0);
  const [loading, setLoading] = useState(false);

  // Active Post context
  const [activePost, setActivePost] = useState<{ id?: number; title?: string; price?: number; image?: string } | null>(null);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  // Khởi tạo kết nối SignalR khi đã đăng nhập
  useEffect(() => {
    if (!isAuthenticated) return;

    void chatService.startConnection();

    // Lắng nghe tin nhắn mới từ SignalR
    const unsubscribe = chatService.onReceiveMessage((msg) => {
      // Nếu đang mở khung chat với người gửi đó
      if (activePartner && (msg.senderId === activePartner.id || msg.receiverId === activePartner.id)) {
        setMessages((prev) => [...prev, msg]);
        scrollToBottom();
        // Đánh dấu đã đọc
        if (msg.senderId === activePartner.id) {
          void chatService.markAsRead(activePartner.id);
        }
      } else {
        // Cập nhật số tin chưa đọc
        setTotalUnread((prev) => prev + 1);
        toast.info(`Tin nhắn mới từ ${msg.senderName}: "${msg.message.slice(0, 30)}..."`);
      }
      void loadConversations();
    });

    void loadConversations();

    return () => {
      unsubscribe();
    };
  }, [isAuthenticated, activePartner]);

  // Xử lý khi có directChatPartner từ RoomDetailPage
  useEffect(() => {
    if (directChatPartner) {
      setIsOpen(true);
      setActivePartner({
        id: directChatPartner.partnerId,
        name: directChatPartner.partnerName,
      });
      if (directChatPartner.postId) {
        setActivePost({
          id: directChatPartner.postId,
          title: directChatPartner.postTitle,
          price: directChatPartner.postPrice,
          image: directChatPartner.postImage,
        });
      }
      void loadMessages(directChatPartner.partnerId);
    }
  }, [directChatPartner]);

  const loadConversations = async () => {
    try {
      const res = await chatService.getConversations();
      setConversations(res.data || []);
      const countRes = await chatService.getUnreadCount();
      setTotalUnread(countRes.data || 0);
    } catch {
      // Ignore if offline
    }
  };

  const loadMessages = async (partnerId: number) => {
    setLoading(true);
    try {
      const res = await chatService.getMessages(partnerId);
      setMessages(res.data || []);
      scrollToBottom();
      void chatService.markAsRead(partnerId);
      void loadConversations();
    } catch (err) {
      toast.error('Không thể tải lịch sử tin nhắn.');
    } finally {
      setLoading(false);
    }
  };

  const handleSelectConversation = (conv: Conversation) => {
    setActivePartner({
      id: conv.partnerId,
      name: conv.partnerName,
      avatar: conv.partnerAvatar,
    });
    if (conv.postId) {
      setActivePost({
        id: conv.postId,
        title: conv.postTitle,
      });
    } else {
      setActivePost(null);
    }
    void loadMessages(conv.partnerId);
  };

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activePartner || !inputMessage.trim()) return;

    const text = inputMessage.trim();
    setInputMessage('');

    try {
      const res = await chatService.sendMessage({
        receiverId: activePartner.id,
        message: text,
        postId: activePost?.id,
      });

      if (res.data) {
        setMessages((prev) => [...prev, res.data]);
        scrollToBottom();
        void loadConversations();
      }
    } catch (err: any) {
      toast.error(err?.response?.data?.message || 'Không thể gửi tin nhắn.');
    }
  };

  if (!isAuthenticated) return null;

  return (
    <>
      {/* Floating Chat Trigger Button */}
      {!isOpen && (
        <button
          onClick={() => setIsOpen(true)}
          className="fixed bottom-6 right-6 z-40 w-14 h-14 rounded-full bg-gradient-to-tr from-[#0084ff] to-cyan-500 text-white flex items-center justify-center shadow-xl hover:scale-110 transition-transform cursor-pointer border-none"
          title="Mở tin nhắn trực tiếp"
        >
          <FiMessageSquare size={24} />
          {totalUnread > 0 && (
            <span className="absolute -top-1 -right-1 w-6 h-6 rounded-full bg-rose-500 text-white text-xs font-black flex items-center justify-center border-2 border-white animate-bounce">
              {totalUnread}
            </span>
          )}
        </button>
      )}

      {/* Chat Window / Drawer */}
      {isOpen && (
        <div className="fixed bottom-6 right-6 z-50 w-96 max-w-[calc(100vw-32px)] h-[540px] bg-white rounded-3xl shadow-2xl border border-slate-200 flex flex-col overflow-hidden animate-in fade-in slide-in-from-bottom-8">
          {/* Header */}
          <div className="bg-gradient-to-r from-[#0084ff] to-blue-600 text-white p-4 flex items-center justify-between">
            {activePartner ? (
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  onClick={() => {
                    setActivePartner(null);
                    onCloseDirectChat?.();
                  }}
                  className="w-8 h-8 rounded-full bg-white/10 hover:bg-white/20 text-white flex items-center justify-center border-none cursor-pointer transition-colors"
                >
                  <FiArrowLeft size={16} />
                </button>
                <div>
                  <h4 className="font-bold text-sm line-clamp-1">{activePartner.name}</h4>
                  <span className="text-[11px] text-blue-100 flex items-center gap-1">
                    <FiCircle size={8} className="fill-emerald-400 text-emerald-400" /> Trực tuyến
                  </span>
                </div>
              </div>
            ) : (
              <div className="flex items-center gap-2">
                <FiMessageSquare size={20} />
                <h4 className="font-bold text-base">Tin nhắn ({conversations.length})</h4>
              </div>
            )}

            <button
              onClick={() => {
                setIsOpen(false);
                onCloseDirectChat?.();
              }}
              className="w-8 h-8 rounded-full bg-white/10 hover:bg-white/20 text-white flex items-center justify-center border-none cursor-pointer transition-colors"
            >
              <FiX size={18} />
            </button>
          </div>

          {/* Body */}
          {activePartner ? (
            /* ACTIVE CHAT VIEW */
            <div className="flex-1 flex flex-col bg-slate-50 overflow-hidden">
              {/* Post Context Banner if available */}
              {activePost && activePost.title && (
                <div className="bg-white border-b border-slate-200 p-2.5 px-3 flex items-center gap-3">
                  {activePost.image && (
                    <img src={activePost.image} alt="" className="w-10 h-10 rounded-xl object-cover" />
                  )}
                  <div className="flex-1 min-w-0">
                    <p className="text-xs font-bold text-slate-800 line-clamp-1">{activePost.title}</p>
                    {activePost.price && (
                      <span className="text-[11px] font-bold text-[#0084ff]">{formatPrice(activePost.price)}/tháng</span>
                    )}
                  </div>
                </div>
              )}

              {/* Messages Scroll Area */}
              <div className="flex-1 p-4 overflow-y-auto space-y-3">
                {loading ? (
                  <div className="text-center py-8 text-xs text-slate-400 animate-pulse">Đang tải tin nhắn...</div>
                ) : messages.length === 0 ? (
                  <div className="text-center py-12 text-slate-400 text-xs">
                    Hãy gửi lời chào đến <b>{activePartner.name}</b> để bắt đầu trò chuyện!
                  </div>
                ) : (
                  messages.map((m) => {
                    const isMe = m.senderId === user?.id;
                    return (
                      <div key={m.id} className={`flex flex-col ${isMe ? 'items-end' : 'items-start'}`}>
                        <div
                          className={`max-w-[80%] px-3.5 py-2.5 rounded-2xl text-xs leading-relaxed shadow-sm ${
                            isMe
                              ? 'bg-[#0084ff] text-white rounded-br-none'
                              : 'bg-white text-slate-800 border border-slate-200/80 rounded-bl-none'
                          }`}
                        >
                          {m.message}
                        </div>
                        <span className="text-[9px] text-slate-400 mt-1 px-1">
                          {new Date(m.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </span>
                      </div>
                    );
                  })
                )}
                <div ref={messagesEndRef} />
              </div>

              {/* Input Form */}
              <form onSubmit={handleSendMessage} className="p-3 bg-white border-t border-slate-200 flex items-center gap-2">
                <input
                  type="text"
                  placeholder="Nhập tin nhắn..."
                  value={inputMessage}
                  onChange={(e) => setInputMessage(e.target.value)}
                  className="flex-1 px-4 py-2.5 rounded-2xl bg-slate-100 border border-transparent focus:bg-white focus:border-[#0084ff] text-xs focus:outline-none"
                />
                <button
                  type="submit"
                  disabled={!inputMessage.trim()}
                  className="w-9 h-9 rounded-2xl bg-[#0084ff] hover:bg-[#0073e6] disabled:opacity-50 text-white flex items-center justify-center transition-all cursor-pointer border-none shadow-md"
                >
                  <FiSend size={14} />
                </button>
              </form>
            </div>
          ) : (
            /* CONVERSATION LIST VIEW */
            <div className="flex-1 overflow-y-auto divide-y divide-slate-100">
              {conversations.length === 0 ? (
                <div className="p-10 text-center text-slate-400 text-xs">
                  <FiMessageSquare className="mx-auto text-3xl mb-2 text-slate-300" />
                  Bạn chưa có cuộc trò chuyện nào.
                </div>
              ) : (
                conversations.map((conv) => (
                  <div
                    key={conv.partnerId}
                    onClick={() => handleSelectConversation(conv)}
                    className="p-3.5 px-4 flex items-center gap-3 hover:bg-blue-50/50 cursor-pointer transition-colors"
                  >
                    <div className="relative">
                      <div className="w-11 h-11 rounded-2xl bg-gradient-to-tr from-blue-500 to-cyan-400 text-white flex items-center justify-center font-bold text-sm">
                        {conv.partnerName.charAt(0).toUpperCase()}
                      </div>
                      {conv.unreadCount > 0 && (
                        <span className="absolute -top-1 -right-1 w-5 h-5 rounded-full bg-rose-500 text-white text-[10px] font-black flex items-center justify-center border-2 border-white">
                          {conv.unreadCount}
                        </span>
                      )}
                    </div>

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between mb-0.5">
                        <h4 className="font-bold text-xs text-slate-900 line-clamp-1">{conv.partnerName}</h4>
                        <span className="text-[10px] text-slate-400">
                          {new Date(conv.lastMessageTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </span>
                      </div>
                      <p className="text-xs text-slate-500 line-clamp-1">{conv.lastMessage}</p>
                    </div>
                  </div>
                ))
              )}
            </div>
          )}
        </div>
      )}
    </>
  );
};

export default ChatDrawer;
