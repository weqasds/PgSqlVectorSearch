import { defineStore } from 'pinia';

export type ThemeType = 'light' | 'dark' | 'system';
export type FontSize = 'small' | 'medium' | 'large';
export type MessageDensity = 'compact' | 'comfortable' | 'spacious';

export interface SettingsState {
  theme: ThemeType;
  fontSize: FontSize;
  messageDensity: MessageDensity;
}

const defaultSettings: SettingsState = {
  theme: 'system',
  fontSize: 'medium',
  messageDensity: 'comfortable'
};

export const useSettingsStore = defineStore('settings', {
  state: (): SettingsState => {
    const storedSettings = localStorage.getItem('settings');
    return storedSettings ? JSON.parse(storedSettings) : defaultSettings;
  },
  actions: {
    updateTheme(theme: ThemeType) {
      this.theme = theme;
      this.applyTheme();
      this.saveSettings();
    },
    updateFontSize(fontSize: FontSize) {
      this.fontSize = fontSize;
      this.saveSettings();
    },
    updateMessageDensity(density: MessageDensity) {
      this.messageDensity = density;
      this.saveSettings();
    },
    applyTheme() {
      let themeToApply = this.theme;
      if (themeToApply === 'system') {
        themeToApply = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
      }
      document.documentElement.classList.remove('light', 'dark');
      document.documentElement.classList.add(themeToApply);
    },
    saveSettings() {
      localStorage.setItem('settings', JSON.stringify(this.$state));
    },
    init() {
      this.applyTheme();
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        if (this.theme === 'system') {
          this.applyTheme();
        }
      });
    }
  }
});
