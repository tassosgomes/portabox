using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortaBox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBlocoAndUnidade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bloco",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condominio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    inativado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inativado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bloco", x => x.id);
                    table.ForeignKey(
                        name: "fk_bloco_asp_net_users_criado_por",
                        column: x => x.criado_por,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bloco_asp_net_users_inativado_por",
                        column: x => x.inativado_por,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bloco_condominios_condominio_id",
                        column: x => x.condominio_id,
                        principalTable: "condominio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unidade",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    bloco_id = table.Column<Guid>(type: "uuid", nullable: false),
                    andar = table.Column<int>(type: "integer", nullable: false),
                    numero = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<Guid>(type: "uuid", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    inativado_em = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    inativado_por = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_unidade", x => x.id);
                    table.CheckConstraint("ck_unidade_andar_non_negative", "andar >= 0");
                    table.ForeignKey(
                        name: "fk_unidade_asp_net_users_criado_por",
                        column: x => x.criado_por,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unidade_asp_net_users_inativado_por",
                        column: x => x.inativado_por,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_unidade_bloco_bloco_id",
                        column: x => x.bloco_id,
                        principalTable: "bloco",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_bloco_condominio",
                table: "bloco",
                column: "condominio_id");

            migrationBuilder.CreateIndex(
                name: "idx_bloco_nome_ativo_unique",
                table: "bloco",
                columns: new[] { "tenant_id", "condominio_id", "nome" },
                unique: true,
                filter: "ativo = true");

            migrationBuilder.CreateIndex(
                name: "ix_bloco_criado_por",
                table: "bloco",
                column: "criado_por");

            migrationBuilder.CreateIndex(
                name: "ix_bloco_inativado_por",
                table: "bloco",
                column: "inativado_por");

            migrationBuilder.CreateIndex(
                name: "idx_unidade_bloco",
                table: "unidade",
                column: "bloco_id");

            migrationBuilder.CreateIndex(
                name: "idx_unidade_canonica_ativa",
                table: "unidade",
                columns: new[] { "tenant_id", "bloco_id", "andar", "numero" },
                unique: true,
                filter: "ativo = true");

            migrationBuilder.CreateIndex(
                name: "ix_unidade_criado_por",
                table: "unidade",
                column: "criado_por");

            migrationBuilder.CreateIndex(
                name: "ix_unidade_inativado_por",
                table: "unidade",
                column: "inativado_por");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "unidade");

            migrationBuilder.DropTable(
                name: "bloco");
        }
    }
}
