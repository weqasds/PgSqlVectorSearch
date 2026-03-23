@echo off
REM RAG Document Parser 环境设置脚本（Windows）
echo 设置 RAG Document Parser 开发环境...

REM 检查Python
python --version >nul 2>&1
if errorlevel 1 (
    echo 错误: 未找到Python。请确保Python已安装并添加到PATH。
    pause
    exit /b 1
)

echo 1. 创建虚拟环境...
if not exist ".venv" (
    python -m venv .venv
    echo   虚拟环境创建成功
) else (
    echo   虚拟环境已存在，跳过...
)

echo 2. 激活虚拟环境...
call .venv\Scripts\activate.bat

echo 3. 升级pip...
python -m pip install --upgrade pip

echo 4. 安装项目依赖...
pip install -e ".[dev]"

if errorlevel 1 (
    echo 错误: 依赖安装失败。
    pause
    exit /b 1
)

echo 5. 配置环境文件...
if not exist ".env" (
    if exist ".env.example" (
        copy .env.example .env >nul
        echo   已创建 .env 文件（从 .env.example）
        echo   请编辑 .env 文件配置参数
    ) else (
        echo   警告: 未找到 .env.example 文件
    )
) else (
    echo   .env 文件已存在，跳过...
)

echo 6. 创建必要目录...
if not exist "temp" mkdir temp
if not exist "logs" mkdir logs
if not exist "testdata" mkdir testdata

echo.
echo 环境设置完成！
echo.
echo 可用命令:
echo   scripts\run_service.bat    - 启动开发服务器
echo   python -m pytest           - 运行测试
echo   python -m rag_document_parser - 运行服务
echo.
echo 开发说明:
echo   1. 编辑 .env 文件配置服务参数
echo   2. 运行 scripts\run_service.bat 启动服务
echo   3. 访问 http://localhost:8000/docs 查看API文档
echo.
pause