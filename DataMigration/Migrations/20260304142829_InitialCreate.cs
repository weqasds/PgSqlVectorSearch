using System;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace DataMigration.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:chat_role", "assistant,system,user")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "knowledge_base",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_base", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeBaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "chat_session",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatTitle = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_session", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_session_user_UserId",
                        column: x => x.UserId,
                        principalTable: "user",
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "chat_message",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChatSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<ChatRole>(type: "chat_role", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "embedding",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Vector = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "rag_retrieval_log",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RagGenerationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "IX_chat_session_UserId",
                table: "chat_session",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_embedding_ChunkId",
                table: "embedding",
                column: "ChunkId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_base_Name",
                table: "knowledge_base",
                column: "Name",
                unique: true);

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
                name: "IX_knowledge_document_KnowledgeBaseId",
                table: "knowledge_document",
                column: "KnowledgeBaseId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_message");

            migrationBuilder.DropTable(
                name: "embedding");

            migrationBuilder.DropTable(
                name: "rag_retrieval_log");

            migrationBuilder.DropTable(
                name: "knowledge_chunk");

            migrationBuilder.DropTable(
                name: "rag_generation");

            migrationBuilder.DropTable(
                name: "knowledge_document");

            migrationBuilder.DropTable(
                name: "chat_session");

            migrationBuilder.DropTable(
                name: "knowledge_base");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
