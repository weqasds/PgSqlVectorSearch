import { defineStore } from 'pinia';
import { authApi, type UserInfo } from '../api/auth';

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('token') || '',
    userInfo: null as UserInfo | null,
    isLoggedIn: !!localStorage.getItem('token')
  }),
  actions: {
    async login(username: string, password: string) {
      const response = await authApi.login({ username, password });
      this.token = response.token;
      this.userInfo = response.user;
      this.isLoggedIn = true;
      localStorage.setItem('token', response.token);
      localStorage.setItem('userInfo', JSON.stringify(response.user));
    },
    async logout() {
      try {
        await authApi.logout();
      } catch (error) {
        console.error('Logout error:', error);
      } finally {
        this.token = '';
        this.userInfo = null;
        this.isLoggedIn = false;
        localStorage.removeItem('token');
        localStorage.removeItem('userInfo');
      }
    },
    async getUserInfo() {
      try {
        const userInfo = await authApi.getUserInfo();
        this.userInfo = userInfo;
        localStorage.setItem('userInfo', JSON.stringify(userInfo));
      } catch (error) {
        console.error('Get user info error:', error);
        this.logout();
      }
    },
    init() {
      const storedUserInfo = localStorage.getItem('userInfo');
      if (storedUserInfo) {
        this.userInfo = JSON.parse(storedUserInfo);
      }
    }
  }
});
