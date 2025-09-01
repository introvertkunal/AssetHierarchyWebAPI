using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetHierarchyWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class MigrationSignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AssetHierarchy_Name",
                table: "AssetHierarchy");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AssetHierarchy",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateTable(
                name: "AssetSignal",
                columns: table => new
                {
                    SignalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SignalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SignalType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssetNodeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetSignal", x => x.SignalId);
                    table.ForeignKey(
                        name: "FK_AssetSignal_AssetHierarchy_AssetNodeId",
                        column: x => x.AssetNodeId,
                        principalTable: "AssetHierarchy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetSignal_AssetNodeId",
                table: "AssetSignal",
                column: "AssetNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetSignal");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AssetHierarchy",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_AssetHierarchy_Name",
                table: "AssetHierarchy",
                column: "Name",
                unique: true);
        }
    }
}
