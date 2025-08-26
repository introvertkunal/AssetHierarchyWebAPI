using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetHierarchyWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class MigrationV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetHierarchy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetHierarchy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetHierarchy_AssetHierarchy_ParentId",
                        column: x => x.ParentId,
                        principalTable: "AssetHierarchy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetHierarchy_Name",
                table: "AssetHierarchy",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetHierarchy_ParentId",
                table: "AssetHierarchy",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetHierarchy");
        }
    }
}
