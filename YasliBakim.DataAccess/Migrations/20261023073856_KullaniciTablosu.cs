using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YasliBakim.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class KullaniciTablosu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Eposta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParolaHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    BakiciId = table.Column<int>(type: "int", nullable: true),
                    YasliId = table.Column<int>(type: "int", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    BasarisizGirisSayisi = table.Column<int>(type: "int", nullable: false),
                    KilitBitisZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SonGirisZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                    table.CheckConstraint("CK_Kullanici_Rol", "([Rol] = 1 AND [BakiciId] IS NULL AND [YasliId] IS NULL) OR ([Rol] = 2 AND [BakiciId] IS NOT NULL AND [YasliId] IS NULL) OR ([Rol] = 3 AND [YasliId] IS NOT NULL AND [BakiciId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_Kullanicilar_Bakicilar_BakiciId",
                        column: x => x.BakiciId,
                        principalTable: "Bakicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Kullanicilar_Yaslilar_YasliId",
                        column: x => x.YasliId,
                        principalTable: "Yaslilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_BakiciId",
                table: "Kullanicilar",
                column: "BakiciId",
                unique: true,
                filter: "[BakiciId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_YasliId",
                table: "Kullanicilar",
                column: "YasliId",
                unique: true,
                filter: "[YasliId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Kullanici_Eposta",
                table: "Kullanicilar",
                column: "Eposta",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Kullanicilar");
        }
    }
}
