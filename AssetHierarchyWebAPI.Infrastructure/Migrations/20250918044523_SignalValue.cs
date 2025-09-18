using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetHierarchyWebAPI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SignalValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignalValues",
                columns: table => new
                {
                    ValueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SignalValueData = table.Column<double>(type: "float", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    SignalId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalValues", x => x.ValueId);
                    table.ForeignKey(
                        name: "FK_SignalValues_AssetSignal_SignalId",
                        column: x => x.SignalId,
                        principalTable: "AssetSignal",
                        principalColumn: "SignalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignalValues_SignalId",
                table: "SignalValues",
                column: "SignalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignalValues");
        }
    }
}
