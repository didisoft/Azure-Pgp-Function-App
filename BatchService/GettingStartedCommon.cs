//https://github.com/Azure-Samples/azure-batch-samples/blob/master/CSharp/Common/GettingStartedCommon.cs
//Copyright (c) Microsoft Corporation

namespace DidiSoft.Batch.Samples.BatchService
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Batch;
    using Microsoft.Azure.Batch.Common;
    using Microsoft.Azure.Batch.FileStaging;
    using Constants = Microsoft.Azure.Batch.Constants;

    public static class GettingStartedCommon
    {
        /// <summary>
        /// Lists all the pools in the Batch account.
        /// </summary>
        /// <param name="batchClient">The BatchClient to use when interacting with the Batch service.</param>
        /// <returns>An asynchronous <see cref="Task"/> representing the operation.</returns>
        public static async Task PrintPoolsAsync(BatchClient batchClient)
        {
            Console.WriteLine("Listing Pools");
            Console.WriteLine("=============");

            // Using optional select clause to return only the properties of interest. Makes query faster and reduces HTTP packet size impact
            IPagedEnumerable<CloudPool> pools = batchClient.PoolOperations.ListPools(new ODATADetailLevel(selectClause: "id,state,currentDedicatedNodes,currentLowPriorityNodes,vmSize"));

            await pools.ForEachAsync(pool =>
            {
                Console.WriteLine("State of pool {0} is {1} and it has {2} dedicated nodes and {3} low-priority nodes of size {4}",
                    pool.Id,
                    pool.State,
                    pool.CurrentDedicatedComputeNodes,
                    pool.CurrentLowPriorityComputeNodes,
                    pool.VirtualMachineSize);
            }).ConfigureAwait(continueOnCapturedContext: false);
            Console.WriteLine("=============");
        }

        /// <summary>
        /// Lists all the jobs in the Batch account.
        /// </summary>
        /// <param name="batchClient">The BatchClient to use when interacting with the Batch service.</param>
        /// <returns>An asynchronous <see cref="Task"/> representing the operation.</returns>
        public static async Task PrintJobsAsync(BatchClient batchClient)
        {
            Console.WriteLine("Listing Jobs");
            Console.WriteLine("============");

            IPagedEnumerable<CloudJob> jobs = batchClient.JobOperations.ListJobs(new ODATADetailLevel(selectClause: "id,state"));
            await jobs.ForEachAsync(job =>
            {
                Console.WriteLine("State of job " + job.Id + " is " + job.State);
            }).ConfigureAwait(continueOnCapturedContext: false);

            Console.WriteLine("============");
        }

        /// <summary>
        /// Prints task information to the console for each of the nodes in the specified pool.
        /// </summary>
        /// <param name="batchClient">The Batch client.</param>
        /// <param name="poolId">The ID of the <see cref="CloudPool"/> containing the nodes whose task information should be printed to the console.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public static async Task PrintNodeTasksAsync(BatchClient batchClient, string poolId)
        {
            Console.WriteLine("Listing Node Tasks");
            Console.WriteLine("==================");

            ODATADetailLevel nodeDetail = new ODATADetailLevel(selectClause: "id,recentTasks");
            IPagedEnumerable<ComputeNode> nodes = batchClient.PoolOperations.ListComputeNodes(poolId, nodeDetail);

            await nodes.ForEachAsync(node =>
            {
                Console.WriteLine();
                Console.WriteLine(node.Id + " tasks:");

                if (node.RecentTasks != null && node.RecentTasks.Any())
                {
                    foreach (TaskInformation task in node.RecentTasks)
                    {
                        Console.WriteLine("\t{0}: {1}", task.TaskId, task.TaskState);
                    }
                }
                else
                {
                    // No tasks found for the node
                    Console.WriteLine("\tNone");
                }
            }).ConfigureAwait(continueOnCapturedContext: false);

            Console.WriteLine("==================");
        }

        /// <summary>
        /// Prints running task and task slot counts to the console for each of the nodes in the specified pool.
        /// </summary>
        /// <param name="batchClient">The Batch client.</param>
        /// <param name="poolId">The ID of the <see cref="CloudPool"/> containing the nodes whose task information should be printed to the console.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public static async Task PrintNodeTaskCountsAsync(BatchClient batchClient, string poolId)
        {
            Console.WriteLine("Listing Node Running Task Counts");
            Console.WriteLine("==================");

            ODATADetailLevel nodeDetail = new ODATADetailLevel(selectClause: "id,runningTasksCount,runningTaskSlotsCount");
            IPagedEnumerable<ComputeNode> nodes = batchClient.PoolOperations.ListComputeNodes(poolId, nodeDetail);

            await nodes.ForEachAsync(node =>
            {
                Console.WriteLine();
                Console.WriteLine(node.Id + " :");
                Console.WriteLine($"RunningTasks = {node.RunningTasksCount}, RunningTaskSlots = {node.RunningTaskSlotsCount}");

            }).ConfigureAwait(continueOnCapturedContext: false);

            Console.WriteLine("==================");
        }

        /// <summary>
        /// Prints task and task slot counts per task state for the specified job.
        /// </summary>
        /// <param name="batchClient">The Batch client.</param>
        /// <param name="poolId">The ID of the <see cref="CloudJob"/>.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public static async Task PrintJobTaskCountsAsync(BatchClient batchClient, string jobId)
        {
            Console.WriteLine("Listing Job Task Counts");
            Console.WriteLine("==================");

            TaskCountsResult result = await batchClient.JobOperations.GetJobTaskCountsAsync(jobId);

            Console.WriteLine();
            Console.WriteLine(jobId + " :");
            Console.WriteLine("\t\tActive\tRunning\tCompleted");
            Console.WriteLine($"TaskCounts:\t{result.TaskCounts.Active}\t{result.TaskCounts.Running}\t{result.TaskCounts.Completed}");
            Console.WriteLine($"TaskSlotCounts:\t{result.TaskSlotCounts.Active}\t{result.TaskSlotCounts.Running}\t{result.TaskSlotCounts.Completed}");

            Console.WriteLine("==================");
        }

        public static string CreateJobId(string prefix)
        {
            // a job is uniquely identified by its ID so your account name along with a timestamp is added as suffix
            return string.Format("{0}-{1}-{2}", prefix, new string(Environment.UserName.Where(char.IsLetterOrDigit).ToArray()), DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        }

        /// <summary>
        /// Waits for all tasks under the specified job to complete and then prints each task's output to the console.
        /// </summary>
        /// <param name="batchClient">The BatchClient to use when interacting with the Batch service.</param>
        /// <param name="tasks">The tasks to wait for.</param>
        /// <param name="timeout">The timeout.  After this time has elapsed if the job is not complete and exception will be thrown.</param>
        /// <returns>An asynchronous <see cref="Task"/> representing the operation.</returns>
        public static async Task WaitForTasksAndPrintOutputAsync(BatchClient batchClient, IEnumerable<CloudTask> tasks, TimeSpan timeout)
        {
            // We use the task state monitor to monitor the state of our tasks -- in this case we will wait for them all to complete.
            TaskStateMonitor taskStateMonitor = batchClient.Utilities.CreateTaskStateMonitor();

            // Wait until the tasks are in completed state.
            List<CloudTask> ourTasks = tasks.ToList();

            await taskStateMonitor.WhenAll(ourTasks, TaskState.Completed, timeout).ConfigureAwait(continueOnCapturedContext: false);

            // dump task output
            foreach (CloudTask t in ourTasks)
            {
                Console.WriteLine("Task {0}", t.Id);

                //Read the standard out of the task
                NodeFile standardOutFile = await t.GetNodeFileAsync(Constants.StandardOutFileName).ConfigureAwait(continueOnCapturedContext: false);
                string standardOutText = await standardOutFile.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
                Console.WriteLine("Standard out:");
                Console.WriteLine(standardOutText);

                //Read the standard error of the task
                NodeFile standardErrorFile = await t.GetNodeFileAsync(Constants.StandardErrorFileName).ConfigureAwait(continueOnCapturedContext: false);
                string standardErrorText = await standardErrorFile.ReadAsStringAsync().ConfigureAwait(continueOnCapturedContext: false);
                Console.WriteLine("Standard error:");
                Console.WriteLine(standardErrorText);

                Console.WriteLine();
            }
        }

        /// <summary>
        /// Generates a file in a temp location with the specified name and text.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="fileText">The text of the file.</param>
        /// <returns>The full path to the file.</returns>
        public static string GenerateTemporaryFile(string fileName, string fileText)
        {
            string filePath = Path.Combine(Environment.GetEnvironmentVariable("TEMP"), fileName);
            File.WriteAllText(filePath, fileText);

            return filePath;
        }
    }
}