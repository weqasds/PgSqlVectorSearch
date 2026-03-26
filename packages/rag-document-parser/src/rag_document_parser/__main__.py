"""RAG Document Parser 命令行入口点"""
import uvicorn

from rag_document_parser.services.config import settings


def main():
    """主函数"""
    print(f"启动 RAG Document Parser v{__import__('rag_document_parser').__version__}")
    print(f"监听地址: {settings.HOST}:{settings.PORT}")
    print(f"文档地址: http://{settings.HOST}:{settings.PORT}/docs")

    uvicorn.run(
        "rag_document_parser.api.app:app",
        host=settings.HOST,
        port=settings.PORT,
        reload=settings.RELOAD,
        workers=settings.WORKERS,
        log_level=settings.LOG_LEVEL.lower(),
    )


if __name__ == "__main__":
    main()
