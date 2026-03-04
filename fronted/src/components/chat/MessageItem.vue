<template>
  <div 
    class="flex" 
    :class="{ 'justify-end': message.role === 'user' }"
  >
    <div 
      class="max-w-[70%] p-3 rounded-lg" 
      :class="message.role === 'user' ? 'bg-primary text-white' : 'bg-base-200'"
    >
      <div v-html="renderedContent"></div>
      <div class="text-xs mt-1 opacity-70">
        {{ formatTime(message.createdAt) }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { marked } from 'marked';
import hljs from 'highlight.js';
import 'highlight.js/styles/github.css';
import { Message } from '../../api/chat';

const props = defineProps<{
  message: Message;
}>();

marked.setOptions({
  highlight: function(code, lang) {
    const language = hljs.getLanguage(lang) ? lang : 'plaintext';
    return hljs.highlight(code, { language }).value;
  },
  breaks: true,
  gfm: true
});

const renderedContent = computed(() => {
  return marked(props.message.content);
});

const formatTime = (dateString: string) => {
  const date = new Date(dateString);
  return date.toLocaleTimeString('zh-CN', { 
    hour: '2-digit',
    minute: '2-digit'
  });
};
</script>
