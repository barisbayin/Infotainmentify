namespace Application.Abstractions
{
    public interface ISecretStore
    {
        string Protect(string plain);
        string Unprotect(string cipher);
    }
}
