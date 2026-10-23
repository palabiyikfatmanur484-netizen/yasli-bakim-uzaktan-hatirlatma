using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using YasliBakim.DataAccess.Context;

namespace YasliBakim.Tests;

/// <summary>
/// Her test için bellekte (in-memory) boş bir SQLite veritabanı açar.
/// Şema aynı DbContext modelinden üretildiği için FK, CHECK ve UNIQUE kısıtları burada da geçerlidir.
/// </summary>
public sealed class TestVeritabani : IDisposable
{
    private readonly SqliteConnection _baglanti;
    public YasliBakimDbContext Context { get; }

    public TestVeritabani()
    {
        _baglanti = new SqliteConnection("DataSource=:memory:");
        _baglanti.Open();

        var options = new DbContextOptionsBuilder<YasliBakimDbContext>().UseSqlite(_baglanti).Options;
        Context = new YasliBakimDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _baglanti.Dispose();
    }
}
