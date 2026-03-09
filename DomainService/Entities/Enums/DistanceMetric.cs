namespace DomainService.Entities.Enums
{
    /// <summary>
    /// 向量距离度量标准枚举
    /// </summary>
    public enum DistanceMetric
    {
        /// <summary>
        /// 欧几里得距离 (L2距离)
        /// </summary>
        L2,

        /// <summary>
        /// 余弦距离
        /// </summary>
        Cosine,

        /// <summary>
        /// 内积
        /// </summary>
        InnerProduct
    }
}