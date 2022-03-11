using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;

namespace FunctionApp1
{
    public static class Function
    {
        [FunctionName("DidisoftBlobFunction1")]
        public static void Run([BlobTrigger("data/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob,
            string name, ILogger log)
        {
            string sourceContainer = "data";
            string destinationContainer = "datapgp";
            string sourceBlob = name;
            // Replace input file name extension with .pgp
            string destinationBlob = Path.GetFileNameWithoutExtension(name) + ".pgp";

            string BatchAccountName = "...";
            string BatchAccountKey = "...";
            string BatchServiceUrl = "...";
            BatchSharedKeyCredentials credentials = new BatchSharedKeyCredentials(
                BatchServiceUrl,
                BatchAccountName,
                BatchAccountKey);

            using (BatchClient batchClient = BatchClient.Open(credentials))
            {
                string jobId = destinationBlob.Replace(".", "_");

                CloudJob unboundJob = batchClient.JobOperations.CreateJob();
                unboundJob.Id = jobId;

                ImageReference imageReference = new ImageReference(
                    publisher: "MicrosoftWindowsServer",
                    offer: "WindowsServer",
                    sku: "2019-Datacenter",
                    version: "latest");

                // Create a VM configuration
                VirtualMachineConfiguration vmConfiguration =
                    new VirtualMachineConfiguration(
                        imageReference: imageReference,
                        nodeAgentSkuId: "batch.node.windows amd64");

                string appId = "EncryptBlobPgp";
                string appVersion = "1.0";

                // For this job, ask the Batch service to automatically create a pool of VMs when the job is submitted.
                unboundJob.PoolInformation = new PoolInformation()
                {
                    AutoPoolSpecification = new AutoPoolSpecification()
                    {
                        AutoPoolIdPrefix = "EncryptBlobPgp",
                        PoolSpecification = new PoolSpecification()
                        {
                            TargetDedicatedComputeNodes = 1,
                            VirtualMachineSize = "Standard_A2_v2",
                            VirtualMachineConfiguration = vmConfiguration
                        },

                        KeepAlive = false,
                        PoolLifetimeOption = PoolLifetimeOption.Job
                    }
                };

                // Commit Job to create it in the service
                unboundJob.Commit();

                //Windows:
                //AZ_BATCH_APP_PACKAGE_NAME#2.7
                //Linux:
                //AZ_BATCH_APP_PACKAGE_name_2_7
                string appPath = String.Format("%AZ_BATCH_APP_PACKAGE_{0}#{1}%", appId.ToUpper(), appVersion);
                // create a simple task. Each task within a job must have a unique ID
                string cmdLine = String.Format("cmd /c {0}\\EncryptBlobPgp.exe {1} {2} {3} {4}",
                                                appPath,
                                                sourceContainer,
                                                sourceBlob,
                                                destinationContainer,
                                                destinationBlob);

                CloudTask cloudTask = new CloudTask("task-encrypt", cmdLine)
                {
                    ApplicationPackageReferences = new List<ApplicationPackageReference>
                    {
                        new ApplicationPackageReference
                        {
                            ApplicationId = appId,
                            Version = appVersion
                        }
                    }
                };

                batchClient.JobOperations.AddTask(jobId, cloudTask);
                log.LogTrace(String.Format("Batch {0} Started", jobId));
            }
        }
    }
}
