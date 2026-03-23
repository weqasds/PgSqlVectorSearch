@echo off
REM RAG Document Parser 依赖安装脚本（Windows）
echo 正在安装 RAG Document Parser 依赖...

REM 检查Python是否可用
python --version >nul 2>&1
if errorlevel 1 (
    echo 错误: 未找到Python。请确保Python已安装并添加到PATH。
    pause
    exit /b 1
)

REM 创建虚拟环境
if not exist ".venv" (
    echo 创建虚拟环境...
    python -m venv .venv
) else (
    echo 虚拟环境已存在，跳过创建...
)

REM 激活虚拟环境
echo 激活虚拟环境...
call .venv\Scripts\activate.bat

REM 升级pip
echo 升级pip...
python -m pip install --upgrade pip

REM 安装依赖
echo 安装依赖...
pip install -e ".[dev]"

if errorlevel 1 (
    echo 错误: 依赖安装失败。
    pause
    exit /b 1
)

echo.
echo 依赖安装完成！
echo.
echo 下一步:
echo 1. 复制 .env.example 为 .env 并修改配置
echo 2. 运行 scripts\run_service.bat 启动服务
echo 3. 访问 http://localhost:8000/docs 查看API文档
echo.
pause