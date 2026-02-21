using System.Diagnostics;
using Windows.Security.Credentials;

namespace AppNotify.Services;

public sealed class CredentialService
{
    private const string Resource = "AppNotify_AppStoreConnect";
    private const string IssuerKey = "IssuerID";
    private const string KeyIdKey = "KeyID";
    private const string PrivateKeyKey = "PrivateKey";

    private readonly PasswordVault _vault = new();

    public void SaveCredentials(string issuerID, string keyID, string privateKey)
    {
        Debug.WriteLine("[Creds] Saving credentials");
        DeleteCredentials();
        _vault.Add(new PasswordCredential(Resource, IssuerKey, issuerID));
        _vault.Add(new PasswordCredential(Resource, KeyIdKey, keyID));
        _vault.Add(new PasswordCredential(Resource, PrivateKeyKey, privateKey));
        Debug.WriteLine("[Creds] Credentials saved");
    }

    public (string IssuerID, string KeyID, string PrivateKey)? GetCredentials()
    {
        try
        {
            var creds = _vault.FindAllByResource(Resource);
            Debug.WriteLine($"[Creds] Found {creds.Count} credential entries");
            string? issuer = null, keyId = null, privateKey = null;

            foreach (var cred in creds)
            {
                cred.RetrievePassword();
                switch (cred.UserName)
                {
                    case IssuerKey: issuer = cred.Password; break;
                    case KeyIdKey: keyId = cred.Password; break;
                    case PrivateKeyKey: privateKey = cred.Password; break;
                }
            }

            if (issuer is not null && keyId is not null && privateKey is not null)
            {
                Debug.WriteLine("[Creds] All credentials retrieved successfully");
                return (issuer, keyId, privateKey);
            }
            Debug.WriteLine("[Creds] Incomplete credentials found");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Creds] GetCredentials exception: {ex.Message}");
        }

        return null;
    }

    public void DeleteCredentials()
    {
        try
        {
            var creds = _vault.FindAllByResource(Resource);
            foreach (var cred in creds)
                _vault.Remove(cred);
        }
        catch
        {
            // Nothing to delete
        }
    }
}
