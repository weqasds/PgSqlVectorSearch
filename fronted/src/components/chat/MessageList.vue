<template>
  <div class="flex-1 overflow-y-auto p-4 message-list flex flex-col">
    <div v-if="messages.length === 0" class="flex items-center justify-center h-full text-base-content/70">
      开始新的对话吧
    </div>
    <MessageItem
      v-for="message in messages"
      :key="message.id"
      :message="message"
      class="message-item"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import MessageItem from './MessageItem.vue';
import { Message } from '../../api/chat';

const props = defineProps<{
  messages: Message[];
}>();

const messages = ref(props.messages);

watch(() => props.messages, (newMessages) => {
  messages.value = newMessages;
  nextTick(() => {
    const container = document.querySelector('.flex-1.overflow-y-auto');
    if (container) {
      container.scrollTop = container.scrollHeight;
    }
  });
}, { deep: true });
</script>
