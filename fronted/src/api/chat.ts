import axiosInstance from './index';

export interface Message {
  id: string;
  content: string;
  role: 'user' | 'assistant';
  createdAt: string;
}

export interface Conversation {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messageCount: number;
}

export interface SendMessageRequest {
  conversationId: string;
  content: string;
}

export interface SendMessageResponse {
  id: string;
  content: string;
  role: 'assistant';
  createdAt: string;
}

export const chatApi = {
  getConversations: async (): Promise<Conversation[]> => {
    const response = await axiosInstance.get('/chat/conversations');
    return response.data;
  },
  createConversation: async (): Promise<Conversation> => {
    const response = await axiosInstance.post('/chat/conversations');
    return response.data;
  },
  getMessages: async (conversationId: string): Promise<Message[]> => {
    const response = await axiosInstance.get(`/chat/conversations/${conversationId}/messages`);
    return response.data;
  },
  sendMessage: async (data: SendMessageRequest): Promise<SendMessageResponse> => {
    const response = await axiosInstance.post('/chat/messages', data);
    return response.data;
  },
  deleteConversation: async (conversationId: string): Promise<void> => {
    await axiosInstance.delete(`/chat/conversations/${conversationId}`);
  }
};
