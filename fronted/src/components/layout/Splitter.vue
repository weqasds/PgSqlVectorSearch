<template>
  <div
    class="splitter"
    :class="[orientation === 'vertical' ? 'splitter-vertical' : 'splitter-horizontal']"
    @mousedown="startDrag"
    @touchstart="startDrag"
  >
    <div class="splitter-handle"></div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

// ensure component name is multi-word to satisfy linter
defineOptions({ name: 'LayoutSplitter' });

const props = withDefaults(defineProps<{
  orientation?: 'vertical' | 'horizontal';
}>(), {
  orientation: 'vertical'
});

const emit = defineEmits<{
  'drag-start': []
  'drag-move': [delta: number]
  'drag-end': []
}>();

const isDragging = ref(false);
const startX = ref(0);
const startY = ref(0);

const startDrag = (e: MouseEvent | TouchEvent) => {
  isDragging.value = true;

  let clientX: number;
  let clientY: number;

  if ('touches' in e && e.touches && e.touches.length > 0) {
    clientX = e.touches[0]!.clientX;
    clientY = e.touches[0]!.clientY;
  } else if (!('touches' in e)) {
    clientX = (e as MouseEvent).clientX;
    clientY = (e as MouseEvent).clientY;
  } else {
    // Fallback
    clientX = 0;
    clientY = 0;
  }

  startX.value = clientX;
  startY.value = clientY;

  emit('drag-start');

  // Add global event listeners
  document.addEventListener('mousemove', handleDrag);
  document.addEventListener('touchmove', handleDrag, { passive: false });
  document.addEventListener('mouseup', stopDrag);
  document.addEventListener('touchend', stopDrag);

  e.preventDefault();
};

const handleDrag = (e: MouseEvent | TouchEvent) => {
  if (!isDragging.value) return;

  let clientX: number;
  let clientY: number;

  if ('touches' in e && e.touches && e.touches.length > 0) {
    clientX = e.touches[0]!.clientX;
    clientY = e.touches[0]!.clientY;
  } else if (!('touches' in e)) {
    clientX = (e as MouseEvent).clientX;
    clientY = (e as MouseEvent).clientY;
  } else {
    // Fallback
    clientX = 0;
    clientY = 0;
  }

  let delta = 0;
  if (props.orientation === 'vertical') {
    delta = clientX - startX.value;
    startX.value = clientX;
  } else {
    delta = clientY - startY.value;
    startY.value = clientY;
  }

  emit('drag-move', delta);

  if (e.type === 'touchmove') {
    e.preventDefault();
  }
};

const stopDrag = () => {
  if (!isDragging.value) return;

  isDragging.value = false;
  emit('drag-end');

  // Remove global event listeners
  document.removeEventListener('mousemove', handleDrag);
  document.removeEventListener('touchmove', handleDrag);
  document.removeEventListener('mouseup', stopDrag);
  document.removeEventListener('touchend', stopDrag);
};
</script>

<style scoped>
.splitter {
  position: relative;
  z-index: 10;
  user-select: none;
}

.splitter-vertical {
  width: 8px;
  cursor: col-resize;
  margin: 0 -4px;
}

.splitter-horizontal {
  height: 8px;
  cursor: row-resize;
  margin: -4px 0;
}

.splitter-handle {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  transition: background-color 0.2s ease;
}

.splitter-vertical .splitter-handle {
  left: 2px;
  right: 2px;
}

.splitter-horizontal .splitter-handle {
  top: 2px;
  bottom: 2px;
}

.splitter:hover .splitter-handle,
.splitter-vertical:active .splitter-handle,
.splitter-horizontal:active .splitter-handle {
  background-color: rgba(251, 252, 252, 0);
}
</style>
