using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataMigration.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "KnowledgeBaseId1",
                table: "knowledge_document",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "KnowledgeDocumentId",
                table: "knowledge_chunk",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Operation = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_document_KnowledgeBaseId1",
                table: "knowledge_document",
                column: "KnowledgeBaseId1");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_chunk_KnowledgeDocumentId",
                table: "knowledge_chunk",
                column: "KnowledgeDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_knowledge_chunk_knowledge_document_KnowledgeDocumentId",
                table: "knowledge_chunk",
                column: "KnowledgeDocumentId",
                principalTable: "knowledge_document",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_knowledge_document_knowledge_base_KnowledgeBaseId1",
                table: "knowledge_document",
                column: "KnowledgeBaseId1",
                principalTable: "knowledge_base",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_knowledge_chunk_knowledge_document_KnowledgeDocumentId",
                table: "knowledge_chunk");

            migrationBuilder.DropForeignKey(
                name: "FK_knowledge_document_knowledge_base_KnowledgeBaseId1",
                table: "knowledge_document");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropIndex(
                name: "IX_knowledge_document_KnowledgeBaseId1",
                table: "knowledge_document");

            migrationBuilder.DropIndex(
                name: "IX_knowledge_chunk_KnowledgeDocumentId",
                table: "knowledge_chunk");

            migrationBuilder.DropColumn(
                name: "KnowledgeBaseId1",
                table: "knowledge_document");

            migrationBuilder.DropColumn(
                name: "KnowledgeDocumentId",
                table: "knowledge_chunk");
        }
    }
}
