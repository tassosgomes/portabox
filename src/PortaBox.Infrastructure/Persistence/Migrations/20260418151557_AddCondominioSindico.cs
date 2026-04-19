using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortaBox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCondominioSindico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "condominio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_fantasia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cnpj = table.Column<string>(type: "character(14)", fixedLength: true, nullable: false),
                    endereco_logradouro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    endereco_numero = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    endereco_complemento = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    endereco_bairro = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    endereco_cidade = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    endereco_uf = table.Column<string>(type: "character(2)", fixedLength: true, nullable: true),
                    endereco_cep = table.Column<string>(type: "character(8)", fixedLength: true, nullable: true),
                    administradora_nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    activated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_condominio", x => x.id);
                    table.ForeignKey(
                        name: "fk_condominio_asp_net_users_activated_by_user_id",
                        column: x => x.activated_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_condominio_asp_net_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sindico",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_completo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    celular_e164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sindico", x => x.id);
                    table.ForeignKey(
                        name: "fk_sindico_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_sindico_condominio_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "condominio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_users_sindico_tenant_id",
                table: "asp_net_users",
                column: "sindico_tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_condominio_cnpj_unique",
                table: "condominio",
                column: "cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_condominio_status",
                table: "condominio",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_condominio_activated_by_user_id",
                table: "condominio",
                column: "activated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_condominio_created_by_user_id",
                table: "condominio",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "idx_sindico_tenant_id",
                table: "sindico",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_sindico_user_id",
                table: "sindico",
                column: "user_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_asp_net_users_condominios_sindico_tenant_id",
                table: "asp_net_users",
                column: "sindico_tenant_id",
                principalTable: "condominio",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_asp_net_users_condominios_sindico_tenant_id",
                table: "asp_net_users");

            migrationBuilder.DropTable(
                name: "sindico");

            migrationBuilder.DropTable(
                name: "condominio");

            migrationBuilder.DropIndex(
                name: "ix_asp_net_users_sindico_tenant_id",
                table: "asp_net_users");
        }
    }
}
