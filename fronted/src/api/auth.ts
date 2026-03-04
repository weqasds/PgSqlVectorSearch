import axiosInstance from './index';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: {
    id: string;
    username: string;
    email: string;
    avatar: string;
  };
}

export interface UserInfo {
  id: string;
  username: string;
  email: string;
  avatar: string;
}

// 模拟用户数据
const mockUser: UserInfo = {
  id: 'mock-user-123',
  username: 'demo',
  email: 'demo@example.com',
  avatar: 'https://api.dicebear.com/7.x/avataaars/svg?seed=demo'
};

// 检查是否使用模拟API
const useMockApi = import.meta.env.DEV && import.meta.env.VITE_USE_MOCK_API !== 'false';

export const authApi = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    // 使用模拟API
    if (useMockApi) {
      // 模拟网络延迟
      await new Promise(resolve => setTimeout(resolve, 300));

      // 检查是否为演示账户
      if (data.username === 'demo' && data.password === 'demo') {
        const mockToken = 'mock-jwt-token-' + Date.now();
        return {
          token: mockToken,
          user: mockUser
        };
      }

      // 其他账户也允许登录，方便测试
      const mockToken = 'mock-jwt-token-' + Date.now();
      return {
        token: mockToken,
        user: {
          id: 'mock-user-' + Date.now(),
          username: data.username,
          email: data.username + '@example.com',
          avatar: `https://api.dicebear.com/7.x/avataaars/svg?seed=${data.username}`
        }
      };
    }

    // 使用真实API
    const response = await axiosInstance.post('/auth/login', data);
    return response.data;
  },
  logout: async (): Promise<void> => {
    if (useMockApi) {
      // 模拟登出
      await new Promise(resolve => setTimeout(resolve, 100));
      return;
    }
    await axiosInstance.post('/auth/logout');
  },
  getUserInfo: async (): Promise<UserInfo> => {
    if (useMockApi) {
      // 返回模拟用户信息
      await new Promise(resolve => setTimeout(resolve, 100));
      return mockUser;
    }
    const response = await axiosInstance.get('/auth/user');
    return response.data;
  }
};
