@echo off
REM RAG Document Parser 服务启动脚本（Windows）
echo 启动 RAG Document Parser 服务...

REM 检查虚拟环境
if not exist ".venv" (
    echo 错误: 未找到虚拟环境。请先运行 scripts\install_deps.bat
    pause
    exit /b 1
)

REM 激活虚拟环境
echo 激活虚拟环境...
call .venv\Scripts\activate.bat

REM 检查.env文件
if not exist ".env" (
    echo 警告: 未找到 .env 配置文件
    echo 正在从 .env.example 创建 .env...
    copy .env.example .env >nul
    echo 请编辑 .env 文件并配置相应参数
    timeout /t 5
)

REM 设置环境变量
set PYTHONPATH=src

REM 运行服务
echo 启动服务...
echo 访问地址: http://localhost:%PORT% (默认8000)
echo API文档: http://localhost:%PORT%/docs
echo.
echo 按 Ctrl+C 停止服务
echo.

python -m rag_document_parser

REM 如果服务退出，暂停显示错误信息
if errorlevel 1 (
    echo.
    echo 服务异常退出，退出代码: %errorlevel%
    pause
)