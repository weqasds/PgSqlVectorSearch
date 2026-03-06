using System;
using DomainService.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataMigration.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:chat_role", "assistant,system,user")
                .Annotation("Npgsql:Enum:entity_type", "chat_message,chat_session,embedding,knowledge_base,knowledge_chunk,knowledge_document,rag_generation,rag_retrieval_log,user")
                .Annotation("Npgsql:Enum:relation_type", "contains,contradicts,depends_on,references,related_to,similar_to,supports")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:chat_role", "assistant,system,user")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_graph_node", x => x.Id);
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "graph_edge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    VisualProperties = table.Column<string>(type: "text", nullable: true),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "graph_edge");

            migrationBuilder.DropTable(
                name: "graph_node");

            migrationBuilder.DropTable(
                name: "knowledge_relation");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:chat_role", "assistant,system,user")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:Enum:chat_role", "assistant,system,user")
                .OldAnnotation("Npgsql:Enum:entity_type", "chat_message,chat_session,embedding,knowledge_base,knowledge_chunk,knowledge_document,rag_generation,rag_retrieval_log,user")
                .OldAnnotation("Npgsql:Enum:relation_type", "contains,contradicts,depends_on,references,related_to,similar_to,supports")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
