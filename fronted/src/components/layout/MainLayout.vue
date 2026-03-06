<template>
  <div class="h-screen flex flex-col">
    <AppHeader />
    <div class="flex flex-1 overflow-hidden">
      <!-- 三栏布局模式 -->
      <div v-if="layoutMode === 'three-column' && hasThreeColumns" class="flex-1 flex overflow-hidden">
        <!-- 左侧导航栏 -->
        <AppAside class="flex-none" :style="{ width: `${sidebarWidth}px` }" />
        <!-- 左侧分隔线 -->
        <Splitter @drag-move="handleSidebarDrag" />
        <!-- 中间内容区域 -->
        <div class="flex-1 min-w-0 flex flex-col overflow-hidden">
          <slot name="content" />
        </div>


        <!-- 右侧分隔线 -->
        <Splitter v-if="hasRightSplitter" @drag-move="handleKnowledgeDrag" />

        <!-- 右侧附加区域 -->
        <div
          v-if="hasRightColumn"
          class="flex-none overflow-hidden"
          :style="{ width: rightColumnWidth + 'px' }"
        >
          <slot name="right" />
        </div>
      </div>

      <!-- 抽屉布局模式 -->
      <div v-else class="flex-1 flex overflow-hidden relative">
        <!-- 左侧导航栏 -->
        <AppAside class="flex-none" :style="{ width: `${sidebarWidth}px` }" />
        <!-- 左侧分隔线 -->
        <Splitter @drag-move="handleSidebarDrag" />
        <!-- 中间内容区域（全宽） -->
        <div class="flex-1 flex flex-col overflow-hidden">
          <slot name="content" />
        </div>

        <!-- 右侧附加区域（抽屉式） -->
        <div
          v-if="hasRightColumn"
          class="absolute top-0 right-0 h-full bg-base-100 shadow-xl"
          :style="{ width: `${rightColumnWidth}px` }"
        >
          <slot name="right" />
        </div>
      </div>
    </div>

    <!-- 全屏模态窗口插槽 -->
    <slot name="fullscreen-modal" />

    <!-- 布局模式切换按钮（右下角） -->
    <button
      v-if="showLayoutToggle"
      @click="uiStore.toggleLayoutMode()"
      class="absolute right-4 bottom-4 btn btn-ghost btn-sm btn-circle bg-base-100 border border-base-300 shadow-lg z-10"
      :title="layoutMode === 'three-column' ? '切换到抽屉模式' : '切换到三栏模式'"
    >
      <svg v-if="layoutMode === 'three-column'" xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
        <line x1="9" y1="3" x2="9" y2="21"/>
        <line x1="15" y1="3" x2="15" y2="21"/>
      </svg>
      <svg v-else xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/>
        <line x1="15" y1="3" x2="15" y2="21"/>
      </svg>
    </button>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted } from 'vue';
import AppHeader from './AppHeader.vue';
import AppAside from './AppAside.vue';
import Splitter from './Splitter.vue';
import { useUIStore } from '../../stores/ui';

const props = withDefaults(defineProps<{
  // 是否显示三列布局（默认为true，对于Chat页面）
  hasThreeColumns?: boolean;
  // 是否显示右侧列（默认为true，对于Chat页面）
  hasRightColumn?: boolean;
  // 是否显示布局切换按钮（默认为true，对于Chat页面）
  showLayoutToggle?: boolean;
  // 是否显示右侧分隔线（默认为true）
  hasRightSplitter?: boolean;
}>(), {
  hasThreeColumns: true,
  hasRightColumn: true,
  showLayoutToggle: true,
  hasRightSplitter: true,
});

const uiStore = useUIStore();
const windowWidth = ref(window.innerWidth);

// 响应式布局：小屏幕自动使用抽屉模式
const layoutMode = computed(() => {
  // 如果屏幕宽度小于1024px，强制使用抽屉模式以获得更好的移动端体验
  if (windowWidth.value < 1024) {
    return 'drawer';
  }
  return uiStore.layoutMode;
});

// 检查是否是响应式抽屉模式（由于窗口宽度小）
const isResponsiveDrawer = computed(() => windowWidth.value < 1024);

// 宽度状态
const sidebarWidth = computed(() =>
  {
    return uiStore.sidebarCollapsed ? 60 : uiStore.sidebarWidth
  });
const rightColumnWidth = computed(() => {
  // 如果是响应式抽屉模式
  if (isResponsiveDrawer.value && props.hasRightColumn) {
    // 响应式模式下，根据本地展开状态决定宽度
    return knowledgeExpandedInResponsiveMode.value ? 300 : 60;
  }

  // 用户选择的抽屉模式
  if (layoutMode.value === 'drawer' && props.hasRightColumn) {
    return uiStore.knowledgeWidth;
  }

  // 三栏模式
  if (props.hasRightColumn) {
    return uiStore.knowledgeWidth;
  }

  return 0;
});

// 本地状态：响应式模式下的右侧列展开状态
const knowledgeExpandedInResponsiveMode = ref(false);

const updateWindowWidth = () => {
  windowWidth.value = window.innerWidth;
};

// 拖拽处理函数
const handleSidebarDrag = (delta: number) => {
  //如果当前侧边栏没有展开，直接变为小宽度
  if (uiStore.sidebarCollapsed==true) {
    return;
  }
  const desired = uiStore.sidebarWidth + delta
  const width = Math.max(180, Math.min(desired, 500))
  uiStore.setSidebarWidth(width)
}


const handleKnowledgeDrag = (delta: number) => {
  const desired = uiStore.knowledgeWidth - delta

  uiStore.setKnowledgeWidth(
    Math.max(240, Math.min(desired, 600))
  )
}
// 右侧列展开/折叠处理函数（暴露给父组件）
const handleRightColumnToggle = () => {
  // 如果是响应式抽屉模式，切换本地展开状态
  if (isResponsiveDrawer.value) {
    knowledgeExpandedInResponsiveMode.value = !knowledgeExpandedInResponsiveMode.value;
    return;
  }

  // 用户选择的抽屉模式或三栏模式，更新store中的宽度
  if (uiStore.knowledgeWidth < 100) {
    // 当前折叠状态，展开到默认宽度
    uiStore.setKnowledgeWidth(layoutMode.value === 'three-column' ? 400 : 300);
  } else {
    // 当前展开状态，折叠到最小宽度
    uiStore.setKnowledgeWidth(60);
  }
};

// 暴露方法给父组件
defineExpose({
  handleRightColumnToggle,
  rightColumnWidth,
  layoutMode,
});

onMounted(() => {
  window.addEventListener('resize', updateWindowWidth);
  updateWindowWidth(); // 初始调用
});

onUnmounted(() => {
  window.removeEventListener('resize', updateWindowWidth);
});
</script>
