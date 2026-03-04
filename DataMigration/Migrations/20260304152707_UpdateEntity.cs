using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataMigration.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "rag_retrieval_log",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "rag_generation",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "knowledge_document",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "knowledge_chunk",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "knowledge_base",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "embedding",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "chat_session",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                table: "chat_message",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "user");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "rag_retrieval_log");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "rag_generation");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "knowledge_document");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "knowledge_chunk");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "knowledge_base");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "embedding");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "chat_session");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "chat_message");
        }
    }
}
