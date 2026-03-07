namespace DomainService.Entities.Enums
{
    /// <summary>
    /// 关系类型枚举
    /// </summary>
    public enum RelationType
    {
        /// <summary>
        /// 相似
        /// </summary>
        SimilarTo,

        /// <summary>
        /// 引用
        /// </summary>
        References,

        /// <summary>
        /// 包含
        /// </summary>
        Contains,

        /// <summary>
        /// 对立
        /// </summary>
        Contradicts,

        /// <summary>
        /// 支持
        /// </summary>
        Supports,

        /// <summary>
        /// 依赖
        /// </summary>
        DependsOn,

        /// <summary>
        /// 相关
        /// </summary>
        RelatedTo
    }
}