using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataMigration.Migrations
{
    /// <inheritdoc />
    public partial class UpdateKnowledgeDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:chat_role", "assistant,system,user")
                .Annotation("Npgsql:Enum:document_status", "failed,pending,processed,processing")
                .Annotation("Npgsql:Enum:entity_type", "chat_message,chat_session,embedding,knowledge_base,knowledge_chunk,knowledge_document,rag_generation,rag_retrieval_log,user")
                .Annotation("Npgsql:Enum:relation_type", "contains,contradicts,depends_on,references,related_to,similar_to,supports")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:chat_role", "assistant,system,user")
                .OldAnnotation("Npgsql:Enum:entity_type", "chat_message,chat_session,embedding,knowledge_base,knowledge_chunk,knowledge_document,rag_generation,rag_retrieval_log,user")
                .OldAnnotation("Npgsql:Enum:relation_type", "contains,contradicts,depends_on,references,related_to,similar_to,supports")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "knowledge_document",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "ChunkCount",
                table: "knowledge_document",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "knowledge_document",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "knowledge_document",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "knowledge_document",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DocumentStatus>(
                name: "Status",
                table: "knowledge_document",
                type: "document_status",
                nullable: false,
                defaultValue: DocumentStatus.Pending);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChunkCount",
                table: "knowledge_document");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "knowledge_document");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "knowledge_document");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "knowledge_document");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "knowledge_document");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:chat_role", "assistant,system,user")
                .Annotation("Npgsql:Enum:entity_type", "chat_message,chat_session,embedding,knowledge_base,knowledge_chunk,knowledge_document,rag_generation,rag_retrieval_log,user")
                .Annotation("Npgsql:Enum:relation_type", "contains,contradicts,depends_on,references,related_to,similar_to,supports")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:chat_role", "assistant,system,user")
                .OldAnnotation("Npgsql:Enum:document_status", "failed,pending,processed,processing")
                .OldAnnotation("Npgsql:Enum:entity_type", "chat_message,chat_session,embedding,knowledge_base,knowledge_chunk,knowledge_document,rag_generation,rag_retrieval_log,user")
                .OldAnnotation("Npgsql:Enum:relation_type", "contains,contradicts,depends_on,references,related_to,similar_to,supports")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "knowledge_document",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
