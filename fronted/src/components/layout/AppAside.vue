<template>
  <aside class="w-64 border-r border-base-200 bg-base-100 h-[calc(100vh-64px)] overflow-y-auto">
    <div class="p-4">
      <button 
        @click="handleNewConversation" 
        class="btn btn-primary w-full mb-4"
      >
        新对话
      </button>
      
      <div class="space-y-2">
        <h2 class="text-xs font-semibold uppercase text-muted-foreground px-2">对话历史</h2>
        <div 
          v-for="conversation in chatStore.conversations" 
          :key="conversation.id"
          @click="handleSelectConversation(conversation.id)"
          class="cursor-pointer p-2 rounded-lg hover:bg-base-200 transition-colors"
          :class="{ 'bg-base-200': chatStore.currentConversationId === conversation.id }"
        >
          <div class="text-sm font-medium truncate">{{ conversation.title }}</div>
          <div class="text-xs text-muted-foreground">
            {{ formatDate(conversation.updatedAt) }} • {{ conversation.messageCount }} 条消息
          </div>
        </div>
        <div v-if="chatStore.conversations.length === 0" class="text-center text-sm text-muted-foreground py-4">
          暂无对话历史
        </div>
      </div>
    </div>
    
    <div class="border-t border-base-200 p-4 mt-4">
      <nav class="space-y-1">
        <router-link 
          to="/history" 
          class="flex items-center space-x-2 p-2 rounded-lg hover:bg-base-200 transition-colors"
          active-class="bg-base-200 font-medium"
        >
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 8v4l3 3"/><circle cx="12" cy="12" r="10"/></svg>
          <span>历史记录</span>
        </router-link>
        <router-link 
          to="/profile" 
          class="flex items-center space-x-2 p-2 rounded-lg hover:bg-base-200 transition-colors"
          active-class="bg-base-200 font-medium"
        >
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
          <span>个人中心</span>
        </router-link>
        <router-link 
          to="/settings" 
          class="flex items-center space-x-2 p-2 rounded-lg hover:bg-base-200 transition-colors"
          active-class="bg-base-200 font-medium"
        >
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
          <span>设置</span>
        </router-link>
      </nav>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { useChatStore } from '../../stores/chat';
import { useRouter } from 'vue-router';

const chatStore = useChatStore();
const router = useRouter();

const handleNewConversation = async () => {
  await chatStore.createConversation();
  router.push('/chat');
};

const handleSelectConversation = async (conversationId: string) => {
  await chatStore.fetchMessages(conversationId);
  router.push('/chat');
};

const formatDate = (dateString: string) => {
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', { 
    month: 'short', 
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};
</script>
