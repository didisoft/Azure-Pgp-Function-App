using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Configuration;

namespace DidiSoftBlobDeleteFunction
{
    public static class DidiSoftBlobDeleteFunction
    {
        [FunctionName("DidiSoftBlobDeleteFunction")]
        public static void Run([BlobTrigger("datapgp/{name}", Connection = "")]Stream myBlob, string name, ILogger log)
        {
            string BatchAccountName = "...";
            string BatchAccountKey = "...";
            string BatchServiceUrl = "...";
            BatchSharedKeyCredentials credentials = new BatchSharedKeyCredentials(
                BatchServiceUrl,
                BatchAccountName,
                BatchAccountKey);

            using (BatchClient batchClient = BatchClient.Open(credentials))
            {
                string jobId = name.Replace(".", "_");

                batchClient.PoolOperations.DeletePool("Pool"+jobId);
                batchClient.JobOperations.DeleteJob(jobId);
            }
        }
    }
}
