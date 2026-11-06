using System.ComponentModel.DataAnnotations;

namespace YasliBakim.Entities.Enums;

public enum AktiviteKategorisi
{
    [Display(Name = "Yürüyüş")] Yuruyus = 1,
    [Display(Name = "Egzersiz")] Egzersiz = 2,
    [Display(Name = "Sosyal (ziyaret, telefon)")] Sosyal = 3,
    [Display(Name = "Ev işi")] EvIsi = 4,
    [Display(Name = "Hobi")] Hobi = 5,
    [Display(Name = "Diğer")] Diger = 99
}
