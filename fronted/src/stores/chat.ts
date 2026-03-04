import { defineStore } from 'pinia';
import { chatApi, Message, Conversation } from '../api/chat';

export const useChatStore = defineStore('chat', {
  state: () => ({
    conversations: [] as Conversation[],
    currentConversationId: '',
    messages: [] as Message[],
    isLoading: false
  }),
  actions: {
    async fetchConversations() {
      this.isLoading = true;
      try {
        this.conversations = await chatApi.getConversations();
      } catch (error) {
        console.error('Fetch conversations error:', error);
      } finally {
        this.isLoading = false;
      }
    },
    async createConversation() {
      this.isLoading = true;
      try {
        const newConversation = await chatApi.createConversation();
        this.conversations.unshift(newConversation);
        this.currentConversationId = newConversation.id;
        this.messages = [];
      } catch (error) {
        console.error('Create conversation error:', error);
      } finally {
        this.isLoading = false;
      }
    },
    async fetchMessages(conversationId: string) {
      this.isLoading = true;
      try {
        this.messages = await chatApi.getMessages(conversationId);
        this.currentConversationId = conversationId;
      } catch (error) {
        console.error('Fetch messages error:', error);
      } finally {
        this.isLoading = false;
      }
    },
    async sendMessage(content: string) {
      if (!this.currentConversationId) {
        await this.createConversation();
      }
      
      const userMessage: Message = {
        id: Date.now().toString(),
        content,
        role: 'user',
        createdAt: new Date().toISOString()
      };
      
      this.messages.push(userMessage);
      
      try {
        const response = await chatApi.sendMessage({
          conversationId: this.currentConversationId,
          content
        });
        
        this.messages.push(response);
        await this.fetchConversations();
      } catch (error) {
        console.error('Send message error:', error);
      }
    },
    async deleteConversation(conversationId: string) {
      try {
        await chatApi.deleteConversation(conversationId);
        this.conversations = this.conversations.filter(c => c.id !== conversationId);
        if (this.currentConversationId === conversationId) {
          this.currentConversationId = '';
          this.messages = [];
        }
      } catch (error) {
        console.error('Delete conversation error:', error);
      }
    }
  }
});
