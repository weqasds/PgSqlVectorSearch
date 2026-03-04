<template>
  <div class="h-screen flex flex-col">
    <AppHeader />
    <div class="flex flex-1 overflow-hidden">
      <AppAside />
      <div class="flex-1 flex flex-col overflow-hidden">
        <MessageList :messages="messages" />
        <InputArea @on-send="handleSendMessage" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import AppHeader from '../components/layout/AppHeader.vue';
import AppAside from '../components/layout/AppAside.vue';
import MessageList from '../components/chat/MessageList.vue';
import InputArea from '../components/chat/InputArea.vue';
import { useChat } from '../composables/useChat';

const { messages, sendMessage, fetchConversations } = useChat();

const handleSendMessage = async (content: string) => {
  await sendMessage(content);
};

onMounted(async () => {
  await fetchConversations();
});
</script>
