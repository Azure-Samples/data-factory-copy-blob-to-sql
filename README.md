---
services: data-factory
platforms: dotnet
author: spelluru
---
# Sample: copy data from Azure Blob Storage to Azure SQL Database
In this tutorial, you create a Data Factory pipeline that copies data from Azure Blob Storage to Azure SQL Database. The configuration pattern in this tutorial applies to copying from a file-based data store to a relational data store.

You perform the following steps in this tutorial:

- Create a data factory.
- Create Azure Storage and Azure SQL Database linked services.
- Create Azure BLob and Azure SQL Database datasets.
- Create a pipeline contains a Copy activity.
- Start a pipeline run.
- Monitor the pipeline and activity runs.

  
## Prerequisites

* **Azure subscription**. If you don't have a subscription, you can create a [free trial](http://azure.microsoft.com/pricing/free-trial/) account.
* **Azure Storage account**. You use the blob storage as **source** data store. If you don't have an Azure storage account, see the [Create a storage account](../storage/common/storage-create-storage-account.md#create-a-storage-account) article for steps to create one.
* **Azure SQL Database**. You use the database as **sink** data store. If you don't have an Azure SQL Database, see the [Create an Azure SQL database](../sql-database/sql-database-get-started-portal.md) article for steps to create one.
* **Visual Studio** 2013, 2015, or 2017. The walkthrough in this article uses Visual Studio 2017.
* **Download and install [Azure .NET SDK](http://azure.microsoft.com/downloads/)**.
* **Create an application in Azure Active Directory** following [this instruction](https://github.com/Azure-Samples/data-factory-copy-blob-to-sql.git). Make note of the following values that you use in later steps: **application ID**, **authentication key**, and **tenant ID**. Assign application to "**Contributor**" role by following instructions in the same article.

### Create blob and SQL table

Now, prepare your Azure Blob and Azure SQL Database for the tutorial by performing the following steps:

#### Create source blob

1. Launch Notepad. Copy the following text and save it as **inputEmp.txt** file on your disk.

	```
    John|Doe
    Jane|Doe
	```

2. Use tools such as [Azure Storage Explorer](http://storageexplorer.com/) to create the **adfv2tutorial** container, and to upload the **inputEmp.txt** file to the container.

#### Create sink SQL table

1. Use the following SQL script to create the **dbo.emp** table in your Azure SQL Database.

    ```sql
    CREATE TABLE dbo.emp
    (
        ID int IDENTITY(1,1) NOT NULL,
        FirstName varchar(50),
        LastName varchar(50),
    )
    GO

    CREATE CLUSTERED INDEX IX_emp_ID ON dbo.emp (ID);
    ```

2. Allow Azure services to access SQL server. Ensure that **Allow access to Azure services** setting is turned **ON** for your Azure SQL server so that the Data Factory service can write data to your Azure SQL server. To verify and turn on this setting, do the following steps:

    1. Click **More services** hub on the left and click **SQL servers**.
    2. Select your server, and click **Firewall** under **SETTINGS**.
    3. In the **Firewall settings** page, click **ON** for **Allow access to Azure services**.


## Build and run the sample

1. Click **Tools** -> **NuGet Package Manager** -> **Package Manager Console**.
2. In the **Package Manager Console**, run the following commands to install packages:

    ```
    Install-Package Microsoft.Azure.Management.DataFactory
    Install-Package Microsoft.Azure.Management.ResourceManager
    Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory
    ```
3. Set values for variables in the Program.cs file: 

    ```csharp

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

    ```
4. Build the project and run the program.

## See Also
For step-by-steps instructions to create this sample from scratch, see [Quickstart: create a data factory and pipeline using .NET SDK](https://docs.microsoft.com/azure/data-factory/tutorial-copy-data-from-blob-to-sql-dot-net).
 

