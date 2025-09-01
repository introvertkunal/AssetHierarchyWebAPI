using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetHierarchyWebAPI.Migrations
{
    /// <inheritdoc />
    public partial class cascadedeletedFIx : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetHierarchy_AssetHierarchy_ParentId",
                table: "AssetHierarchy");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetHierarchy_AssetHierarchy_ParentId",
                table: "AssetHierarchy",
                column: "ParentId",
                principalTable: "AssetHierarchy",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetHierarchy_AssetHierarchy_ParentId",
                table: "AssetHierarchy");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetHierarchy_AssetHierarchy_ParentId",
                table: "AssetHierarchy",
                column: "ParentId",
                principalTable: "AssetHierarchy",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
