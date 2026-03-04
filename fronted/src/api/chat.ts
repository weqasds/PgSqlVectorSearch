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

// 模拟对话数据
const generateMockConversations = (): Conversation[] => {
  const conversations: Conversation[] = [];
  const titles = ['项目讨论', '技术问题', '设计评审', '需求分析', 'Bug报告'];

  for (let i = 0; i < 5; i++) {
    const date = new Date(Date.now() - i * 24 * 60 * 60 * 1000);
    conversations.push({
      id: `conv-${i + 1}`,
      title: titles[i % titles.length] + (i > titles.length ? ` ${Math.floor(i / titles.length) + 1}` : ''),
      createdAt: date.toISOString(),
      updatedAt: date.toISOString(),
      messageCount: Math.floor(Math.random() * 20) + 1
    });
  }
  return conversations;
};

// 模拟消息数据
const generateMockMessages = (conversationId: string): Message[] => {
  const messages: Message[] = [];
  const userMessages = [
    '你好，我有一个关于项目的问题。',
    '这个功能什么时候能完成？',
    '需要帮忙测试新版本吗？',
    '文档在哪里可以找到？',
    '这个bug的复现步骤是什么？'
  ];
  const assistantMessages = [
    '你好！我很乐意帮助你解答项目相关的问题。',
    '这个功能预计在下周完成，目前正在测试阶段。',
    '是的，测试新版本会很有帮助，可以提前发现潜在问题。',
    '文档可以在项目的wiki页面找到，地址是...',
    '这个bug的复现步骤是：1. 打开应用 2. 点击设置 3. 选择高级选项'
  ];

  for (let i = 0; i < 3; i++) {
    const date = new Date(Date.now() - i * 60 * 60 * 1000);
    messages.push({
      id: `msg-user-${conversationId}-${i}`,
      content: userMessages[i % userMessages.length],
      role: 'user',
      createdAt: date.toISOString()
    });

    messages.push({
      id: `msg-assistant-${conversationId}-${i}`,
      content: assistantMessages[i % assistantMessages.length],
      role: 'assistant',
      createdAt: new Date(date.getTime() + 5 * 60 * 1000).toISOString()
    });
  }
  return messages;
};

// 检查是否使用模拟API
const useMockApi = import.meta.env.DEV && import.meta.env.VITE_USE_MOCK_API !== 'false';

export const chatApi = {
  getConversations: async (): Promise<Conversation[]> => {
    if (useMockApi) {
      await new Promise(resolve => setTimeout(resolve, 300));
      return generateMockConversations();
    }
    const response = await axiosInstance.get('/chat/conversations');
    return response.data;
  },
  createConversation: async (): Promise<Conversation> => {
    if (useMockApi) {
      await new Promise(resolve => setTimeout(resolve, 200));
      const date = new Date().toISOString();
      return {
        id: 'conv-new-' + Date.now(),
        title: '新对话',
        createdAt: date,
        updatedAt: date,
        messageCount: 0
      };
    }
    const response = await axiosInstance.post('/chat/conversations');
    return response.data;
  },
  getMessages: async (conversationId: string): Promise<Message[]> => {
    if (useMockApi) {
      await new Promise(resolve => setTimeout(resolve, 200));
      return generateMockMessages(conversationId);
    }
    const response = await axiosInstance.get(`/chat/conversations/${conversationId}/messages`);
    return response.data;
  },
  sendMessage: async (data: SendMessageRequest): Promise<SendMessageResponse> => {
    if (useMockApi) {
      await new Promise(resolve => setTimeout(resolve, 800));
      const responses = [
        '这是一个模拟响应。在实际应用中，这里会是AI的回复。',
        '我理解你的问题。让我思考一下如何最好地回答。',
        '根据我的分析，这个问题可以这样解决...',
        '谢谢你的提问！这是一个很好的问题。',
        '让我为你提供更多关于这个主题的信息。'
      ];
      const randomResponse = responses[Math.floor(Math.random() * responses.length)];
      return {
        id: 'msg-' + Date.now(),
        content: randomResponse,
        role: 'assistant',
        createdAt: new Date().toISOString()
      };
    }
    const response = await axiosInstance.post('/chat/messages', data);
    return response.data;
  },
  deleteConversation: async (conversationId: string): Promise<void> => {
    if (useMockApi) {
      await new Promise(resolve => setTimeout(resolve, 200));
      return;
    }
    await axiosInstance.delete(`/chat/conversations/${conversationId}`);
  }
};
