using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace YasliBakim.Entities.Enums;

public static class EnumUzantilari
{
    // [Display(Name = "Çok kötü")] varsa onu, yoksa enum adını döndürür (ekranda "CokKotu" yazmasın).
    public static string GorunenAd(this Enum deger) =>
        deger.GetType().GetField(deger.ToString())?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? deger.ToString();
}
