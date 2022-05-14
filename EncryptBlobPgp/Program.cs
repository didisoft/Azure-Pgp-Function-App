using System;
using System.IO;

using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

using DidiSoft.Pgp;

///////////////////////////////////////////////////////////////////
//
//  Example code for PGP encrypting a blob from Azure Storage Container
//
//  (c) DidiSoft Inc, 2022 
//  https://didisoft.com/
//
///////////////////////////////////////////////////////////////////
namespace EncryptBlob
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Expected parameters <source-container> <source-blob> <destination-container> <destination-blob>");
                return;
            }

            string sourceContainer = args[0];
            string sourceBlob = args[1];
            string destinationContainer = args[2];
            string destinationBlob = args[3];

            string connectionString = "DefaultEndpointsProtocol=https;AccountName=didisoftstorage1;AccountKey=fIU8h2WHbCiP9zsz2UqBWu1N9Da3kVaE3hWkSM5eeBr1db9CwWx8haUD/5LC/cwwHdvCQ9Qp7mrg+AStrjIVeg==;EndpointSuffix=core.windows.net";
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(connectionString);

            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();

            CloudBlobContainer containerSrc = blobClient.GetContainerReference(sourceContainer);
            CloudBlob blobSrc = containerSrc.GetBlobReference(sourceBlob);

            CloudBlobContainer containerDest = blobClient.GetContainerReference(destinationContainer);
            CloudBlockBlob blobDest = containerDest.GetBlockBlobReference(destinationBlob);

            Console.WriteLine("blobDest:" + blobDest.Name);

            using (Stream input = blobSrc.OpenRead())
            using (Stream output = blobDest.OpenWrite())
            {
                PGPLib pgp = new PGPLib();

                //
                // Load public key from Azure Key Vault
                //
                string vaultName = "didisoft";
                string tenant = "didisoft";
                string clientId = "didisoftcl1";
                string clientSecret = "CiP9zsz2UqBWu1N9Da3kVaE3hWkSM5eeBr1db9CwWx";
                KeysAzureVault vault = new KeysAzureVault(vaultName, tenant, clientId, clientSecret);
                string publicKey = vault.GetPublicKey("recipient@acmcompany.com");

                bool asciiArmorOutput = false;
                pgp.EncryptStream(input, sourceBlob, publicKey, output, asciiArmorOutput);
            }

            Console.WriteLine("Encryption done.");
        }
    }
}
