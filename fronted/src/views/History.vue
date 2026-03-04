<template>
  <div class="h-screen flex flex-col">
    <AppHeader />
    <div class="flex flex-1 overflow-hidden">
      <AppAside />
      <div class="flex-1 p-6 overflow-y-auto">
        <h1 class="text-2xl font-bold mb-6">历史记录</h1>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <div 
            v-for="conversation in conversations" 
            :key="conversation.id"
            @click="handleSelectConversation(conversation.id)"
            class="border border-base-200 rounded-lg p-4 hover:shadow-md transition-shadow cursor-pointer"
          >
            <h2 class="font-medium mb-2">{{ conversation.title }}</h2>
            <div class="text-sm text-muted-foreground mb-2">
              {{ formatDate(conversation.createdAt) }}
            </div>
            <div class="text-sm text-muted-foreground">
              {{ conversation.messageCount }} 条消息
            </div>
          </div>
        </div>
        <div v-if="conversations.length === 0" class="text-center text-muted-foreground py-12">
          暂无历史会话
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import AppHeader from '../components/layout/AppHeader.vue';
import AppAside from '../components/layout/AppAside.vue';
import { useChat } from '../composables/useChat';

const router = useRouter();
const { conversations, selectConversation, fetchConversations } = useChat();

const handleSelectConversation = async (conversationId: string) => {
  await selectConversation(conversationId);
  router.push('/chat');
};

const formatDate = (dateString: string) => {
  const date = new Date(dateString);
  return date.toLocaleString('zh-CN', { 
    year: 'numeric',
    month: 'short', 
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

onMounted(async () => {
  await fetchConversations();
});
</script>
