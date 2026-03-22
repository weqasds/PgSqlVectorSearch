using System;
using System.Text.Json;
using DomainService.Entities.Chat;
using DomainService.Entities.Enums;
using DomainService.Entities.KnowledgeManagement;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace DataMigration.Migrations
{
    /// <inheritdoc />
    public partial class Create : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:chat_role", "assistant,system,user")
                .Annotation("Npgsql:Enum:document_status", "failed,pending,processed,processing")
                .Annotation("Npgsql:Enum:entity_type", "chat_message,chat_session,embedding,knowledge_base,knowledge_chunk,knowledge_document,rag_generation,rag_retrieval_log,user")
                .Annotation("Npgsql:Enum:file_type", "excel,html,image,json,markdown,pdf,power_point,text,unknown,word,xml,zip")
                .Annotation("Npgsql:Enum:relation_type", "contains,contradicts,depends_on,references,related_to,similar_to,supports")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: false),
                    TableName = table.Column<string>(type: "text", nullable: true),
                    EntityId = table.Column<string>(type: "text", nullable: true),
                    EntityName = table.Column<string>(type: "text", nullable: true),
                    OldValues = table.Column<JsonElement>(type: "jsonb", nullable: true),
                    NewValues = table.Column<JsonElement>(type: "jsonb", nullable: true),
                    AffectedColumns = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "graph_node",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<EntityType>(type: "entity_type", nullable: false),
                    PositionX = table.Column<float>(type: "real", nullable: false, defaultValue: 0f),
                    PositionY = table.Column<float>(type: "real", nullable: false, defaultValue: 0f),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_graph_node", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_base",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_base", x => x.Id);
                    table.ForeignKey(
                        name: "FK_knowledge_base_user_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "chat_session",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatTitle = table.Column<string>(type: "text", nullable: false),
                    KnowledgeBaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferencedKnowledgeBaseIds = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_session", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_session_knowledge_base_KnowledgeBaseId",
                        column: x => x.KnowledgeBaseId,
                        principalTable: "knowledge_base",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_chat_session_user_UserId",
                        column: x => x.UserId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeBaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<FileType>(type: "file_type", nullable: false),
                    Status = table.Column<DocumentStatus>(type: "document_status", nullable: false, defaultValue: DocumentStatus.Pending),
                    ChunkCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_document", x => x.Id);
                    table.ForeignKey(
                        name: "FK_knowledge_document_knowledge_base_KnowledgeBaseId",
                        column: x => x.KnowledgeBaseId,
                        principalTable: "knowledge_base",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_knowledge_base",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeBaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_knowledge_base", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_knowledge_base_knowledge_base_KnowledgeBaseId",
                        column: x => x.KnowledgeBaseId,
                        principalTable: "knowledge_base",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_knowledge_base_user_UserId",
                        column: x => x.UserId,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_message",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<ChatRole>(type: "chat_role", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_message", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_message_chat_session_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "chat_session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rag_generation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rag_generation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rag_generation_chat_session_ChatSessionId",
                        column: x => x.ChatSessionId,
                        principalTable: "chat_session",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_chunk",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_chunk", x => x.Id);
                    table.ForeignKey(
                        name: "FK_knowledge_chunk_knowledge_document_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "knowledge_document",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "embedding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Vector = table.Column<Vector>(type: "vector(1024)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_embedding", x => x.Id);
                    table.ForeignKey(
                        name: "FK_embedding_knowledge_chunk_ChunkId",
                        column: x => x.ChunkId,
                        principalTable: "knowledge_chunk",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_relation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationType = table.Column<RelationType>(type: "relation_type", nullable: false),
                    Strength = table.Column<float>(type: "real", nullable: false, defaultValue: 1f),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_relation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_knowledge_relation_knowledge_chunk_SourceChunkId",
                        column: x => x.SourceChunkId,
                        principalTable: "knowledge_chunk",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_knowledge_relation_knowledge_chunk_TargetChunkId",
                        column: x => x.TargetChunkId,
                        principalTable: "knowledge_chunk",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_knowledge_relation_user_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "rag_retrieval_log",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RagGenerationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rag_retrieval_log", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rag_retrieval_log_knowledge_chunk_ChunkId",
                        column: x => x.ChunkId,
                        principalTable: "knowledge_chunk",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rag_retrieval_log_rag_generation_RagGenerationId",
                        column: x => x.RagGenerationId,
                        principalTable: "rag_generation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "graph_edge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisualProperties = table.Column<string>(type: "text", nullable: true),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_graph_edge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_graph_edge_graph_node_SourceNodeId",
                        column: x => x.SourceNodeId,
                        principalTable: "graph_node",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_graph_edge_graph_node_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "graph_node",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_graph_edge_knowledge_relation_RelationId",
                        column: x => x.RelationId,
                        principalTable: "knowledge_relation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_message_ChatSessionId",
                table: "chat_message",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_message_CreatedAt",
                table: "chat_message",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chat_session_CreatedAt",
                table: "chat_session",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_chat_session_KnowledgeBaseId",
                table: "chat_session",
                column: "KnowledgeBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_session_UserId",
                table: "chat_session",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_embedding_ChunkId",
                table: "embedding",
                column: "ChunkId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_embedding_Vector",
                table: "embedding",
                column: "Vector")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_graph_edge_RelationId",
                table: "graph_edge",
                column: "RelationId");

            migrationBuilder.CreateIndex(
                name: "IX_graph_edge_SourceNodeId",
                table: "graph_edge",
                column: "SourceNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_graph_edge_SourceNodeId_TargetNodeId",
                table: "graph_edge",
                columns: new[] { "SourceNodeId", "TargetNodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_graph_edge_TargetNodeId",
                table: "graph_edge",
                column: "TargetNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_graph_node_EntityId_EntityType",
                table: "graph_node",
                columns: new[] { "EntityId", "EntityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_graph_node_EntityType",
                table: "graph_node",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_base_IsPublic",
                table: "knowledge_base",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_base_Name",
                table: "knowledge_base",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_base_OwnerId",
                table: "knowledge_base",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_chunk_DocumentId",
                table: "knowledge_chunk",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_chunk_DocumentId_ChunkIndex",
                table: "knowledge_chunk",
                columns: new[] { "DocumentId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_document_FileHash",
                table: "knowledge_document",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_document_KnowledgeBaseId",
                table: "knowledge_document",
                column: "KnowledgeBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_relation_CreatedBy",
                table: "knowledge_relation",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_relation_RelationType",
                table: "knowledge_relation",
                column: "RelationType");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_relation_SourceChunkId",
                table: "knowledge_relation",
                column: "SourceChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_relation_SourceChunkId_TargetChunkId_RelationType",
                table: "knowledge_relation",
                columns: new[] { "SourceChunkId", "TargetChunkId", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_relation_TargetChunkId",
                table: "knowledge_relation",
                column: "TargetChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_rag_generation_ChatSessionId",
                table: "rag_generation",
                column: "ChatSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_rag_retrieval_log_ChunkId",
                table: "rag_retrieval_log",
                column: "ChunkId");

            migrationBuilder.CreateIndex(
                name: "IX_rag_retrieval_log_RagGenerationId",
                table: "rag_retrieval_log",
                column: "RagGenerationId");

            migrationBuilder.CreateIndex(
                name: "IX_user_knowledge_base_AccessLevel",
                table: "user_knowledge_base",
                column: "AccessLevel");

            migrationBuilder.CreateIndex(
                name: "IX_user_knowledge_base_KnowledgeBaseId",
                table: "user_knowledge_base",
                column: "KnowledgeBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_user_knowledge_base_UserId",
                table: "user_knowledge_base",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_knowledge_base_UserId_KnowledgeBaseId",
                table: "user_knowledge_base",
                columns: new[] { "UserId", "KnowledgeBaseId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "chat_message");

            migrationBuilder.DropTable(
                name: "embedding");

            migrationBuilder.DropTable(
                name: "graph_edge");

            migrationBuilder.DropTable(
                name: "rag_retrieval_log");

            migrationBuilder.DropTable(
                name: "user_knowledge_base");

            migrationBuilder.DropTable(
                name: "graph_node");

            migrationBuilder.DropTable(
                name: "knowledge_relation");

            migrationBuilder.DropTable(
                name: "rag_generation");

            migrationBuilder.DropTable(
                name: "knowledge_chunk");

            migrationBuilder.DropTable(
                name: "chat_session");

            migrationBuilder.DropTable(
                name: "knowledge_document");

            migrationBuilder.DropTable(
                name: "knowledge_base");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
