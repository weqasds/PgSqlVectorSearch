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

export const authApi = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const response = await axiosInstance.post('/auth/login', data);
    return response.data;
  },
  logout: async (): Promise<void> => {
    await axiosInstance.post('/auth/logout');
  },
  getUserInfo: async (): Promise<UserInfo> => {
    const response = await axiosInstance.get('/auth/user');
    return response.data;
  }
};
