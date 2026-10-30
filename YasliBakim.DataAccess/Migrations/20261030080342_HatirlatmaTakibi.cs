using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YasliBakim.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class HatirlatmaTakibi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AktiviteKayitlari_HatirlatmaId",
                table: "AktiviteKayitlari");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AktiviteKaydi_Tur",
                table: "AktiviteKayitlari");

            migrationBuilder.AddColumn<DateTime>(
                name: "PlanlananZaman",
                table: "AktiviteKayitlari",
                type: "datetime2",
                nullable: true);

            // Elle eklendi: mevcut onay kayıtlarında planlanan zaman yok. Kayıt gününe hatırlatmanın saati
            // eklenerek doldurulur; aksi hâlde aşağıdaki yeni CHECK kısıtı eski satırlar yüzünden eklenemez.
            migrationBuilder.Sql("""
                UPDATE a
                SET a.PlanlananZaman = DATEADD(SECOND, DATEDIFF(SECOND, CAST('00:00' AS time), h.Saat),
                                               CAST(CAST(a.KayitZamani AS date) AS datetime2))
                FROM AktiviteKayitlari a
                JOIN Hatirlatmalar h ON h.Id = a.HatirlatmaId
                WHERE a.Tur = 1 AND a.PlanlananZaman IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "UX_AktiviteKaydi_Hatirlatma_Planlanan",
                table: "AktiviteKayitlari",
                columns: new[] { "HatirlatmaId", "PlanlananZaman" },
                unique: true,
                filter: "[HatirlatmaId] IS NOT NULL AND [PlanlananZaman] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AktiviteKaydi_Tur",
                table: "AktiviteKayitlari",
                sql: "([Tur] = 1 AND [HatirlatmaId] IS NOT NULL AND [Durum] IS NOT NULL AND [PlanlananZaman] IS NOT NULL) OR ([Tur] = 2 AND [HatirlatmaId] IS NULL AND [RuhHali] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_AktiviteKaydi_Hatirlatma_Planlanan",
                table: "AktiviteKayitlari");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AktiviteKaydi_Tur",
                table: "AktiviteKayitlari");

            migrationBuilder.DropColumn(
                name: "PlanlananZaman",
                table: "AktiviteKayitlari");

            migrationBuilder.CreateIndex(
                name: "IX_AktiviteKayitlari_HatirlatmaId",
                table: "AktiviteKayitlari",
                column: "HatirlatmaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AktiviteKaydi_Tur",
                table: "AktiviteKayitlari",
                sql: "([Tur] = 1 AND [HatirlatmaId] IS NOT NULL AND [Durum] IS NOT NULL) OR ([Tur] = 2 AND [HatirlatmaId] IS NULL AND [RuhHali] IS NOT NULL)");
        }
    }
}
