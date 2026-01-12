using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LRS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceDocumentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_AdministrativeSources_AdministrativeSourceId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Sources_SourceId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_AdministrativeSourceId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "AdministrativeSourceId",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "OromoValue",
                table: "AdministrativeSourceTypes",
                newName: "TigrinyaValue");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RecordationDate",
                table: "Sources",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<bool>(
                name: "Status",
                table: "Sources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmissionDate",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<int>(
                name: "SourceId",
                table: "Documents",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "AlfDocumentId",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoid",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OromifaValue",
                table: "AdministrativeSourceTypes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApplicationId",
                table: "AdministrativeSources",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BaUnitId",
                table: "AdministrativeSources",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceId",
                table: "AdministrativeSources",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "AdministrativeSourceTypes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AmharicValue", "OromifaValue" },
                values: new object[] { "የይዞታ ማረጋገጫ ሰርተፊኬት", "Waraqaa ragaa eenyummaa mirkaneessu" });

            migrationBuilder.UpdateData(
                table: "AdministrativeSourceTypes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AmharicValue", "OromifaValue" },
                values: new object[] { "የመሬት ግብር ክፍያ ደረሰኝ", "Waraqaa Kaffaltii Gibiraa" });

            migrationBuilder.UpdateData(
                table: "AdministrativeSourceTypes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "AmharicValue", "OromifaValue" },
                values: new object[] { "የእግድ ደብዳቤ", "Xalayaa Dhoorkii Mana Murtii" });

            migrationBuilder.InsertData(
                table: "AdministrativeSourceTypes",
                columns: new[] { "Id", "AmharicValue", "EnglishValue", "HarariValue", "OromifaValue", "TigrinyaValue" },
                values: new object[,]
                {
                    { 4, "የእግድ ስረዛ ደብዳቤ", "Injunction release Letter", null, "Xalayaa Dhoorkii Mana Murtii Kaasu", null },
                    { 5, "የዕዳ ምዝገባ ደብዳቤ", "Mortgage registration letter", null, "Xalayaa Dhoorkii Baankii", null },
                    { 6, "የዕዳ ስረዛ ደብዳቤ", "Mortgage registration release", null, "Xalayaa Dhoorkii Baankii Kaasu", null },
                    { 7, "የከፊል ሽያጭ ውል ሰነድ", "Partial Sale Contract", null, "Galmee Waliigaltee Walakkaa Gurgurtaa", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdministrativeSources_SourceId",
                table: "AdministrativeSources",
                column: "SourceId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdministrativeSources_Sources_SourceId",
                table: "AdministrativeSources",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Sources_SourceId",
                table: "Documents",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdministrativeSources_Sources_SourceId",
                table: "AdministrativeSources");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Sources_SourceId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_AdministrativeSources_SourceId",
                table: "AdministrativeSources");

            migrationBuilder.DeleteData(
                table: "AdministrativeSourceTypes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "AdministrativeSourceTypes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "AdministrativeSourceTypes",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "AdministrativeSourceTypes",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Sources");

            migrationBuilder.DropColumn(
                name: "AlfDocumentId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsVoid",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "OromifaValue",
                table: "AdministrativeSourceTypes");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "AdministrativeSources");

            migrationBuilder.DropColumn(
                name: "BaUnitId",
                table: "AdministrativeSources");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "AdministrativeSources");

            migrationBuilder.RenameColumn(
                name: "TigrinyaValue",
                table: "AdministrativeSourceTypes",
                newName: "OromoValue");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RecordationDate",
                table: "Sources",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Sources",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmissionDate",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SourceId",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdministrativeSourceId",
                table: "Documents",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AdministrativeSourceTypes",
                keyColumn: "Id",
                keyValue: 1,
                column: "AmharicValue",
                value: null);

            migrationBuilder.UpdateData(
                table: "AdministrativeSourceTypes",
                keyColumn: "Id",
                keyValue: 2,
                column: "AmharicValue",
                value: null);

            migrationBuilder.UpdateData(
                table: "AdministrativeSourceTypes",
                keyColumn: "Id",
                keyValue: 3,
                column: "AmharicValue",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_AdministrativeSourceId",
                table: "Documents",
                column: "AdministrativeSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_AdministrativeSources_AdministrativeSourceId",
                table: "Documents",
                column: "AdministrativeSourceId",
                principalTable: "AdministrativeSources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Sources_SourceId",
                table: "Documents",
                column: "SourceId",
                principalTable: "Sources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
