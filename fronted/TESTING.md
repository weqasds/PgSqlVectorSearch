# 前端模拟登录测试说明

## 概述

为了方便前端界面测试，项目已配置模拟登录和数据。在开发环境下，无需后端服务即可测试所有界面。

## 使用方法

### 1. 启动开发服务器
```bash
npm run dev
# 或
pnpm dev
```

### 2. 访问登录页面
打开浏览器访问：`http://localhost:5173`（或其他Vite分配的端口）

### 3. 登录方式

#### 方式一：演示登录
- 点击"演示登录"按钮
- 将使用预置的演示账户自动登录

#### 方式二：手动输入
- 用户名：任意字符串（如 `test`, `user1`, `admin`）
- 密码：任意字符串（不验证密码）
- 点击"登录"按钮

### 4. 可测试的界面

登录成功后可以访问：
- **聊天界面** (`/chat`) - 主聊天功能
- **历史记录** (`/history`) - 对话历史查看
- **个人资料** (`/profile`) - 用户信息管理
- **系统设置** (`/settings`) - 应用设置

## 模拟数据说明

### 用户数据
- 用户名：`demo`
- 邮箱：`demo@example.com`
- 头像：自动生成的Avatar
- Token：模拟JWT令牌

### 聊天数据
- 5个模拟对话，包含不同标题和时间
- 每个对话包含3对模拟消息（用户+助手）
- 发送消息会返回随机模拟回复

### 功能特性
- 路由守卫已启用，未登录用户会被重定向到登录页
- 模拟网络延迟（300-800ms）以提供真实体验
- 所有操作都在前端模拟，无需后端服务

## 切换回真实API

如需在开发环境中连接真实后端服务：

1. 确保后端服务正在运行（默认端口：3000）
2. 创建环境配置文件：
   ```bash
   # 复制示例文件
   cp .env.development.local.example .env.development.local
   ```
3. 编辑 `.env.development.local` 文件：
   ```env
   VITE_USE_MOCK_API=false
   VITE_API_BASE_URL=http://localhost:3000/api
   ```
4. 重启开发服务器

## 环境变量配置

### 开发环境
- `.env.development.local` - 本地开发配置（优先级最高）
- `.env.development` - 开发环境配置
- `.env.local` - 通用本地配置

### 生产环境
- `.env.production.local` - 本地生产配置
- `.env.production` - 生产环境配置

### 常用变量
- `VITE_USE_MOCK_API` - 是否使用模拟API（开发环境默认：true）
- `VITE_API_BASE_URL` - 后端API地址（默认：http://localhost:3000/api）

## 注意事项

- 模拟数据仅在开发环境下生效
- 生产环境会自动切换到真实API
- 所有模拟数据不会持久化，刷新页面后会重置
- 如需定制模拟数据，请编辑 `src/api/auth.ts` 和 `src/api/chat.ts` 文件