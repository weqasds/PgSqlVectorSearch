import { defineStore } from 'pinia'

export type LayoutMode = 'three-column' | 'drawer'

export interface UIState {
  sidebarCollapsed: boolean
  layoutMode: LayoutMode
  sidebarWidth: number
  chatWidth: number
  knowledgeWidth: number
}

const defaultUIState: UIState = {
  sidebarCollapsed: false,
  layoutMode: 'three-column', // 默认三栏布局
  sidebarWidth: 256,
  chatWidth: 600,
  knowledgeWidth: 384,
}

export const useUIStore = defineStore('ui', {
  state: (): UIState => {
    const storedUI = localStorage.getItem('ui')
    if (!storedUI) return { ...defaultUIState }

    try {
      const parsed = JSON.parse(storedUI)
      return {
        ...defaultUIState,
        ...parsed,
      }
    } catch {
      return { ...defaultUIState }
    }
  },
  actions: {
    toggleSidebar() {
      this.sidebarCollapsed = !this.sidebarCollapsed
      this.saveUI()
    },
    setSidebarCollapsed(collapsed: boolean) {
      this.sidebarCollapsed = collapsed
      this.saveUI()
    },
    toggleLayoutMode() {
      const newMode = this.layoutMode === 'three-column' ? 'drawer' : 'three-column'
      this.layoutMode = newMode

      // 切换模式时调整知识库宽度
      if (newMode === 'drawer') {
        // 抽屉模式：知识库区域默认折叠为60px
        this.knowledgeWidth = 60
      } else {
        // 三栏模式：恢复默认宽度
        this.knowledgeWidth = defaultUIState.knowledgeWidth
      }

      this.saveUI()
    },
    setLayoutMode(mode: LayoutMode) {
      this.layoutMode = mode
      this.saveUI()
    },
    setSidebarWidth(width: number) {
      // 允许折叠到较小尺寸，最小 60px
      this.sidebarWidth = Math.max(60, Math.min(width, 500))
      this.saveUI()
    },
    setChatWidth(width: number) {
      // 聊天区域最小设置为 200px，防止完全消失
      this.chatWidth = Math.max(200, Math.min(width, 800))
      this.saveUI()
    },
    setKnowledgeWidth(width: number) {
      // 知识库可折叠到 60px
      this.knowledgeWidth = Math.max(60, Math.min(width, 600))
      this.saveUI()
    },
    resetLayout() {
      this.sidebarWidth = defaultUIState.sidebarWidth
      this.chatWidth = defaultUIState.chatWidth
      this.knowledgeWidth = defaultUIState.knowledgeWidth
      this.saveUI()
    },
    saveUI() {
      localStorage.setItem('ui', JSON.stringify(this.$state))
    },
    init() {
      // Future initialization if needed
    },
  },
})
