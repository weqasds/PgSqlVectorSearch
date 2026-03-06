<template>
  <header class="bg-base-100 border-b border-base-200 py-3 px-4 flex items-center justify-between">
    <div class="flex items-center space-x-2">
    </div>
    <div class="flex items-center space-x-4">
      <!-- 布局模式切换按钮 -->
      <button
        @click="uiStore.toggleLayoutMode()"
        class="btn btn-sm btn-ghost hover:bg-base-200"
        :title="uiStore.layoutMode === 'three-column' ? '切换到抽屉模式' : '切换到三栏模式'"
      >
        <svg v-if="uiStore.layoutMode === 'three-column'" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
          <line x1="9" y1="3" x2="9" y2="21"/>
          <line x1="15" y1="3" x2="15" y2="21"/>
        </svg>
        <svg v-else xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
          <line x1="15" y1="3" x2="15" y2="21"/>
        </svg>
      </button>

      <div class="flex items-center space-x-2">
        <img
          v-if="authStore.userInfo?.avatar"
          :src="authStore.userInfo.avatar"
          alt="User avatar"
          class="w-8 h-8 rounded-full"
        />
        <span v-else class="w-8 h-8 rounded-full bg-primary flex items-center justify-center text-primary-content">
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
import { useUIStore } from '../../stores/ui';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const uiStore = useUIStore();
const router = useRouter();

const handleLogout = async () => {
  await authStore.logout();
  router.push('/login');
};
</script>
