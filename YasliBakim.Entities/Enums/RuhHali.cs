using System.ComponentModel.DataAnnotations;

namespace YasliBakim.Entities.Enums;

public enum RuhHali
{
    [Display(Name = "Çok iyi")] CokIyi = 1,
    [Display(Name = "İyi")] Iyi = 2,
    [Display(Name = "Normal")] Normal = 3,
    [Display(Name = "Kötü")] Kotu = 4,
    [Display(Name = "Çok kötü")] CokKotu = 5
}
