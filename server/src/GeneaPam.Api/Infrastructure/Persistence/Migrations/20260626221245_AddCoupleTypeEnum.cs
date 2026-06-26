using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneaPam.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoupleTypeEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The old hardcoded literal 'Partner' is not a member of the CoupleType set; map any
            // existing rows to the neutral default 'Partners' so the new CHECK can be applied.
            migrationBuilder.Sql("UPDATE couples SET type = 'Partners' WHERE type = 'Partner';");

            migrationBuilder.AddCheckConstraint(
                name: "ck_couples_type",
                table: "couples",
                sql: "type IN ('Married','Partners','Separated','Divorced','Other')"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "ck_couples_type", table: "couples");
        }
    }
}
