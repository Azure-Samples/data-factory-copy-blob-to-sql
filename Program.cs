using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Rest;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.DataFactory;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace ADFv2Tutorial
{
    class Program
    {
        static void Main(string[] args)
        {
            // Set variables
            string tenantID = "<tenant ID>";
            string applicationId = "<Activity directory application ID>";
            string authenticationKey = "<Activity directory application authentication key>";
            string subscriptionId = "<subscription ID>";
            string resourceGroup = "<resource group name>";

            // Note that the data stores (Azure Storage, Azure SQL Database, etc.) and computes (HDInsight, etc.) used by data factory can be in other regions.
            string region = "East US";
            string dataFactoryName = "<name of the data factory>"; //must be globally unique

            // Specify the source Azure Blob information
            string storageAccount = "<name of Azure Storage account>";
            string storageKey = "<key for your Azure Storage account>";
            string inputBlobPath = "adfv2tutorial/";
            string inputBlobName = "inputEmp.txt";

            // Specify the sink Azure SQL Database information
            string azureSqlConnString = "Server=tcp:<name of Azure SQL Server>.database.windows.net,1433;Database=spsqldb;User ID=spelluru;Password=Sowmya123;Trusted_Connection=False;Encrypt=True;Connection Timeout=30";
            string azureSqlTableName = "dbo.emp";

            string storageLinkedServiceName = "AzureStorageLinkedService";
            string sqlDbLinkedServiceName = "AzureSqlDbLinkedService";
            string blobDatasetName = "BlobDataset";
            string sqlDatasetName = "SqlDataset";
            string pipelineName = "Adfv2TutorialBlobToSqlCopy";


            // Authenticate and create a data factory management client
            var context = new AuthenticationContext("https://login.windows.net/" + tenantID);
            ClientCredential cc = new ClientCredential(applicationId, authenticationKey);
            AuthenticationResult result = context.AcquireTokenAsync("https://management.azure.com/", cc).Result;
            ServiceClientCredentials cred = new TokenCredentials(result.AccessToken);
            var client = new DataFactoryManagementClient(cred) { SubscriptionId = subscriptionId };

            // Create data factory
            Console.WriteLine("Creating data factory " + dataFactoryName + "...");
            Factory dataFactory = new Factory
            {
                Location = region,
                Identity = new FactoryIdentity()

            };
            client.Factories.CreateOrUpdate(resourceGroup, dataFactoryName, dataFactory);
            Console.WriteLine(SafeJsonConvert.SerializeObject(dataFactory, client.SerializationSettings));

            while (client.Factories.Get(resourceGroup, dataFactoryName).ProvisioningState == "PendingCreation")
            {
                System.Threading.Thread.Sleep(1000);
            }

            // Create an Azure Storage linked service
            Console.WriteLine("Creating linked service " + storageLinkedServiceName + "...");

            LinkedServiceResource storageLinkedService = new LinkedServiceResource(
                new AzureStorageLinkedService
                {
                    ConnectionString = new SecureString("DefaultEndpointsProtocol=https;AccountName=" + storageAccount + ";AccountKey=" + storageKey)
                }
            );
            client.LinkedServices.CreateOrUpdate(resourceGroup, dataFactoryName, storageLinkedServiceName, storageLinkedService);
            Console.WriteLine(SafeJsonConvert.SerializeObject(storageLinkedService, client.SerializationSettings));

            // Create an Azure SQL Database linked service
            Console.WriteLine("Creating linked service " + sqlDbLinkedServiceName + "...");

            LinkedServiceResource sqlDbLinkedService = new LinkedServiceResource(
                new AzureSqlDatabaseLinkedService
                {
                    ConnectionString = new SecureString(azureSqlConnString)
                }
            );
            client.LinkedServices.CreateOrUpdate(resourceGroup, dataFactoryName, sqlDbLinkedServiceName, sqlDbLinkedService);
            Console.WriteLine(SafeJsonConvert.SerializeObject(sqlDbLinkedService, client.SerializationSettings));

            // Create a Azure Blob dataset
            Console.WriteLine("Creating dataset " + blobDatasetName + "...");
            DatasetResource blobDataset = new DatasetResource(
                new AzureBlobDataset
                {
                    LinkedServiceName = new LinkedServiceReference
                    {
                        ReferenceName = storageLinkedServiceName
                    },
                    FolderPath = inputBlobPath,
                    FileName = inputBlobName,
                    Format = new TextFormat { ColumnDelimiter = "|" },
                    Structure = new List<DatasetDataElement>
                    {
            new DatasetDataElement
            {
                Name = "FirstName",
                Type = "String"
            },
            new DatasetDataElement
            {
                Name = "LastName",
                Type = "String"
            }
                    }
                }
            );
            client.Datasets.CreateOrUpdate(resourceGroup, dataFactoryName, blobDatasetName, blobDataset);
            Console.WriteLine(SafeJsonConvert.SerializeObject(blobDataset, client.SerializationSettings));

            // Create a Azure SQL Database dataset
            Console.WriteLine("Creating dataset " + sqlDatasetName + "...");
            DatasetResource sqlDataset = new DatasetResource(
                new AzureSqlTableDataset
                {
                    LinkedServiceName = new LinkedServiceReference
                    {
                        ReferenceName = sqlDbLinkedServiceName
                    },
                    TableName = azureSqlTableName
                }
            );
            client.Datasets.CreateOrUpdate(resourceGroup, dataFactoryName, sqlDatasetName, sqlDataset);
            Console.WriteLine(SafeJsonConvert.SerializeObject(sqlDataset, client.SerializationSettings));

            // Create a pipeline with copy activity
            Console.WriteLine("Creating pipeline " + pipelineName + "...");
            PipelineResource pipeline = new PipelineResource
            {
                Activities = new List<Activity>
                {
                    new CopyActivity
                    {
                        Name = "CopyFromBlobToSQL",
                        Inputs = new List<DatasetReference>
                        {
                            new DatasetReference()
                            {
                                ReferenceName = blobDatasetName
                            }
                        },
                        Outputs = new List<DatasetReference>
                        {
                            new DatasetReference
                            {
                                ReferenceName = sqlDatasetName
                            }
                        },
                        Source = new BlobSource { },
                        Sink = new SqlSink { }
                    }
                }
            };
            client.Pipelines.CreateOrUpdate(resourceGroup, dataFactoryName, pipelineName, pipeline);
            Console.WriteLine(SafeJsonConvert.SerializeObject(pipeline, client.SerializationSettings));

            // Create a pipeline run
            Console.WriteLine("Creating pipeline run...");
            CreateRunResponse runResponse = client.Pipelines.CreateRunWithHttpMessagesAsync(resourceGroup, dataFactoryName, pipelineName).Result.Body;
            Console.WriteLine("Pipeline run ID: " + runResponse.RunId);

            // Monitor the pipeline run
            Console.WriteLine("Checking pipeline run status...");
            PipelineRun pipelineRun;
            while (true)
            {
                pipelineRun = client.PipelineRuns.Get(resourceGroup, dataFactoryName, runResponse.RunId);
                Console.WriteLine("Status: " + pipelineRun.Status);
                if (pipelineRun.Status == "InProgress")
                    System.Threading.Thread.Sleep(15000);
                else
                    break;
            }

            // Check the copy activity run details
            Console.WriteLine("Checking copy activity run details...");
            List<ActivityRun> activityRuns = client.ActivityRuns.ListByPipelineRun(
                resourceGroup, dataFactoryName, runResponse.RunId, DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(10), pipelineName).ToList();
            if (pipelineRun.Status == "Succeeded")
            {
                Console.WriteLine(activityRuns.First().Output);
                //SaveToJson(SafeJsonConvert.SerializeObject(activityRuns.First().Output, client.SerializationSettings), "ActivityRunResult.json", folderForJsons);
            }
            else
                Console.WriteLine(activityRuns.First().Error);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
