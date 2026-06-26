using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeneaPam.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RearchitectFiliationChildParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Breaking re-architecture (ADR 0005): the Filiation goes from a child→couple link
            // to an independent child→parent edge. There is no meaningful data-preserving
            // transform of the old (child, couple) rows into (child, parent) rows, so the table
            // is dropped and recreated in its new shape.
            migrationBuilder.DropTable(name: "filiations");

            // Couple person FKs flip Restrict → Cascade (deleting a Person removes any Couple
            // they were in, per ADR 0005 decision #7).
            migrationBuilder.DropForeignKey(
                name: "FK_couples_persons_person_a_id",
                table: "couples"
            );
            migrationBuilder.DropForeignKey(
                name: "FK_couples_persons_person_b_id",
                table: "couples"
            );
            migrationBuilder.AddForeignKey(
                name: "FK_couples_persons_person_a_id",
                table: "couples",
                column: "person_a_id",
                principalTable: "persons",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );
            migrationBuilder.AddForeignKey(
                name: "FK_couples_persons_person_b_id",
                table: "couples",
                column: "person_b_id",
                principalTable: "persons",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.CreateTable(
                name: "filiations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tree_id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parentage_type = table.Column<string>(type: "text", nullable: false),
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
                    table.CheckConstraint(
                        "ck_filiations_no_self_parent",
                        "child_person_id <> parent_person_id"
                    );
                    table.CheckConstraint(
                        "ck_filiations_parentage_type",
                        "parentage_type IN ('Biological','Adoptive','Step','Foster')"
                    );
                    table.ForeignKey(
                        name: "FK_filiations_persons_child_person_id",
                        column: x => x.child_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_filiations_persons_parent_person_id",
                        column: x => x.parent_person_id,
                        principalTable: "persons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_filiations_trees_tree_id",
                        column: x => x.tree_id,
                        principalTable: "trees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_filiations_parent_person_id",
                table: "filiations",
                column: "parent_person_id"
            );
            migrationBuilder.CreateIndex(
                name: "IX_filiations_tree_id",
                table: "filiations",
                column: "tree_id"
            );
            migrationBuilder.CreateIndex(
                name: "ux_filiations_child_parent",
                table: "filiations",
                columns: new[] { "child_person_id", "parent_person_id" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "filiations");

            migrationBuilder.DropForeignKey(
                name: "FK_couples_persons_person_a_id",
                table: "couples"
            );
            migrationBuilder.DropForeignKey(
                name: "FK_couples_persons_person_b_id",
                table: "couples"
            );
            migrationBuilder.AddForeignKey(
                name: "FK_couples_persons_person_a_id",
                table: "couples",
                column: "person_a_id",
                principalTable: "persons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
            );
            migrationBuilder.AddForeignKey(
                name: "FK_couples_persons_person_b_id",
                table: "couples",
                column: "person_b_id",
                principalTable: "persons",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict
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
                name: "IX_filiations_couple_id",
                table: "filiations",
                column: "couple_id"
            );
        }
    }
}
