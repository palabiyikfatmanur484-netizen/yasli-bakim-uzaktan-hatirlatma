using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YasliBakim.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class IlkSema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bakicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Eposta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bakicilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Yaslilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Soyad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DogumTarihi = table.Column<DateOnly>(type: "date", nullable: false),
                    Telefon = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Adres = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    KronikHastaliklar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AcilDurumNotu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Yaslilar", x => x.Id);
                    table.CheckConstraint("CK_Yasli_DogumTarihi", "[DogumTarihi] >= '1900-01-01'");
                });

            migrationBuilder.CreateTable(
                name: "Hatirlatmalar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YasliId = table.Column<int>(type: "int", nullable: false),
                    OlusturanBakiciId = table.Column<int>(type: "int", nullable: true),
                    Baslik = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tur = table.Column<int>(type: "int", nullable: false),
                    Saat = table.Column<TimeOnly>(type: "time", nullable: false),
                    Tekrar = table.Column<int>(type: "int", nullable: false),
                    BaslangicTarihi = table.Column<DateOnly>(type: "date", nullable: false),
                    BitisTarihi = table.Column<DateOnly>(type: "date", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hatirlatmalar", x => x.Id);
                    table.CheckConstraint("CK_Hatirlatma_Tarih", "[BitisTarihi] IS NULL OR [BitisTarihi] >= [BaslangicTarihi]");
                    table.ForeignKey(
                        name: "FK_Hatirlatmalar_Bakicilar_OlusturanBakiciId",
                        column: x => x.OlusturanBakiciId,
                        principalTable: "Bakicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Hatirlatmalar_Yaslilar_YasliId",
                        column: x => x.YasliId,
                        principalTable: "Yaslilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YasliBakicilar",
                columns: table => new
                {
                    YasliId = table.Column<int>(type: "int", nullable: false),
                    BakiciId = table.Column<int>(type: "int", nullable: false),
                    YakinlikDerecesi = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    BirincilSorumluMu = table.Column<bool>(type: "bit", nullable: false),
                    AtanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YasliBakicilar", x => new { x.YasliId, x.BakiciId });
                    table.ForeignKey(
                        name: "FK_YasliBakicilar_Bakicilar_BakiciId",
                        column: x => x.BakiciId,
                        principalTable: "Bakicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YasliBakicilar_Yaslilar_YasliId",
                        column: x => x.YasliId,
                        principalTable: "Yaslilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AktiviteKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YasliId = table.Column<int>(type: "int", nullable: false),
                    HatirlatmaId = table.Column<int>(type: "int", nullable: true),
                    Tur = table.Column<int>(type: "int", nullable: false),
                    KayitZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: true),
                    RuhHali = table.Column<int>(type: "int", nullable: true),
                    Not = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AktiviteKayitlari", x => x.Id);
                    table.CheckConstraint("CK_AktiviteKaydi_Tur", "([Tur] = 1 AND [HatirlatmaId] IS NOT NULL AND [Durum] IS NOT NULL) OR ([Tur] = 2 AND [HatirlatmaId] IS NULL AND [RuhHali] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_AktiviteKayitlari_Hatirlatmalar_HatirlatmaId",
                        column: x => x.HatirlatmaId,
                        principalTable: "Hatirlatmalar",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AktiviteKayitlari_Yaslilar_YasliId",
                        column: x => x.YasliId,
                        principalTable: "Yaslilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Olaylar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    YasliId = table.Column<int>(type: "int", nullable: false),
                    AktiviteKaydiId = table.Column<int>(type: "int", nullable: true),
                    IlgilenenBakiciId = table.Column<int>(type: "int", nullable: true),
                    Tur = table.Column<int>(type: "int", nullable: false),
                    Seviye = table.Column<int>(type: "int", nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OlusmaZamani = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    CozulmeZamani = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Olaylar", x => x.Id);
                    table.CheckConstraint("CK_Olay_CozulmeZamani", "[CozulmeZamani] IS NULL OR [CozulmeZamani] >= [OlusmaZamani]");
                    table.ForeignKey(
                        name: "FK_Olaylar_AktiviteKayitlari_AktiviteKaydiId",
                        column: x => x.AktiviteKaydiId,
                        principalTable: "AktiviteKayitlari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Olaylar_Bakicilar_IlgilenenBakiciId",
                        column: x => x.IlgilenenBakiciId,
                        principalTable: "Bakicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Olaylar_Yaslilar_YasliId",
                        column: x => x.YasliId,
                        principalTable: "Yaslilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AktiviteKaydi_Yasli_Zaman",
                table: "AktiviteKayitlari",
                columns: new[] { "YasliId", "KayitZamani" });

            migrationBuilder.CreateIndex(
                name: "IX_AktiviteKayitlari_HatirlatmaId",
                table: "AktiviteKayitlari",
                column: "HatirlatmaId");

            migrationBuilder.CreateIndex(
                name: "UX_Bakici_Eposta",
                table: "Bakicilar",
                column: "Eposta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Hatirlatma_Yasli_Aktif_Saat",
                table: "Hatirlatmalar",
                columns: new[] { "YasliId", "AktifMi", "Saat" });

            migrationBuilder.CreateIndex(
                name: "IX_Hatirlatmalar_OlusturanBakiciId",
                table: "Hatirlatmalar",
                column: "OlusturanBakiciId");

            migrationBuilder.CreateIndex(
                name: "IX_Olay_Durum_Seviye",
                table: "Olaylar",
                columns: new[] { "Durum", "Seviye" });

            migrationBuilder.CreateIndex(
                name: "IX_Olay_Yasli_Zaman",
                table: "Olaylar",
                columns: new[] { "YasliId", "OlusmaZamani" });

            migrationBuilder.CreateIndex(
                name: "IX_Olaylar_AktiviteKaydiId",
                table: "Olaylar",
                column: "AktiviteKaydiId",
                unique: true,
                filter: "[AktiviteKaydiId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Olaylar_IlgilenenBakiciId",
                table: "Olaylar",
                column: "IlgilenenBakiciId");

            migrationBuilder.CreateIndex(
                name: "IX_YasliBakici_BakiciId",
                table: "YasliBakicilar",
                column: "BakiciId");

            migrationBuilder.CreateIndex(
                name: "IX_Yasli_Soyad_Ad",
                table: "Yaslilar",
                columns: new[] { "Soyad", "Ad" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Olaylar");

            migrationBuilder.DropTable(
                name: "YasliBakicilar");

            migrationBuilder.DropTable(
                name: "AktiviteKayitlari");

            migrationBuilder.DropTable(
                name: "Hatirlatmalar");

            migrationBuilder.DropTable(
                name: "Bakicilar");

            migrationBuilder.DropTable(
                name: "Yaslilar");
        }
    }
}
