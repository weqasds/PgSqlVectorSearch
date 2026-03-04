// eslint-disable-next-line vue/multi-word-component-names
<template>
  <div class="min-h-screen flex items-center justify-center bg-base-100">
    <div class="w-full max-w-md p-8 bg-white rounded-lg shadow-md">
      <h1 class="text-2xl font-bold mb-6 text-center">登录</h1>
      <form @submit.prevent="handleLogin" class="space-y-4">
        <div>
          <label for="username" class="block text-sm font-medium mb-1">用户名</label>
          <input
            type="text"
            id="username"
            v-model="form.username"
            class="w-full p-2 border border-base-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            required
          />
        </div>
        <div>
          <label for="password" class="block text-sm font-medium mb-1">密码</label>
          <input
            type="password"
            id="password"
            v-model="form.password"
            class="w-full p-2 border border-base-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
            required
          />
        </div>
        <button
          type="submit"
          class="w-full btn btn-primary"
          :disabled="isLoading"
        >
          {{ isLoading ? '登录中...' : '登录' }}
        </button>
        <button
          type="button"
          @click="handleDemoLogin"
          class="w-full btn btn-ghost"
        >
          演示登录
        </button>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '../stores/auth';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();
const isLoading = ref(false);

const form = ref({
  username: '',
  password: ''
});

const handleLogin = async () => {
  isLoading.value = true;
  try {
    await authStore.login(form.value.username, form.value.password);
    router.push('/chat');
  } catch (error) {
    console.error('Login error:', error);
    alert('登录失败，请检查用户名和密码');
  } finally {
    isLoading.value = false;
  }
};

const handleDemoLogin = async () => {
  isLoading.value = true;
  try {
    // 模拟登录，实际项目中应该调用真实的登录接口
    await authStore.login('demo', 'demo');
    router.push('/chat');
  } catch (error) {
    console.error('Demo login error:', error);
    alert('演示登录失败');
  } finally {
    isLoading.value = false;
  }
};
</script>
