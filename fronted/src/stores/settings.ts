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
      this.applyFontSize();
      this.saveSettings();
    },
    updateMessageDensity(density: MessageDensity) {
      this.messageDensity = density;
      this.applyMessageDensity();
      this.saveSettings();
    },
    applyTheme() {
      let themeToApply = this.theme;
      if (themeToApply === 'system') {
        themeToApply = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
      }
      document.documentElement.setAttribute('data-theme', themeToApply);
    },
    applyFontSize() {
      const html = document.documentElement;
      const fontSizeMap = {
        small: '14px',
        medium: '16px',
        large: '18px'
      };
      html.style.setProperty('--font-size-base', fontSizeMap[this.fontSize]);
    },
    applyMessageDensity() {
      const html = document.documentElement;
      const densityMap = {
        compact: '0.5rem',
        comfortable: '1rem',
        spacious: '1.5rem'
      };
      html.style.setProperty('--message-spacing', densityMap[this.messageDensity]);
    },
    saveSettings() {
      localStorage.setItem('settings', JSON.stringify(this.$state));
    },
    init() {
      this.applyTheme();
      this.applyFontSize();
      this.applyMessageDensity();
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        if (this.theme === 'system') {
          this.applyTheme();
        }
      });
    }
  }
});
