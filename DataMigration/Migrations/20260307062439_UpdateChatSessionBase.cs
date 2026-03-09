using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataMigration.Migrations
{
    /// <inheritdoc />
    public partial class UpdateChatSessionBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileHash",
                table: "knowledge_document",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "KnowledgeBaseId",
                table: "chat_session",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferencedKnowledgeBaseIds",
                table: "chat_session",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_document_FileHash",
                table: "knowledge_document",
                column: "FileHash");

            migrationBuilder.CreateIndex(
                name: "IX_chat_session_KnowledgeBaseId",
                table: "chat_session",
                column: "KnowledgeBaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_chat_session_knowledge_base_KnowledgeBaseId",
                table: "chat_session",
                column: "KnowledgeBaseId",
                principalTable: "knowledge_base",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_chat_session_knowledge_base_KnowledgeBaseId",
                table: "chat_session");

            migrationBuilder.DropIndex(
                name: "IX_knowledge_document_FileHash",
                table: "knowledge_document");

            migrationBuilder.DropIndex(
                name: "IX_chat_session_KnowledgeBaseId",
                table: "chat_session");

            migrationBuilder.DropColumn(
                name: "FileHash",
                table: "knowledge_document");

            migrationBuilder.DropColumn(
                name: "KnowledgeBaseId",
                table: "chat_session");

            migrationBuilder.DropColumn(
                name: "ReferencedKnowledgeBaseIds",
                table: "chat_session");
        }
    }
}
