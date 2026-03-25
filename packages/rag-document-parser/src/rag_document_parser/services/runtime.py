"""运行时共享服务实例。"""
from rag_document_parser.services.parser_service import ParserService
from rag_document_parser.services.task_service import TaskService

parser_service = ParserService()
task_service = TaskService()


def get_parser_service() -> ParserService:
    """获取共享解析服务。"""
    return parser_service


def get_task_service() -> TaskService:
    """获取共享任务服务。"""
    return task_service
