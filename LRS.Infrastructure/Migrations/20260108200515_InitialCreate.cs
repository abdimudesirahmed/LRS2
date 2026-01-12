using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LRS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdministrativeSourceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnglishValue = table.Column<string>(type: "text", nullable: false),
                    HarariValue = table.Column<string>(type: "text", nullable: true),
                    OromoValue = table.Column<string>(type: "text", nullable: true),
                    AmharicValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrativeSourceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    RecordationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsCreated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdministrativeSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdministrativeSourceTypeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrativeSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdministrativeSources_AdministrativeSourceTypes_Administrat~",
                        column: x => x.AdministrativeSourceTypeId,
                        principalTable: "AdministrativeSourceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppRegId = table.Column<string>(type: "text", nullable: false),
                    UniqueParcelId = table.Column<string>(type: "text", nullable: false),
                    SourceId = table.Column<int>(type: "integer", nullable: false),
                    AdministrativeSourceId = table.Column<int>(type: "integer", nullable: true),
                    DocumentName = table.Column<string>(type: "text", nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_AdministrativeSources_AdministrativeSourceId",
                        column: x => x.AdministrativeSourceId,
                        principalTable: "AdministrativeSources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Documents_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AdministrativeSourceTypes",
                columns: new[] { "Id", "AmharicValue", "EnglishValue", "HarariValue", "OromoValue" },
                values: new object[,]
                {
                    { 1, null, "Title Certificate", null, null },
                    { 2, null, "Tax Receipt", null, null },
                    { 3, null, "Injunction registration Letter", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeSources_AdministrativeSourceTypeId",
                table: "AdministrativeSources",
                column: "AdministrativeSourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_AdministrativeSourceId",
                table: "Documents",
                column: "AdministrativeSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SourceId",
                table: "Documents",
                column: "SourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "AdministrativeSources");

            migrationBuilder.DropTable(
                name: "Sources");

            migrationBuilder.DropTable(
                name: "AdministrativeSourceTypes");
        }
    }
}
