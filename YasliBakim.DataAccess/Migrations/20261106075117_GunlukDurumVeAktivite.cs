using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YasliBakim.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class GunlukDurumVeAktivite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AktiviteKaydi_Tur",
                table: "AktiviteKayitlari");

            migrationBuilder.AddColumn<int>(
                name: "AgriSeviyesi",
                table: "AktiviteKayitlari",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kategori",
                table: "AktiviteKayitlari",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KaydedenBakiciId",
                table: "AktiviteKayitlari",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SureDakika",
                table: "AktiviteKayitlari",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AktiviteKayitlari_KaydedenBakiciId",
                table: "AktiviteKayitlari",
                column: "KaydedenBakiciId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AktiviteKaydi_Agri",
                table: "AktiviteKayitlari",
                sql: "[AgriSeviyesi] IS NULL OR [AgriSeviyesi] BETWEEN 0 AND 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AktiviteKaydi_Sure",
                table: "AktiviteKayitlari",
                sql: "[SureDakika] IS NULL OR [SureDakika] BETWEEN 1 AND 480");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AktiviteKaydi_Tur",
                table: "AktiviteKayitlari",
                sql: "([Tur] = 1 AND [HatirlatmaId] IS NOT NULL AND [Durum] IS NOT NULL AND [PlanlananZaman] IS NOT NULL) OR ([Tur] = 2 AND [HatirlatmaId] IS NULL AND [RuhHali] IS NOT NULL) OR ([Tur] = 3 AND [HatirlatmaId] IS NULL AND [Kategori] IS NOT NULL AND [SureDakika] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_AktiviteKayitlari_Bakicilar_KaydedenBakiciId",
                table: "AktiviteKayitlari",
                column: "KaydedenBakiciId",
                principalTable: "Bakicilar",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AktiviteKayitlari_Bakicilar_KaydedenBakiciId",
                table: "AktiviteKayitlari");

            migrationBuilder.DropIndex(
                name: "IX_AktiviteKayitlari_KaydedenBakiciId",
                table: "AktiviteKayitlari");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AktiviteKaydi_Agri",
                table: "AktiviteKayitlari");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AktiviteKaydi_Sure",
                table: "AktiviteKayitlari");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AktiviteKaydi_Tur",
                table: "AktiviteKayitlari");

            // Eski CHECK kısıtı yalnızca Tur 1 ve 2'yi tanır; geri alırken aktivite (Tur = 3) kayıtları kaldırılmalı.
            migrationBuilder.Sql("DELETE FROM [AktiviteKayitlari] WHERE [Tur] = 3;");

            migrationBuilder.DropColumn(
                name: "AgriSeviyesi",
                table: "AktiviteKayitlari");

            migrationBuilder.DropColumn(
                name: "Kategori",
                table: "AktiviteKayitlari");

            migrationBuilder.DropColumn(
                name: "KaydedenBakiciId",
                table: "AktiviteKayitlari");

            migrationBuilder.DropColumn(
                name: "SureDakika",
                table: "AktiviteKayitlari");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AktiviteKaydi_Tur",
                table: "AktiviteKayitlari",
                sql: "([Tur] = 1 AND [HatirlatmaId] IS NOT NULL AND [Durum] IS NOT NULL AND [PlanlananZaman] IS NOT NULL) OR ([Tur] = 2 AND [HatirlatmaId] IS NULL AND [RuhHali] IS NOT NULL)");
        }
    }
}
