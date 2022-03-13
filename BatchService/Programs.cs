///////////////////////////////////////////////////////////////////
//
//  Azure Batch Service that invokes EncryptBlobPgp
//
//  (c) DidiSoft Inc, 2022 
//  https://didisoft.com/
//
///////////////////////////////////////////////////////////////////
namespace DidiSoft.Batch.Samples.BatchService
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Auth;
    using Microsoft.Azure.Batch.Common;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// The main program of the HelloWorld sample
    /// </summary>
    public static class Program
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

            try
            {
                AccountSettings accountSettings = SampleHelpers.LoadAccountSettings();

                EncryptBlobAsync(accountSettings, 
                                sourceContainer,
                                sourceBlob,
                                destinationContainer,
                                destinationBlob).Wait();
            }
            catch (AggregateException aggregateException)
            {
                // Go through all exceptions and dump useful information
                foreach (Exception exception in aggregateException.InnerExceptions)
                {
                    Console.WriteLine(exception.ToString());
                    Console.WriteLine();
                }

                throw;
            }
        }

        /// <summary>
        /// Submits a job to the Azure Batch service, and waits for it to complete
        /// </summary>
        private static async Task EncryptBlobAsync(AccountSettings accountSettings, 
                                                string sourceContainer,
                                                string sourceBlob,
                                                string destinationContainer,
                                                string destinationBlob)
        {
            // Set up the Batch Service credentials used to authenticate with the Batch Service.
            BatchSharedKeyCredentials credentials = new BatchSharedKeyCredentials(
                accountSettings.BatchServiceUrl,
                accountSettings.BatchAccountName,
                accountSettings.BatchAccountKey);

            Console.WriteLine(accountSettings.BatchServiceUrl);
            Console.WriteLine(accountSettings.BatchAccountName);
            Console.WriteLine(accountSettings.BatchAccountKey);

            // Get an instance of the BatchClient for a given Azure Batch account.
            using (BatchClient batchClient = BatchClient.Open(credentials))
            {
                // add a retry policy. The built-in policies are No Retry (default), Linear Retry, and Exponential Retry
                //batchClient.CustomBehaviors.Add(RetryPolicyProvider.ExponentialRetryProvider(TimeSpan.FromSeconds(5), 3));

                string jobId = destinationBlob.Replace(".", "_");

                try
                {
                    // Submit the job
                    await SubmitJobAsync(batchClient, jobId,
                                            sourceContainer,
                                            sourceBlob,
                                            destinationContainer,
                                            destinationBlob);

                    // Wait for the job to complete
                    await WaitForJobAndPrintOutputAsync(batchClient, jobId);
                }
                finally
                {
                    // Delete the job to ensure the tasks are cleaned up
                    if (!string.IsNullOrEmpty(jobId))
                    {
                        Console.WriteLine("Exiting job: {0}", jobId);
                        await batchClient.PoolOperations.DeletePoolAsync("Pool"+jobId);
                        await batchClient.JobOperations.DeleteJobAsync(jobId);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a job and adds a task to it.
        /// </summary>
        /// <param name="batchClient">The BatchClient to use when interacting with the Batch service.</param>
        /// <param name="configurationSettings">The configuration settings</param>
        /// <param name="jobId">The ID of the job.</param>
        /// <returns>An asynchronous <see cref="Task"/> representing the operation.</returns>
        private static async Task SubmitJobAsync(BatchClient batchClient, string jobId,
                                                string sourceContainer,
                                                string sourceBlob,
                                                string destinationContainer,
                                                string destinationBlob)
        {
            // create an empty unbound Job
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
            //AZ_BATCH_APP_PACKAGE_BLENDER#2.7
            //Linux:
            //AZ_BATCH_APP_PACKAGE_blender_2_7
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

            await batchClient.JobOperations.AddTaskAsync(jobId, cloudTask);
        }

        /// <summary>
        /// Waits for all tasks under the specified job to complete and then prints each task's output to the console.
        /// </summary>
        /// <param name="batchClient">The BatchClient to use when interacting with the Batch service.</param>
        /// <param name="jobId">The ID of the job.</param>
        /// <returns>An asynchronous <see cref="Task"/> representing the operation.</returns>
        private static async Task WaitForJobAndPrintOutputAsync(BatchClient batchClient, string jobId)
        {
            Console.WriteLine("Waiting for all tasks to complete on job: {0} ...", jobId);

            // We use the task state monitor to monitor the state of our tasks -- in this case we will wait for them all to complete.
            TaskStateMonitor taskStateMonitor = batchClient.Utilities.CreateTaskStateMonitor();

            List<CloudTask> ourTasks = await batchClient.JobOperations.ListTasks(jobId).ToListAsync();

            // Wait for all tasks to reach the completed state.
            // If the pool is being resized then enough time is needed for the nodes to reach the idle state in order
            // for tasks to run on them.
            await taskStateMonitor.WhenAll(ourTasks, TaskState.Completed, TimeSpan.FromMinutes(10));

            // dump task output
            foreach (CloudTask t in ourTasks)
            {
                Console.WriteLine("Task {0}", t.Id);

                //Read the standard out of the task
                NodeFile standardOutFile = await t.GetNodeFileAsync(Constants.StandardOutFileName);
                string standardOutText = await standardOutFile.ReadAsStringAsync();
                Console.WriteLine("Standard out:");
                Console.WriteLine(standardOutText);

                Console.WriteLine();
            }
        }
    }
}