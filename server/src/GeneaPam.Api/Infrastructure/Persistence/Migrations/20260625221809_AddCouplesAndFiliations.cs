using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneaPam.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCouplesAndFiliations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "couples",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tree_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_a_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_b_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_couples", x => x.id);
                    table.ForeignKey(
                        name: "FK_couples_persons_person_a_id",
                        column: x => x.person_a_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_couples_persons_person_b_id",
                        column: x => x.person_b_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_couples_trees_tree_id",
                        column: x => x.tree_id,
                        principalTable: "trees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "filiations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    couple_id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_person_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_filiations", x => x.id);
                    table.ForeignKey(
                        name: "FK_filiations_couples_couple_id",
                        column: x => x.couple_id,
                        principalTable: "couples",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_filiations_persons_child_person_id",
                        column: x => x.child_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_couples_person_a_id",
                table: "couples",
                column: "person_a_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_couples_person_b_id",
                table: "couples",
                column: "person_b_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_couples_tree_id",
                table: "couples",
                column: "tree_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_filiations_child_person_id",
                table: "filiations",
                column: "child_person_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_filiations_couple_id",
                table: "filiations",
                column: "couple_id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "filiations");

            migrationBuilder.DropTable(name: "couples");
        }
    }
}
