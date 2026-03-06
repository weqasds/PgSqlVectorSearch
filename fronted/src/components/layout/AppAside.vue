<template>
  <aside
    :class="['border-r border-base-200 bg-base-100 h-[calc(100vh-64px)] overflow-y-auto transition-all duration-300 ease-in-out']">
    <div class="p-4">
      <!-- 顶部标题和折叠按钮区域 -->
      <div class="flex items-center mb-4">
        <!-- 展开时：显示AI Chat标题，右侧显示折叠按钮 -->
        <div v-if="!isCollapsed" class="flex items-center justify-between w-full">
          <h1 class="text-lg font-semibold">AI Chat</h1>
          <button
            @click="uiStore.toggleSidebar()"
            class="btn btn-ghost btn-sm btn-square rounded-full transition-all duration-200 ease-in-out hover:scale-110"
            title="折叠侧边栏"
            @mouseenter="toggleButtonHover = true"
            @mouseleave="toggleButtonHover = false"
          >
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="15 18 9 12 15 6"/>
            </svg>
          </button>
        </div>

        <!-- 折叠时：显示AiChat图标，鼠标悬浮时显示展开按钮 -->
        <div v-else class="w-full flex justify-center">
          <button
            @click="uiStore.toggleSidebar()"
            class="btn btn-ghost btn-square rounded-full transition-all duration-300 ease-in-out"
            :title="toggleButtonHover ? '展开侧边栏' : 'AI Chat'"
            @mouseenter="toggleButtonHover = true"
            @mouseleave="toggleButtonHover = false"
          >
            <!-- 默认显示AiChat图标 -->
            <svg v-if="!toggleButtonHover" xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="transition-all duration-300 ease-in-out">
              <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/>
            </svg>
            <!-- 鼠标悬浮时显示展开按钮 -->
            <svg v-else xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="transition-all duration-300 ease-in-out">
              <polyline points="9 18 15 12 9 6"/>
            </svg>
          </button>
        </div>
      </div>

      <!-- 新对话按钮 -->
      <div class="mb-4" :class="{ 'flex justify-center': isCollapsed }">
        <button
          @click="handleNewConversation"
          class="btn btn-primary transition-all duration-300 ease-in-out"
          :class="isCollapsed ? 'btn-square rounded-full' : 'w-full'"
          :title="isCollapsed ? '新对话' : ''"
        >
          <svg v-if="isCollapsed" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <line x1="12" y1="5" x2="12" y2="19"/>
            <line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          <span v-else class="transition-opacity duration-300 ease-in-out">新对话</span>
        </button>
      </div>

      <div class="space-y-2">
        <h2 v-if="!isCollapsed" class="text-xs font-semibold uppercase text-base-content/70 px-2">对话历史</h2>
        <div
          v-for="conversation in chatStore.conversations"
          :key="conversation.id"
          @click="handleSelectConversation(conversation.id)"
          :class="['cursor-pointer p-2 rounded-lg hover:bg-base-200 transition-colors', { 'bg-base-200': chatStore.currentConversationId === conversation.id }, isCollapsed ? 'flex items-center justify-center' : '']"
          :title="isCollapsed ? conversation.title : ''"
        >
          <template v-if="!isCollapsed">
            <div class="text-sm font-medium truncate">{{ conversation.title }}</div>
            <div class="text-xs text-base-content/70">
              {{ formatDate(conversation.updatedAt) }} • {{ conversation.messageCount }} 条消息
            </div>
          </template>
          <template v-else>
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
          </template>
        </div>
        <div v-if="chatStore.conversations.length === 0 && !isCollapsed" class="text-center text-sm text-base-content/70 py-4">
          暂无对话历史
        </div>
      </div>
    </div>

    <div class="border-t border-base-200 p-4 mt-4">
      <nav class="space-y-1">
        <router-link
          to="/history"
          :class="['flex items-center p-2 rounded-lg hover:bg-base-200 transition-colors', isCollapsed ? 'justify-center' : 'space-x-2']"
          active-class="bg-base-200 font-medium"
          :title="isCollapsed ? '历史记录' : ''"
        >
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M12 8v4l3 3"/><circle cx="12" cy="12" r="10"/></svg>
          <span v-if="!isCollapsed">历史记录</span>
        </router-link>
        <router-link
          to="/profile"
          :class="['flex items-center p-2 rounded-lg hover:bg-base-200 transition-colors', isCollapsed ? 'justify-center' : 'space-x-2']"
          active-class="bg-base-200 font-medium"
          :title="isCollapsed ? '个人中心' : ''"
        >
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="12" cy="7" r="4"/></svg>
          <span v-if="!isCollapsed">个人中心</span>
        </router-link>
        <router-link
          to="/settings"
          :class="['flex items-center p-2 rounded-lg hover:bg-base-200 transition-colors', isCollapsed ? 'justify-center' : 'space-x-2']"
          active-class="bg-base-200 font-medium"
          :title="isCollapsed ? '设置' : ''"
        >
          <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>
          <span v-if="!isCollapsed">设置</span>
        </router-link>
      </nav>
    </div>
  </aside>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useChatStore } from '../../stores/chat';
import { useRouter } from 'vue-router';
import { useUIStore } from '../../stores/ui';

const chatStore = useChatStore();
const router = useRouter();
const uiStore = useUIStore();
const isCollapsed = computed(() => uiStore.sidebarCollapsed);
const toggleButtonHover = ref(false);

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
