<!-- eslint-disable vue/multi-word-component-names -->
// eslint-disable-next-line vue/multi-word-component-names
<template>
  <MainLayout
    ref="layoutRef"
    :has-three-columns="true"
    :has-right-column="true"
    :show-layout-toggle="true"
    :has-right-splitter="true"
  >
    <!-- 中间内容区域插槽 -->
    <template #content>
      <div class="h-full flex flex-col overflow-hidden">
        <MessageList :messages="messages" />
        <InputArea :on-send="handleSendMessage" />
      </div>
    </template>

    <!-- 右侧知识库可视化区域插槽 -->
    <template #right>
      <KnowledgeVisualization
        :style="{ width: '100%' }"
        :is-collapsed="isKnowledgeCollapsed"
        @expand="handleExpandKnowledge"
        @open-fullscreen="showKnowledgeFullscreen = true"
      />
    </template>

    <!-- 全屏模态窗口插槽 -->
    <template #fullscreen-modal>
      <!-- 知识库全屏模态窗口 -->
      <div v-if="showKnowledgeFullscreen" class="fixed inset-0 z-50 bg-black/60 flex items-center justify-center">
        <div class="bg-base-100 rounded-lg w-11/12 h-5/6 flex flex-col overflow-hidden">
          <div class="border-b border-base-200 p-4 flex items-center justify-between">
            <h2 class="text-xl font-semibold">知识图谱全屏视图</h2>
            <button @click="showKnowledgeFullscreen = false" class="btn btn-ghost btn-square">
              <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <line x1="18" y1="6" x2="6" y2="18"/>
                <line x1="6" y1="6" x2="18" y2="18"/>
              </svg>
            </button>
          </div>
          <div class="flex-1 overflow-auto p-4">
            <!-- 放大版的知识图谱内容 -->
            <div class="h-full flex items-center justify-center border-2 border-dashed border-base-300 rounded-lg">
              <div class="text-center">
                <svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" stroke-linecap="round" stroke-linejoin="round" class="mx-auto mb-4 text-primary">
                  <polygon points="12 2 2 7 12 12 22 7 12 2"/>
                  <polyline points="2 17 12 22 22 17"/>
                  <polyline points="2 12 12 17 22 12"/>
                </svg>
                <p class="text-lg font-medium">知识图谱可视化</p>
                <p class="text-sm text-base-content/70 mt-2">全屏模式下可查看详细的知识节点和关系网络</p>
              </div>
            </div>
          </div>
          <div class="border-t border-base-200 p-3 flex justify-end">
            <button @click="showKnowledgeFullscreen = false" class="btn btn-primary">关闭全屏</button>
          </div>
        </div>
      </div>
    </template>
  </MainLayout>
</template>

<script setup lang="ts">
import { onMounted, ref, computed } from 'vue';
import MainLayout from '../components/layout/MainLayout.vue';
import MessageList from '../components/chat/MessageList.vue';
import InputArea from '../components/chat/InputArea.vue';
import KnowledgeVisualization from '../components/knowledge/KnowledgeVisualization.vue';
import { useChat } from '../composables/useChat';

const { messages, sendMessage, fetchConversations } = useChat();
const showKnowledgeFullscreen = ref(false);
const layoutRef = ref<InstanceType<typeof MainLayout> | null>(null);

// 计算知识库是否折叠
const isKnowledgeCollapsed = computed(() => {
  if (layoutRef.value) {
    const width = layoutRef.value.rightColumnWidth;
    return width < 100;
  }
  return true;
});

const handleExpandKnowledge = () => {
  if (layoutRef.value) {
    layoutRef.value.handleRightColumnToggle();
  }
};

const handleSendMessage = async (content: string) => {
  await sendMessage(content);
};

onMounted(() => {
  fetchConversations();
});
</script>
