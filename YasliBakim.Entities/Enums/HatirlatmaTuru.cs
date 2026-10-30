using System.ComponentModel.DataAnnotations;

namespace YasliBakim.Entities.Enums;

public enum HatirlatmaTuru
{
    [Display(Name = "İlaç")] Ilac = 1,
    [Display(Name = "Randevu")] Randevu = 2,
    [Display(Name = "Su içme")] SuIcme = 3,
    [Display(Name = "Yemek")] Yemek = 4,
    [Display(Name = "Egzersiz")] Egzersiz = 5,
    [Display(Name = "Diğer")] Diger = 99
}
