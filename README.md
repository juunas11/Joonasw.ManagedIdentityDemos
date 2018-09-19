# Azure Managed Identity demo collection

This is an ASP.NET Core 2.2 (preview 2) app which demonstrates usage
of some Azure services with Managed Identity authentication:

* Key Vault for configuration data
* Blob Storage
* SQL Database
* Service Bus Queue

There is also a demo of calling a custom API, which is in the Joonasw.ManagedIdentityDemos.CustomApi folder.

## Setup instructions

For local development you will need the 2.2 .NET Core SDK (currently preview 2).
If you use Visual Studio, currently you'd need VS 2017 Preview (15.9.0 preview 2).

For local development, you can give your user account access to the resources.
You will need to login to the az CLI and ensure you are logged in to the right Azure AD tenant.
Another option for Visual Studio users is to set your account in Tools -> Options -> Azure Service Authentication.

For local development, it is usually preferrable to set the Azure AD tenant id
explicitly in the appsettings.Development.json of the app.
You should be able to find the following setting: `ManagedIdentityTenantId`.
Find your Azure AD tenant id from the Azure Portal (Azure Active Directory -> Properties) and set it there.
This will ensure that you are always acquiring tokens for the correct Azure AD tenant at runtime locally.

To run the app in Azure, you'll need at least one Web App to run the main app.
And don't forget to enable Managed Identity on the app.
This will generate a Service Principal that you'll be giving access to.
At the moment, you need to install the ASP.NET Core 2.2 Preview 2 x86 Runtime extension on the App Service.

### Key Vault

My article on the subject: https://joonasw.net/view/aspnet-core-azure-keyvault-msi

You'll need to create an Azure Key Vault first.
The only configuration setting in appsettings.json you'll have to set is `KeyVaultBaseUrl`.
Here is an example:

```json
{
  "Demo": {
    "KeyVaultBaseUrl": "https://keyvaultname.vault.azure.net/"
  }
}
```

You should then add a secret in the Key Vault with the name **Demo--KeyVaultSecret**.
The value can be anything you want.

Then you will need to create an access policy that gives Secret Get & List permissions
to your user account and/or the generated managed identity service principal.

Now you should be able to run the app and see the secret value in the Key Vault tab.

### Blob Storage

Documentation: https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad

My article on the subject: https://joonasw.net/view/azure-ad-authentication-with-azure-storage-and-managed-service-identity

This time you'll need to a Storage Account.
The settings that affect this demo are:

* Demo:StorageAccountName
* Demo:StorageContainerName
* Demo:StorageBlobName

These should be fairly self-explanatory.
The first one is the name of the Storage account,
the second a blob container that you need to create,
and the last the name of a file that you add to that container.

Once you have done those things add one of the following roles to either your user account
and/or the generated service principal:

* Storage Blob Data Reader
* Storage Blob Data Contributor

The role is added via the Access Control (IAM) tab of the Storage account or the container.

After doing these things, the Storage demo tab should work.
It will load the files contents and display them as text on the view.

### SQL Database

First you'll need an Azure SQL Database of course.
The explanation on how to enable Azure AD authentication there is a bit long.
You can read the official documentation here: https://docs.microsoft.com/en-us/azure/sql-database/sql-database-aad-authentication-configure.

The main thing that you need to achieve is add your user account and/or service principal read access to a database.
The article above will tell you how to add your user account there.
Adding service principals is not so straightforward.
You need to add the service principal to an Azure AD group, and then add this group access in the SQL database.
To do that, you need to use PowerShell: https://docs.microsoft.com/en-us/powershell/module/azuread/?view=azureadps-2.0.

```ps
Connect-AzureAD

#Enterprise Applications -> All applications + search filter -> Apply -> Find app -> Properties -> Object id
#or check from e.g. the App Service resource via Resource Explorer
$msiSpId = '82c55921-c5a6-4c26-847a-4a3cb2620d06'

#You'll need the Azure AD Group's Object Id (you can get it from the group's Properties)
$sqlGroupId = 'a3941883-6d1d-4460-84cd-446562f26815'
Add-AzureADGroupMember -ObjectId $sqlGroupId -RefObjectId $msiSpId
```

The app does not generate the database table,
so here is an SQL script you can run after doing the Azure AD admin setup
and connecting with your admin account:

```sql
--Login with the AAD Admin

CREATE TABLE Test
(
 [Id] INT IDENTITY,
 [Value] NVARCHAR(128)
);

INSERT INTO Test ([Value]) VALUES ('Test');
INSERT INTO Test ([Value]) VALUES ('Test 2');
INSERT INTO Test ([Value]) VALUES ('Test 3');

-- SQL Table Readers is the name of the group in Azure AD
CREATE USER [SQL Table Readers] FROM EXTERNAL PROVIDER;
GRANT SELECT ON dbo.Test TO [SQL Table Readers];

-- Here is how you would add access to a user
CREATE USER [firstname.lastname@yourtenant.onmicrosoft.com] FROM EXTERNAL PROVIDER;
GRANT SELECT ON dbo.Test TO [firstname.lastname@yourtenant.onmicrosoft.com];
```

Then you can setup the configuration settings needed for the SQL database:

* Demo:SqlConnectionString

Here is an example value for the connection string:

```json
{
  "Demo": {
    "SqlConnectionString": "Data Source=yourserver.database.windows.net; Initial Catalog=DatabaseName;"
  }
}
```

Just replace *yourserver* with your SQL Server name and *DatabaseName* with your SQL Database name.
No keys or passwords, just the way we like it.

This should (finally) be enough to run the sample.
There is a commented-out call in the DemoService class which uses ADO.NET instead
of EF Core if you are interested in that option.

### Azure Service Bus

For this sample, you need to create an Azure Service Bus namespace
and create a queue in it.
Then, configure the relevant settings in appsettings.json:

```json
{
  "Demo": {
    "ServiceBusNamespace": "your-namespace-name",
    "ServiceBusQueueName": "your-queue-name"
  }
}
```

Then, add your user account/the generated service principal to the Contributor/Owner role on the
Service Bus namespace via the Access Control (IAM) tab.

If you run the app on Azure, make sure you enable Web Sockets so the listener works.

That's it for the configuration.
The demo should now work, and consists of two parts.
The listener tab hooks up th

### Custom API

