<template>
  <header class="bg-base-100 border-b border-base-200 py-3 px-4 flex items-center justify-between">
    <div class="flex items-center space-x-2">
      <h1 class="text-xl font-bold">AI Chat</h1>
    </div>
    <div class="flex items-center space-x-4">
      <div class="flex items-center space-x-2">
        <img 
          v-if="authStore.userInfo?.avatar" 
          :src="authStore.userInfo.avatar" 
          alt="User avatar"
          class="w-8 h-8 rounded-full"
        />
        <span v-else class="w-8 h-8 rounded-full bg-primary flex items-center justify-center text-white">
          {{ authStore.userInfo?.username?.charAt(0).toUpperCase() || 'U' }}
        </span>
        <span class="text-sm font-medium">{{ authStore.userInfo?.username || 'User' }}</span>
      </div>
      <button 
        @click="handleLogout" 
        class="btn btn-sm btn-ghost hover:bg-base-200"
      >
        退出
      </button>
    </div>
  </header>
</template>

<script setup lang="ts">
import { useAuthStore } from '../../stores/auth';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();

const handleLogout = async () => {
  await authStore.logout();
  router.push('/login');
};
</script>
