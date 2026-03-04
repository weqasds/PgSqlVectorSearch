<template>
  <div class="h-screen flex flex-col">
    <AppHeader />
    <div class="flex flex-1 overflow-hidden">
      <AppAside />
      <div class="flex-1 p-6 overflow-y-auto">
        <h1 class="text-2xl font-bold mb-6">个人中心</h1>
        <div class="max-w-md mx-auto bg-white rounded-lg shadow-md p-6">
          <div class="flex flex-col items-center mb-6">
            <img 
              v-if="authStore.userInfo?.avatar" 
              :src="authStore.userInfo.avatar" 
              alt="User avatar"
              class="w-24 h-24 rounded-full mb-4"
            />
            <span v-else class="w-24 h-24 rounded-full bg-primary flex items-center justify-center text-white text-4xl mb-4">
              {{ authStore.userInfo?.username?.charAt(0).toUpperCase() || 'U' }}
            </span>
            <h2 class="text-xl font-bold">{{ authStore.userInfo?.username || 'User' }}</h2>
            <p class="text-muted-foreground">{{ authStore.userInfo?.email || 'No email' }}</p>
          </div>
          <div class="space-y-4">
            <div>
              <label for="username" class="block text-sm font-medium mb-1">用户名</label>
              <input 
                type="text" 
                id="username" 
                v-model="form.username"
                class="w-full p-2 border border-base-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <div>
              <label for="email" class="block text-sm font-medium mb-1">邮箱</label>
              <input 
                type="email" 
                id="email" 
                v-model="form.email"
                class="w-full p-2 border border-base-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              />
            </div>
            <button 
              @click="handleSave" 
              class="w-full btn btn-primary"
            >
              保存
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import AppHeader from '../components/layout/AppHeader.vue';
import AppAside from '../components/layout/AppAside.vue';
import { useAuthStore } from '../stores/auth';

const authStore = useAuthStore();

const form = ref({
  username: '',
  email: ''
});

const handleSave = () => {
  // 这里应该调用更新用户信息的API
  alert('保存成功');
};

onMounted(() => {
  if (authStore.userInfo) {
    form.value.username = authStore.userInfo.username;
    form.value.email = authStore.userInfo.email;
  }
});
</script>
