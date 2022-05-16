using System;
using System.IO;
using System.Threading.Tasks;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using DidiSoft.Pgp;

namespace EncryptBlob
{
    /// <summary>
    /// Storage of PGP keys inside Azure Key Vault
    /// </summary>
    /// <remarks>
    /// Copyright (c) DidiSoft Inc 2022 / didisoft.com
    /// </remarks>
    /// <example>
    /// Example usage of KeysAzureVault
    /// <code lang="C#">
    /// VaultKeyStorage storage = new VaultKeyStorage();
    /// string publicKey = vault.GetPublicKey("recipient@acmcompany.com");
    /// ... use publicKey with DidiSoft.Pgp.PGPLib
    /// </code>
    /// </example>
    public class VaultKeyStorage
    {
        private string privateKeyId = "pgp_private_key"; // 
        private string keyVaultName;
        private string tenantId, clientId, clientSecret;

        /// <summary>
        /// Loads/Saves PGP keys in Azure Key Vault
        /// </summary>
        /// <param name="vault">Vault name</param>
        /// <param name="tenantId">tenant Id</param>
        /// <param name="clientId">client Id</param>
        /// <param name="clientSecret">client secret</param>
        public VaultKeyStorage(string vault, string tenantId, string clientId, string clientSecret)
        {
            this.keyVaultName = vault;
            this.tenantId = tenantId;
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        /// <summary>
        /// Loads the public key of a recipient, stored in Key Vault
        /// </summary>
        /// <param name="recipient">Recipient identifier</param>
        /// <returns>Public key in ASCII armored format</returns>
        /// <example>
        /// Example usage of KeysAzureVault
        /// <code lang="C#">
        /// VaultKeyStorage storage = new VaultKeyStorage();
        /// string publicKey = vault.GetPublicKey("recipient@acmcompany.com");
        /// ... use publicKey with DidiSoft.Pgp.PGPLib
        /// </code>
        /// </example>
        public string GetPublicKey(string recipient)
        {
            return Task.Run(() => GetPublicKeyAsync(recipient)).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Loads the public key of a recipient, stored in Key Vault
        /// </summary>
        /// <param name="recipient">Recipient identifier</param>
        /// <returns>Public key in ASCII armored format</returns>
        public async Task<string> GetPublicKeyAsync(string recipient)
        {
            var secret = await GetClient().GetSecretAsync(recipient);
            return secret.Value.Value;
        }

        /// <summary>
        /// Sabes PGP public key in ASCII armored format inside Azure Key Vault
        /// </summary>
        /// <param name="recipient">recipient Id</param>
        /// <param name="keyAscii">key in ASCII armored format</param>
        public void SavePublicKey(string recipient, string keyAscii)
        {
            Task.Run(() => SavePublicKeyAsync(recipient, keyAscii)).GetAwaiter();
        }

        /// <summary>
        /// Sabes PGP public key in ASCII armored format inside Azure Key Vault
        /// </summary>
        /// <param name="recipient">recipient Id</param>
        /// <param name="keyAscii">key in ASCII armored format</param>
        public async void SavePublicKeyAsync(string recipient, string keyAscii)
        {
            await GetClient().SetSecretAsync(recipient, keyAscii);
        }

        /// <summary>
        /// Sabes PGP public key in ASCII armored format inside Azure Key Vault
        /// </summary>
        /// <param name="recipient">recipient Id</param>
        /// <param name="keyAscii">key in ASCII armored format</param>
        public void SavePublicKey(string recipient, Stream keyStream)
        {

            Task.Run(() => SavePublicKeyAsync(recipient, keyStream)).GetAwaiter();
        }

        /// <summary>
        /// Sabes PGP public key in ASCII armored format inside Azure Key Vault
        /// </summary>
        /// <param name="recipient">recipient Id</param>
        /// <param name="keyAscii">key in ASCII armored format</param>
        public async void SavePublicKeyAsync(string recipient, Stream keyStream)
        {
            PGPKeyPair keyPair = new PGPKeyPair(keyStream);
            await GetClient().SetSecretAsync(recipient, keyPair.ExportPublicKeyAsString());
        }

        /// <summary>
        /// Loads our private key
        /// </summary>
        /// <remarks>
        /// This method is designed with the presumption that we have only one private key
        /// <br></br>
        /// In case we have more tha one private key, then it has to be modified like GetPublicKey
        /// </remarks>
        /// <returns>Private key in ASCII armored format</returns>
        public string GetPrivateKey()
        {
            return Task.Run(() => GetPrivateKeyAsync()).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Loads our private key
        /// </summary>
        /// <remarks>
        /// This method is designed with the presumption that we have only one private key
        /// <br></br>
        /// In case we have more tha one private key, then it has to be modified like GetPublicKey
        /// </remarks>
        /// <returns>Private key in ASCII armored format</returns>
        public async Task<string> GetPrivateKeyAsync()
        {
            var secret = await GetClient().GetSecretAsync(this.privateKeyId);
            return secret.Value.Value;
        }

        /// <summary>
        /// Saves PGP private key in ASCII armored format inside Azure KeyVault
        /// </summary>
        /// <remarks>
        /// This method is designed with the presumption that we have only one private key
        /// <br></br>
        /// In case we have more tha one private key, then it has to be modified like SavePublicKey
        /// </remarks>
        /// <param name="keyAscii">key in ASCII armored format</param>
        public void SavePrivateKey(string keyAscii)
        {
            Task.Run(() => SavePrivateKeyAsync(keyAscii)).GetAwaiter();
        }

        /// <summary>
        /// Saves PGP private key in ASCII armored format inside Azure KeyVault
        /// </summary>
        /// <remarks>
        /// This method is designed with the presumption that we have only one private key
        /// <br></br>
        /// In case we have more tha one private key, then it has to be modified like SavePublicKey
        /// </remarks>
        /// <param name="keyAscii">key in ASCII armored format</param>
        public void SavePrivateKey(Stream keyStream)
        {
            Task.Run(() => SavePrivateKeyAsync(keyStream)).GetAwaiter();
        }

        /// <summary>
        /// Saves PGP private key in ASCII armored format inside Azure KeyVault
        /// </summary>
        /// <remarks>
        /// This method is designed with the presumption that we have only one private key
        /// <br></br>
        /// In case we have more tha one private key, then it has to be modified like SavePublicKey
        /// </remarks>
        /// <param name="recipient">recipient Id</param>
        /// <param name="keyAscii">key in ASCII armored format</param>
        public async void SavePrivateKeyAsync(string keyAscii)
        {
            await GetClient().SetSecretAsync(this.privateKeyId, keyAscii);
        }

        /// <summary>
        /// Saves PGP private key in ASCII armored format inside Azure KeyVault
        /// </summary>
        /// <remarks>
        /// This method is designed with the presumption that we have only one private key
        /// <br></br>
        /// In case we have more tha one private key, then it has to be modified like SavePublicKey
        /// </remarks>
        /// <param name="recipient">recipient Id</param>
        /// <param name="keyAscii">key in ASCII armored format</param>
        public async void SavePrivateKeyAsync(Stream keyStream)
        {
            PGPKeyPair keyPair = new PGPKeyPair(keyStream);
            if (!keyPair.HasPrivateKey)
                throw new DidiSoft.Pgp.Exceptions.WrongPrivateKeyException("No privata key in supplied source");

            await GetClient().SetSecretAsync(this.privateKeyId, keyPair.ExportPrivateKeyAsString());
        }

        /// <summary>
        /// Deletes PGP private key from Azure Key Vault
        /// </summary>
        /// <remarks>
        /// This method is designed with the presumption that we have only one private key
        /// <br></br>
        /// In case we have more tha one private key, then it has to be modified like <see cref="SavePublicKeyAsync(string, string)"/>
        /// </remarks>
        public void DeletePrivateKey()
        {
            var operation = GetClient().GetDeletedSecret(privateKeyId);
        }

        /// <summary>
        /// Deletes PGP private key from Azure Key Vault
        /// </summary>
        /// <remarks>
        /// This method is designed with the presumption that we have only one private key
        /// <br></br>
        /// In case we have more tha one private key, then it has to be modified like <see cref="SavePublicKeyAsync(string, string)"/>
        /// </remarks>
        public async void DeletePrivateKeyAsync()
        {
            DeleteSecretOperation operation = await GetClient().StartDeleteSecretAsync(this.privateKeyId);
            await operation.WaitForCompletionAsync();
        }


        /// <summary>
        /// Deletes PGP private key from Azure Key Vault
        /// </summary>
        /// <remarks>
        /// This method is designed with the presumption that we have only one private key
        /// <br></br>
        /// In case we have more tha one private key, then it has to be modified like <see cref="SavePublicKeyAsync(string, string)"/>
        /// </remarks>
        /// <param name="recipientId">Name associated with the public key</param>
        public void DeletePublicKey(string recipientId)
        {
            var operation = GetClient().GetDeletedSecret(recipientId);
        }

        /// <summary>
        /// Deletes PGP private key from Azure Key Vault
        /// </summary>
        /// <remarks>
        /// This method is designed with the presumption that we have only one private key
        /// <br></br>
        /// In case we have more tha one private key, then it has to be modified like <see cref="SavePublicKeyAsync(string, string)"/>
        /// </remarks>
        /// <param name="recipientId">Name associated with the public key</param>
        public async void DeletePublicKeyAsync(string recipientId)
        {
            DeleteSecretOperation operation = await GetClient().StartDeleteSecretAsync(recipientId);
            await operation.WaitForCompletionAsync();
        }

        #region Helpers
        private SecretClient GetClient()
        {
            var kvUri = "https://" + keyVaultName + ".vault.azure.net";
            return new SecretClient(new Uri(kvUri), new ClientSecretCredential(tenantId, clientId, clientSecret));
        }
        #endregion
    }
}
