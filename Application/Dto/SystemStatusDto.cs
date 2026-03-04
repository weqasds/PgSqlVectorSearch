namespace Application.ApplicationServiceContract;

public class SystemStatusDto
{
    public bool DatabaseConnected { get; set; }
    public bool VectorSearchAvailable { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalChunks { get; set; }
    public SystemLoad Load { get; set; } = new SystemLoad();
}

public class SystemLoad
{
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public int ActiveThreads { get; set; }
}