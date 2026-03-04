import { useChatStore } from '../stores/chat';

export const useChat = () => {
  const chatStore = useChatStore();

  const sendMessage = async (content: string) => {
    await chatStore.sendMessage(content);
  };

  const createConversation = async () => {
    await chatStore.createConversation();
  };

  const selectConversation = async (conversationId: string) => {
    await chatStore.fetchMessages(conversationId);
  };

  const fetchConversations = async () => {
    await chatStore.fetchConversations();
  };

  const deleteConversation = async (conversationId: string) => {
    await chatStore.deleteConversation(conversationId);
  };

  return {
    sendMessage,
    createConversation,
    selectConversation,
    fetchConversations,
    deleteConversation,
    conversations: chatStore.conversations,
    currentConversationId: chatStore.currentConversationId,
    messages: chatStore.messages,
    isLoading: chatStore.isLoading
  };
};
