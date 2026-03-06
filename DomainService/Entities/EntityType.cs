namespace DomainService.Entities
{
    /// <summary>
    /// 实体类型枚举
    /// </summary>
    public enum EntityType
    {
        /// <summary>
        /// 知识块
        /// </summary>
        KnowledgeChunk,

        /// <summary>
        /// 知识文档
        /// </summary>
        KnowledgeDocument,

        /// <summary>
        /// 知识库
        /// </summary>
        KnowledgeBase,

        /// <summary>
        /// 用户
        /// </summary>
        User,

        /// <summary>
        /// 对话会话
        /// </summary>
        ChatSession,

        /// <summary>
        /// 对话消息
        /// </summary>
        ChatMessage,

        /// <summary>
        /// 向量嵌入
        /// </summary>
        Embedding,

        /// <summary>
        /// RAG生成记录
        /// </summary>
        RagGeneration,

        /// <summary>
        /// RAG检索日志
        /// </summary>
        RagRetrievalLog
    }
}