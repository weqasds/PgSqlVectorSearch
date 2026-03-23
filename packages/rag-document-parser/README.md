# RAG Document Parser

一个无状态、高性能的文档解析服务，专注于PDF解析、旧版Office转换和OCR补充识别。易于C#调用，支持同步和异步处理。

## 功能特性

- 📄 **PDF解析**：使用PyMuPDF和pdfplumber进行高性能PDF解析
- 🏢 **Office文档转换**：支持旧版Office文档（.doc, .xls, .ppt）转换
- 🔍 **OCR补充识别**：使用Tesseract OCR进行扫描文档识别
- ⚡ **高性能**：异步处理，内存优化，流式处理
- 🔧 **易于集成**：RESTful API，JSON格式，易于C#调用
- 📊 **监控指标**：Prometheus指标，健康检查，结构化日志

## 快速开始

### 安装依赖

```bash
# 创建虚拟环境
python -m venv .venv

# 激活虚拟环境（Windows）
.venv\Scripts\activate

# 安装依赖
pip install -e ".[dev]"
```

### 运行服务

```bash
# 开发模式
uvicorn rag_document_parser.api.app:app --reload --port 8000

# 或使用脚本
python -m rag_document_parser
```

### 使用示例

```bash
# 健康检查
curl http://localhost:8000/health

# 解析文档
curl -X POST -F "file=@document.pdf" http://localhost:8000/parse
```

## API文档

服务启动后访问：
- Swagger UI: http://localhost:8000/docs
- ReDoc: http://localhost:8000/redoc

## 配置

通过环境变量配置服务：

```bash
# 服务配置
PORT=8000
HOST=0.0.0.0
LOG_LEVEL=INFO

# 文件处理
MAX_UPLOAD_SIZE=104857600  # 100MB

# 解析配置
PDF_ENGINE=pymupdf
ENABLE_OCR=true
OCR_THRESHOLD=50
```

详细配置见`.env.example`文件。

## 开发

### 项目结构

```
src/rag_document_parser/
├── api/           # API层（FastAPI应用）
├── services/      # 服务层（业务逻辑）
├── engines/       # 引擎层（解析引擎）
├── models/        # 数据模型
└── utils/         # 工具函数
```

### 运行测试

```bash
# 运行所有测试
pytest

# 运行特定测试
pytest tests/test_api/

# 带覆盖率
pytest --cov=rag_document_parser
```

## 依赖

### 核心依赖
- FastAPI - Web框架
- PyMuPDF - PDF解析
- pdfplumber - 表格提取
- pytesseract - OCR识别
- python-docx/openpyxl/python-pptx - Office文档处理

### 系统依赖
- Tesseract OCR 5.x+
- LibreOffice 7.0+ (headless模式)

## 许可证

MIT