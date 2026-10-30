using System.ComponentModel.DataAnnotations;

namespace YasliBakim.Entities.Enums;

public enum TekrarTipi
{
    [Display(Name = "Tek seferlik")] Tek = 1,
    [Display(Name = "Her gün")] Gunluk = 2,
    [Display(Name = "Her hafta")] Haftalik = 3,
    [Display(Name = "Her ay")] Aylik = 4
}
