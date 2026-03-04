<template>
  <div class="border-t border-base-200 p-4 bg-base-100">
    <form @submit.prevent="handleSubmit" class="flex space-x-2">
      <textarea 
        v-model="inputValue"
        placeholder="输入消息..."
        class="flex-1 p-3 border border-base-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary resize-none"
        rows="1"
        @keydown.enter.exact="handleSubmit"
        @keydown.enter.shift="$event.preventDefault()"
      ></textarea>
      <button 
        type="submit" 
        class="btn btn-primary flex-shrink-0"
        :disabled="!inputValue.trim()"
      >
        发送
      </button>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps<{
  onSend: (content: string) => void;
}>();

const inputValue = ref('');

const handleSubmit = () => {
  const content = inputValue.value.trim();
  if (content) {
    props.onSend(content);
    inputValue.value = '';
  }
};
</script>
