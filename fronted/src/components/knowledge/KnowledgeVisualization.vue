<template>
  <div class="h-full flex flex-col bg-base-100 border-l border-base-200">
    <!-- 顶部标题和工具栏 -->
    <div class="border-b border-base-200 p-4">
      <div class="flex items-center justify-between">
        <h2 v-if="!props.isCollapsed" class="text-lg font-semibold">知识库可视化</h2>
        <div v-else class="flex items-center">
          <button @click="handleExpandClick" class="btn btn-ghost btn-sm btn-square" title="展开知识库">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="15 18 9 12 15 6"/>
            </svg>
          </button>
        </div>
        <div class="flex items-center space-x-2">
          <button v-if="!props.isCollapsed" class="btn btn-ghost btn-sm btn-square" title="设置">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="3"/>
              <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l-.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"/>
            </svg>
          </button>
        </div>
      </div>
      <div v-if="!props.isCollapsed" class="mt-2 flex space-x-2">
        <button class="btn btn-xs btn-outline btn-active">知识图谱</button>
        <button class="btn btn-xs btn-outline">文档引用</button>
        <button class="btn btn-xs btn-outline">检索结果</button>
      </div>
    </div>

    <!-- 主内容区域 -->
    <div v-if="!props.isCollapsed" class="flex-1 overflow-auto p-4">
      <!-- 知识图谱可视化占位 -->
      <div
        class="rounded-lg border-2 border-dashed border-base-300 bg-base-200 h-64 flex items-center justify-center mb-4 cursor-pointer hover:border-primary hover:bg-base-300 transition-all duration-200"
        @click="handleGraphClick"
      >
        <div class="text-center">
          <svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" stroke-linecap="round" stroke-linejoin="round" class="mx-auto mb-2 text-base-400">
            <polygon points="12 2 2 7 12 12 22 7 12 2"/>
            <polyline points="2 17 12 22 22 17"/>
            <polyline points="2 12 12 17 22 12"/>
          </svg>
          <p class="text-sm text-base-content/70">知识图谱可视化区域</p>
          <p class="text-xs text-base-content/70 mt-1">显示RAG检索的知识节点和关系</p>
          <p class="text-xs text-primary mt-2">点击查看全屏</p>
        </div>
      </div>

      <!-- 文档引用列表 -->
      <div>
        <h3 class="text-sm font-medium mb-2">相关文档引用</h3>
        <div class="space-y-2">
          <div class="p-3 rounded-lg bg-base-200 border border-base-300 hover:bg-base-300 transition-colors cursor-pointer">
            <div class="flex justify-between items-start">
              <div>
                <h4 class="text-sm font-medium">向量检索原理</h4>
                <p class="text-xs text-base-content/70 mt-1">pgvector扩展提供了高效的向量相似度搜索功能，支持余弦距离...</p>
              </div>
              <span class="badge badge-sm badge-outline">0.87</span>
            </div>
            <div class="mt-2 text-xs text-base-content/70 flex items-center">
              <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="mr-1">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                <polyline points="14 2 14 8 20 8"/>
                <line x1="16" y1="13" x2="8" y2="13"/>
                <line x1="16" y1="17" x2="8" y2="17"/>
                <polyline points="10 9 9 9 8 9"/>
              </svg>
              knowledge-base.md • 第 12-25 行
            </div>
          </div>

          <div class="p-3 rounded-lg bg-base-200 border border-base-300 hover:bg-base-300 transition-colors cursor-pointer">
            <div class="flex justify-between items-start">
              <div>
                <h4 class="text-sm font-medium">RAG架构设计</h4>
                <p class="text-xs text-base-content/70 mt-1">DDD分层架构将系统划分为领域层、应用层和基础设施层...</p>
              </div>
              <span class="badge badge-sm badge-outline">0.92</span>
            </div>
            <div class="mt-2 text-xs text-base-content/70 flex items-center">
              <svg xmlns="http://www.w3.org/2000/svg" width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="mr-1">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                <polyline points="14 2 14 8 20 8"/>
                <line x1="16" y1="13" x2="8" y2="13"/>
                <line x1="16" y1="17" x2="8" y2="17"/>
                <polyline points="10 9 9 9 8 9"/>
              </svg>
              architecture.md • 第 45-67 行
            </div>
          </div>
        </div>
      </div>
    </div>
    <div v-else class="flex-1 flex items-center justify-center p-4">
      <div class="text-center">
        <svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1" stroke-linecap="round" stroke-linejoin="round" class="mx-auto mb-2 text-base-400">
          <polygon points="12 2 2 7 12 12 22 7 12 2"/>
          <polyline points="2 17 12 22 22 17"/>
          <polyline points="2 12 12 17 22 12"/>
        </svg>
        <p class="text-xs text-base-content/70">知识库</p>
        <p class="text-xs text-primary mt-1">点击展开</p>
      </div>
    </div>

    <!-- 底部状态栏 -->
    <div v-if="!props.isCollapsed" class="border-t border-base-200 p-2 text-xs text-base-content/70 flex justify-between">
      <span>知识节点: 12</span>
      <span>关系边: 18</span>
      <span>检索耗时: 127ms</span>
    </div>
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  isCollapsed?: boolean;
}>();

const emit = defineEmits<{
  expand: [];
  'open-fullscreen': [];
}>();

const handleGraphClick = () => {
  emit('open-fullscreen');
};

const handleExpandClick = () => {
  emit('expand');
};
</script>