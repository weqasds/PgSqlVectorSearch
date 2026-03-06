import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import App from './App.vue'
import { useSettingsStore } from './stores/settings'

import './main.css'

const app = createApp(App)

app.use(createPinia())
app.use(router)

// Initialize settings store to apply theme
const settingsStore = useSettingsStore()
settingsStore.init()

app.mount('#app')
