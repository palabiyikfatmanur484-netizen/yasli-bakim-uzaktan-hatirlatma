namespace YasliBakim.Business.Guvenlik;

public interface IParolaHasher
{
    string Hashle(string parola);
    bool Dogrula(string parola, string kayitliHash);
}
