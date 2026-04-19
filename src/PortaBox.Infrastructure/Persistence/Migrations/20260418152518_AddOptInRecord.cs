using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortaBox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOptInRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "opt_in_record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data_assembleia = table.Column<DateOnly>(type: "date", nullable: false),
                    quorum_descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    signatario_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    signatario_cpf = table.Column<string>(type: "character(11)", fixedLength: true, nullable: false),
                    data_termo = table.Column<DateOnly>(type: "date", nullable: false),
                    registered_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_opt_in_record", x => x.id);
                    table.ForeignKey(
                        name: "fk_opt_in_record_asp_net_users_registered_by_user_id",
                        column: x => x.registered_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_opt_in_record_condominio_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "condominio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_opt_in_record_tenant_id_unique",
                table: "opt_in_record",
                column: "tenant_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_opt_in_record_registered_by_user_id",
                table: "opt_in_record",
                column: "registered_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "opt_in_record");
        }
    }
}
