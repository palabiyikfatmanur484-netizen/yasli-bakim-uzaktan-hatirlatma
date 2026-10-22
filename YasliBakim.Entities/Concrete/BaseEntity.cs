namespace YasliBakim.Entities.Concrete;

/// <summary>
/// Tüm varlıkların ortak alanları (birincil anahtar ve kayıt zamanı).
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
