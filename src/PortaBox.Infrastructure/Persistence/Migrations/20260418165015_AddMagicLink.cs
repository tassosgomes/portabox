using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortaBox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMagicLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "magic_link",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purpose = table.Column<short>(type: "smallint", nullable: false),
                    token_hash = table.Column<string>(type: "character(64)", fixedLength: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consumed_by_ip = table.Column<IPAddress>(type: "inet", nullable: true),
                    invalidated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_magic_link", x => x.id);
                    table.ForeignKey(
                        name: "fk_magic_link_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_magic_link_token_hash_unique",
                table: "magic_link",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_magic_link_user_purpose_open",
                table: "magic_link",
                columns: new[] { "user_id", "purpose" },
                filter: "consumed_at IS NULL AND invalidated_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "magic_link");
        }
    }
}
