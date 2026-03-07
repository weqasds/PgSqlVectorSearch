using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataMigration.Migrations
{
    /// <inheritdoc />
    public partial class UpdateKnowledgeBaseUserPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "knowledge_base",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "knowledge_base",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_knowledge_base",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeBaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessLevel = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_base_IsPublic",
                table: "knowledge_base",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_base_OwnerId",
                table: "knowledge_base",
                column: "OwnerId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_knowledge_base_user_OwnerId",
                table: "knowledge_base",
                column: "OwnerId",
                principalTable: "user",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_knowledge_base_user_OwnerId",
                table: "knowledge_base");

            migrationBuilder.DropTable(
                name: "user_knowledge_base");

            migrationBuilder.DropIndex(
                name: "IX_knowledge_base_IsPublic",
                table: "knowledge_base");

            migrationBuilder.DropIndex(
                name: "IX_knowledge_base_OwnerId",
                table: "knowledge_base");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "knowledge_base");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "knowledge_base");
        }
    }
}
