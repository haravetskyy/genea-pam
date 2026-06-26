using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneaPam.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePersonFactStubWithFactTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "person_facts");

            migrationBuilder.DropColumn(name: "birth_date", table: "persons");

            migrationBuilder.DropColumn(name: "birth_date_precision", table: "persons");

            migrationBuilder.DropColumn(name: "death_date", table: "persons");

            migrationBuilder.DropColumn(name: "death_date_precision", table: "persons");

            migrationBuilder.CreateTable(
                name: "facts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tree_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    custom_label = table.Column<string>(type: "text", nullable: true),
                    owner_person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_couple_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date_value = table.Column<DateOnly>(type: "date", nullable: true),
                    precision = table.Column<string>(type: "text", nullable: true),
                    place_text = table.Column<string>(type: "text", nullable: true),
                    lat = table.Column<double>(type: "double precision", nullable: true),
                    lng = table.Column<double>(type: "double precision", nullable: true),
                    text_value = table.Column<string>(type: "text", nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_by = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_facts", x => x.id);
                    table.CheckConstraint(
                        "ck_facts_exactly_one_owner",
                        "(owner_person_id IS NULL) <> (owner_couple_id IS NULL)"
                    );
                    table.CheckConstraint(
                        "ck_facts_precision",
                        "precision IS NULL OR precision IN ('FullDate','MonthYear','YearOnly','Approximate')"
                    );
                    table.CheckConstraint(
                        "ck_facts_type",
                        "type IN ('Birth','Death','Marriage','Separation','Divorce','Occupation','Nationality','Religion','Other')"
                    );
                    table.ForeignKey(
                        name: "FK_facts_couples_owner_couple_id",
                        column: x => x.owner_couple_id,
                        principalTable: "couples",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_facts_persons_owner_person_id",
                        column: x => x.owner_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_facts_trees_tree_id",
                        column: x => x.tree_id,
                        principalTable: "trees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_facts_owner_couple_id",
                table: "facts",
                column: "owner_couple_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_facts_owner_person_id",
                table: "facts",
                column: "owner_person_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_facts_tree_id",
                table: "facts",
                column: "tree_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "facts");

            migrationBuilder.AddColumn<DateOnly>(
                name: "birth_date",
                table: "persons",
                type: "date",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "birth_date_precision",
                table: "persons",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<DateOnly>(
                name: "death_date",
                table: "persons",
                type: "date",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "death_date_precision",
                table: "persons",
                type: "text",
                nullable: true
            );

            migrationBuilder.CreateTable(
                name: "person_facts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tree_id = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_person_facts", x => x.id);
                    table.ForeignKey(
                        name: "FK_person_facts_trees_tree_id",
                        column: x => x.tree_id,
                        principalTable: "trees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_person_facts_tree_id",
                table: "person_facts",
                column: "tree_id"
            );
        }
    }
}
