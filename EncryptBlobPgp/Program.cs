using System;
using System.IO;

using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

using DidiSoft.Pgp;

namespace EncryptBlob
{
    class Program
    {
        static void Main(string[] args)
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
            //string storageAccount = "didisoftstorage1";
            //string storageKey = "fIU8h2WHbCiP9zsz2UqBWu1N9Da3kVaE3hWkSM5eeBr1db9CwWx8haUD";
            //new CloudStorageAccount(storageCredentials, useHttps: true);

            CloudBlobClient blobClient = cloudStorageAccount.CreateCloudBlobClient();

            CloudBlobContainer containerSrc = blobClient.GetContainerReference(sourceContainer);
            CloudBlob blobSrc = containerSrc.GetBlobReference(sourceBlob);

            CloudBlobContainer containerDest = blobClient.GetContainerReference(destinationContainer);
            CloudBlockBlob blobDest = containerDest.GetBlockBlobReference(destinationBlob);

            Console.WriteLine("blobDest:" + blobDest.Name);

            string lineFeed = "\r\n";
            String inlinePublicKey = "-----BEGIN PGP PUBLIC KEY BLOCK-----" + lineFeed +
    "Version: GnuPG v2.1.0-ecc (GNU/Linux)" + lineFeed +
"" + lineFeed +
    "mFIETJPQrRMIKoZIzj0DAQcCAwQLx6e669XwjHTHe3HuROe7C1oYMXuZbaU5PjOs" + lineFeed +
    "xSkyxtL2D00e/jWgufuNN4ftS+6XygEtB7j1g1vnCTVF1TLmtCRlY19kc2FfZGhf" + lineFeed +
    "MjU2IDxvcGVucGdwQGJyYWluaHViLm9yZz6IegQTEwgAIgUCTJPQrQIbAwYLCQgH" + lineFeed +
    "AwIGFQgCCQoLBBYCAwECHgECF4AACgkQC6Ut8LqlnZzmXQEAiKgiSzPSpUOJcX9d" + lineFeed +
    "JtLJ5As98Alit2oFwzhxG7mSVmQA/RP67yOeoUtdsK6bwmRA95cwf9lBIusNjehx" + lineFeed +
    "XDfpHj+/uFYETJPQrRIIKoZIzj0DAQcCAwR/cMCoGEzcrqXbILqP7Rfke977dE1X" + lineFeed +
    "XsRJEwrzftreZYrn7jXSDoiXkRyfVkvjPZqUvB5cknsaoH/3UNLRHClxAwEIB4hh" + lineFeed +
    "BBgTCAAJBQJMk9CtAhsMAAoJEAulLfC6pZ2c1yYBAOSUmaQ8rkgihnepbnpK7tNz" + lineFeed +
    "3QEocsLEtsTCDUBGNYGyAQDclifYqsUChXlWKaw3md+yHJPcWZXzHt37c4q/MhIm" + lineFeed +
    "oQ==" + lineFeed +
    "=hMzp" + lineFeed +
    "-----END PGP PUBLIC KEY BLOCK-----";


            using (Stream input = blobSrc.OpenRead())
            using (Stream output = blobDest.OpenWrite())
            {
                PGPLib pgp = new PGPLib();

                bool asciiArmorOutput = false;
                pgp.EncryptStream(input, sourceBlob, new MemoryStream(System.Text.Encoding.UTF8.GetBytes(inlinePublicKey)), output, asciiArmorOutput);
            }

            Console.WriteLine("Encryption done.");
        }
    }
}
