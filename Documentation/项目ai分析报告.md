# ✅ 当前项目结构下的 AI 相关代码：整体情况（DDD 视角）

**更新日期**: 2026-03-09

目前项目在 **领域层（DomainService）** 定义了多套 AI 相关接口（契约），在 **基础设施层（Infrastructure/SemanticKernelAI）** 提供了具体实现。这种 “接口在领域层，具体实现放在基础设施层” 的分层思路符合 **DDD 依赖倒置原则**（高层依赖抽象，低层实现抽象）。

### 🧩 主要 AI 相关接口/实现分布（目前已存在）

- **领域层（DomainService）**

  - 接口（契约）在 CrossCutting：
    - `IEmbeddingService`
    - `ILLMService`
    - `ISemanticKernelService`
    - `IPromptTemplateService`
    - `IVectorStore`（向量存储服务接口）
  - 现有业务服务（如 `VectorRetrievalService`）依赖了 `IEmbeddingService` 和 `IVectorStore`。
- **基础设施层（Infrastructure/SemanticKernelAI）**

  - 实现类：
    - `OpenAIEmbeddingService : IEmbeddingService`
    - `OpenAILLMService : ILLMService`
    - `SemanticKernelService : ISemanticKernelService`
    - `EmbeddingRepository : IVectorStore`（基于pgvector的向量存储实现）
  - 实现基于 **Microsoft.SemanticKernel** + OpenAI。

> ✅ 这一层次划分在 “接口在域层、实现放基础设施层” 是合理的；但当前“完成度/调用链”还不完整（见下面「存在的问题」）。

---

## ⚠️ 目前结构/实现中的主要问题（影响合理性）

### 1) 设计上存在重复/混淆的职责边界

- `SemanticKernelService` 其实在做「嵌入 + 聊天」双职责（类似把 `IEmbeddingService` + `ILLMService` 合在一起），但项目中又有分别的服务（`OpenAIEmbeddingService`、`OpenAILLMService`），目前角色不清晰：
  - 是 `SemanticKernelService` 作为“统一入口”？
  - 还是作为可选功能？

### 2) 仓储接口重构已改善向量存储职责分离

- ✅ **向量存储职责清晰化**：引入`IVectorStore`接口，将向量存储和相似性搜索职责从领域层分离，`EmbeddingRepository`作为基础设施服务实现此接口
- ✅ **仓储接口精简**：移除非聚合根仓储接口（`IEmbeddingRepository`、`IChatMessageRepository`、`IKnowledgeChunkRepository`），遵循DDD聚合根原则
- ⚠️ **服务层代码更新**：需要更新服务层代码，使用聚合根仓储或`IVectorStore`访问非聚合根实体

---

## ✅ 修改建议（让 AI 相关部分更符合 DDD）

**部分建议已实施**：以下建议中的部分内容已通过仓储接口重构和IVectorStore引入得到改善。其他建议仍需实施。

### 1) 让业务层真正调用 AI 实现（把 stub 逻辑替换为接口调用）

重点改造位置：RagGenerationService.cs

建议改造方向（伪代码思路）：

- 注入：`ILLMService` + `IPromptTemplateService`
- 生成 Prompt：`promptTemplateService.BuildRagPromptAsync(...)`
- 调用 LLM：`llmService.GenerateResponseAsync(prompt, options)`
- 额外：可用 `IEmbeddingService`计算 token /用 `OptimizeContextAsync`控制上下文长度

> 这样 `RagGenerationService` 只依赖抽象接口，业务层不需要知道是 OpenAI 还是 Azure、也不需要直接调用 Semantic Kernel。

---

### 2) 建议统一 AI 服务层职责（清晰分工）

**更新**：向量存储职责已通过`IVectorStore`接口分离，`EmbeddingRepository`作为基础设施服务提供pgvector支持。AI服务层职责分配仍需明确。

当前存在三个服务实现（OpenAIEmbedding / OpenAILLM / SemanticKernelService），我建议选择一种清晰的路线：

选项 A：

- 保持 `IEmbeddingService` + `ILLMService` 独立；
- `SemanticKernelService` 退为“可选工具/工厂”或者移除（避免重复）。

选项 B：

- 把 `SemanticKernelService` 作为“高阶封装”，它内部使用 `IEmbeddingService` + `ILLMService`；
- 然后业务层只依赖 `ISemanticKernelService`，从上层封装获取“嵌入 + 生成 + 模型列表”等。

---

### 4) 减少领域层对第三方库耦合（可选但推荐）

当前 DomainService 直接使用 `Pgvector.Vector`，建议：

- 定义**领域专用向量类型**（比如 `DomainService.Entities.Vector` 或 `DomainService.ValueObjects.Vector`）
- 让 Infrastructure 负责把它映射成 `Pgvector.Vector`（或其它向量类型）
- 这样领域层不依赖 `Pgvector` 包，领域模型更纯粹，基础设施可替换性更高。

---

### 5) 完善 AI 相关配置 & 错误/限流策略（实用建议）

- 用配置（`appsettings.json`）控制模型、温度、token 限制，避免硬编码。
- 强化 AI 调用的**重试/限流**（尤其是 OpenAI/语义内核会出现超时/限流问题）。
- 让 `IEmbeddingService` 或 `ILLMService` 提供“可插入缓存（embedding cache）”能力，减少重复调用。

---

## ✅ 总结（结论式定位）

| 状态                    | 结论                                      |
| ----------------------- | ----------------------------------------- |
| 接口定义（Domain）      | ✅ 符合 DDD 层次：抽象放领域              |
| 实现放置（Infra）       | ✅ 放在基础设施层是合理的                 |
| 运行时调用（DI / 使用） | ❌ 未完成（主要缺失 DI 和业务层调用）     |
| 领域层耦合性            | ⚠️ 存在对 `Pgvector` 直接依赖，可优化 |

---

如果你希望，我可以继续帮你 **列出 Program.cs 需要补哪些注册 + 具体改造 RagGenerationService 的注入/调用结构（包含示例代码）**，让整个 AI 业务线能真正跑起来。
