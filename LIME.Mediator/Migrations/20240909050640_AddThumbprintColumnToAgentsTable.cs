using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LIME.Mediator.Migrations
{
    /// <inheritdoc />
    public partial class AddThumbprintColumnToAgentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "thumbprint",
                table: "agents",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "thumbprint",
                table: "agents");
        }
    }
}
