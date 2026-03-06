<template>
  <MainLayout
    :has-three-columns="false"
    :has-right-column="false"
    :show-layout-toggle="false"
  >
    <!-- 内容区域插槽 -->
    <template #content>
      <div class="h-full p-6 overflow-y-auto">
        <h1 class="text-2xl font-bold mb-6">设置</h1>
        <div class="max-w-md mx-auto bg-base-100 rounded-lg shadow-md p-6">
          <div class="mb-6">
            <h2 class="text-lg font-semibold mb-4">主题设置</h2>
            <div class="space-y-2">
              <label class="flex items-center space-x-2 cursor-pointer">
                <input
                  type="radio"
                  v-model="settings.theme"
                  value="light"
                  @change="handleThemeChange"
                  class="radio theme-controller"
                  name="theme-radio"
                />
                <span>明亮</span>
              </label>
              <label class="flex items-center space-x-2 cursor-pointer">
                <input
                  type="radio"
                  v-model="settings.theme"
                  value="dark"
                  @change="handleThemeChange"
                  class="radio theme-controller"
                  name="theme-radio"
                />
                <span>暗黑</span>
              </label>
              <label class="flex items-center space-x-2 cursor-pointer">
                <input
                  type="radio"
                  v-model="settings.theme"
                  value="system"
                  @change="handleThemeChange"
                  class="radio theme-controller"
                  name="theme-radio"
                />
                <span>跟随系统</span>
              </label>
            </div>
          </div>

          <div class="mb-6">
            <h2 class="text-lg font-semibold mb-4">聊天设置</h2>
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium mb-1">字体大小</label>
                <select
                  v-model="settings.fontSize"
                  @change="handleFontSizeChange"
                  class="w-full p-2 border border-base-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                >
                  <option value="small">小</option>
                  <option value="medium">中</option>
                  <option value="large">大</option>
                </select>
              </div>
              <div>
                <label class="block text-sm font-medium mb-1">消息密度</label>
                <select
                  v-model="settings.messageDensity"
                  @change="handleMessageDensityChange"
                  class="w-full p-2 border border-base-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
                >
                  <option value="compact">紧凑</option>
                  <option value="comfortable">舒适</option>
                  <option value="spacious">宽松</option>
                </select>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </MainLayout>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import MainLayout from '../components/layout/MainLayout.vue';
import { useSettingsStore, type ThemeType, type FontSize, type MessageDensity } from '../stores/settings';

const settingsStore = useSettingsStore();

const settings = ref({
  theme: settingsStore.theme as ThemeType,
  fontSize: settingsStore.fontSize as FontSize,
  messageDensity: settingsStore.messageDensity as MessageDensity
});

const handleThemeChange = () => {
  settingsStore.updateTheme(settings.value.theme);
};

const handleFontSizeChange = () => {
  settingsStore.updateFontSize(settings.value.fontSize);
};

const handleMessageDensityChange = () => {
  settingsStore.updateMessageDensity(settings.value.messageDensity);
};

onMounted(() => {
  settingsStore.init();
});
</script>
