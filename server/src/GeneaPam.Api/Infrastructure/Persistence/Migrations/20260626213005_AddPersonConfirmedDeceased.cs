using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneaPam.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonConfirmedDeceased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "confirmed_deceased",
                table: "persons",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "confirmed_deceased", table: "persons");
        }
    }
}
