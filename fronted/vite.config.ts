import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueJsx from '@vitejs/plugin-vue-jsx'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueJsx(),
    tailwindcss({
      config: {
        content: [
          './index.html',
          './src/**/*.{vue,js,ts,jsx,tsx}',
        ],
        daisyui: {
          themes: ['light', 'dark'],
        },
      },
    }),
  ],
})
