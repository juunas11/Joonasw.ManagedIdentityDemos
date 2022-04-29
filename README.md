# Azure Managed Identity demo collection

This is an ASP.NET Core 6.0 app which demonstrates usage
of some Azure services with Managed Identity authentication:

- Key Vault
- App Configuration
- Cosmos DB
- SQL Database
- Data Lake Gen 2
- Blob Storage
- Event Hub
- Service Bus Queue
- Azure Maps
- Cognitive Services

There is also a demo of calling a custom API, which is in the Joonasw.ManagedIdentityDemos.CustomApi folder.

## Setup instructions

For local development you will need the [6.0 .NET SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).
If you use Visual Studio, currently you'd need at least VS 2022.

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

An extension method ([UseAzureKeyVaultConfiguration](https://github.com/juunas11/Joonasw.ManagedIdentityDemos/blob/master/Joonasw.ManagedIdentityDemos/Extensions/WebHostBuilderExtensions.cs))
is used in [Program.cs](https://github.com/juunas11/Joonasw.ManagedIdentityDemos/blob/master/Joonasw.ManagedIdentityDemos/Program.cs)
to add the Key Vault configuration source.

### Blob Storage

Documentation: https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad

My article on the subject: https://joonasw.net/view/azure-ad-authentication-with-azure-storage-and-managed-service-identity

This time you'll need to a Storage Account.
The settings that affect this demo are:

```json
{
  "Demo": {
    "StorageAccountName": "your-storage-account-name",
    "StorageContainerName": "your-blob-container-name",
    "StorageBlobName": "name-of-file-in-container"
  }
}
```

These should be fairly self-explanatory.
The first one is the name of the Storage account,
the second a blob container that you have created,
and the last the name of a file that you have added to that container.

Once you have done those things,
add one of the following roles to either your user account
and/or the generated service principal:

* Storage Blob Data Reader
* Storage Blob Data Contributor

The role is added via the Access Control (IAM) tab of the Storage account or the container.

After doing these things, the Storage demo tab should work.
It will load the files contents and display them as text on the view.

You can see how it uses the Storage SDK in
the `AccessStorage` function of [DemoService](https://github.com/juunas11/Joonasw.ManagedIdentityDemos/blob/master/Joonasw.ManagedIdentityDemos/Services/DemoService.cs).

### SQL Database

First you'll need an Azure SQL Database of course.
The explanation on how to enable Azure AD authentication there is a bit long.
You can read the official documentation here: https://docs.microsoft.com/en-us/azure/sql-database/sql-database-aad-authentication-configure.

The main thing that you need to achieve is
add your user account and/or service principal read access to a database.
The article above will tell you how to add your user account there.

Adding service principals used to be a bit difficult.
You needed to add the service principal to an Azure AD group,
and then add this group access in the SQL database.
Not anymore.
You can now give the service principal access on SQL just by using its name.

You can find the service principal's name in the Azure Portal (under Azure Active Directory -> Enterprise applications).
It'll be e.g. your App Service's name if it is system-assigned,
or the name you chose if it is a user-assigned identity.

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

-- joonasmsitests is the name of the service principal in Azure AD
CREATE USER [joonasmsitests] FROM EXTERNAL PROVIDER;
GRANT SELECT ON dbo.Test TO [joonasmsitests];

-- Here is how you would add access to a user
--CREATE USER [firstname.lastname@yourtenant.onmicrosoft.com] FROM EXTERNAL PROVIDER;
--GRANT SELECT ON dbo.Test TO [firstname.lastname@yourtenant.onmicrosoft.com];
```

Then you can setup the configuration settings needed for the SQL database.
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
The listener tab connects to the SignalR hub using a WebSocket connection
and prints all received messages.
The sender tab sends messages to the queue.

Sending messages is done in the `SendServiceBusQueueMessage` function of [DemoService](https://github.com/juunas11/Joonasw.ManagedIdentityDemos/blob/master/Joonasw.ManagedIdentityDemos/Services/DemoService.cs).
Receiving messages happens in the background service [QueueListenerService](https://github.com/juunas11/Joonasw.ManagedIdentityDemos/blob/master/Joonasw.ManagedIdentityDemos/Background/QueueListenerService.cs).

### Custom API

My article on this: https://joonasw.net/view/calling-your-apis-with-aad-msi-using-app-permissions

The last demo calls an API we have made.
For this demo, you will need to register an application in Azure AD
that represents the API.
To do this, go to *Azure Active Directory -> App registrations*.
When creating the registration, make sure the type is *Web app/API*.
The sign-on URL can be `https://localhost`, it won't be used anyway.

After creating it, grab the *Application Id*.
Also go to the *Properties* and grab the *Application ID URI*.
Enter the ID URI in the front-end application's settings:

```json
{
  "Demo": {
    "CustomApiApplicationIdUri": "your-api-id-uri"
  }
}
```

Then set the following settings in the API app's configuration:

```json
{
  "Authentication": {
    "ClientId": "your-app-application-id",
    "ApplicationIdUri": "your-app-application-id-uri",
    "Authority": "https://login.microsoftonline.com/your-tenant-id"
  }
}
```

The first two settings are the bits of info we got after creating the app.
The authority should contain your Azure AD's id.
You can find it from *Azure Active Directory -> Properties*.

To run the demo on Azure, you will need an additional Web App to run the API.

Now technically we could run the sample.
But if we want to do things properly, we will want to specify an application permission
on our API that the front-end then uses.
This way we can limit what the app can do.

As a sample, here is the application permission defined in the API *Manifest*:

```json
{
  "appRoles": [
    {
      "allowedMemberTypes": [
        "Application"
      ],
      "displayName": "Read all things",
      "id": "32028ccd-3212-4f39-3212-beabd6787d81",
      "isEnabled": true,
      "description": "Allow the application to read all things as itself.",
      "value": "Things.Read.All"
    }
  ]
}
```

This can then be assigned to the generated service principal with PowerShell:

```ps
#Login to Azure AD
Connect-AzureAD

#Id of the role specified in the manifet
$roleId = '32028ccd-3212-4f39-3212-beabd6787d81'

#Enter your App Service name in the SearchString
$msiSpId = (Get-AzureADServicePrincipal -SearchString 'your-app-name').ObjectId

#Enter the name of the app registration for the API in the SearchString
$apiSpId = (Get-AzureADServicePrincipal -SearchString 'your-api-name').ObjectId

New-AzureADServiceAppRoleAssignment -ObjectId $msiSpId -Id $roleId -PrincipalId $msiSpId -ResourceId $apiSpId
```

Now we can run the API and front-end app,
and call the API from the Custom API tab.
The call is implemented in the `AccessCustomApi` function of [DemoService](https://github.com/juunas11/Joonasw.ManagedIdentityDemos/blob/master/Joonasw.ManagedIdentityDemos/Services/DemoService.cs).

## Azure Event Hubs

To run the Event Hubs demos, you'll of course need an Event Hubs namespace.
Create an Event Hub there.
You also need to create a Storage account (or use the same one from the other demo).
This Storage account is used by the listener to keep locks on the partitions.
Then, configure the relevant settings in appsettings.json:

```json
{
  "Demo": {
    "EventHubNamespace": "your-namespace-name",
    "EventHubName": "your-hub-name",
    "EventHubStorageContainerName": "mi-demos-leases"
  }
}
```

You can leave the container name as it is or change it to another.

Then, add your user account/the generated service principal to the Contributor/Owner role on the
Event Hubs namespace via the Access Control (IAM) tab.

You also need to setup Key Vault as a configuration provider following the Key Vault
section instructions.

You'll need a secret named **Demo--EventHubsStorageConnectionString**,
containing a connection string for the Storage account you want to use for holding the partition locks.

If you run the app on Azure, make sure you enable Web Sockets so the listener works.

That's it for the configuration.
The demo should now work, and consists of two parts.
The listener tab connects to the SignalR hub using a WebSocket connection
and prints all received messages.
The sender tab sends messages to the queue.

Sending messages is done in the `SendEventHubsMessage` function of [DemoService](https://github.com/juunas11/Joonasw.ManagedIdentityDemos/blob/master/Joonasw.ManagedIdentityDemos/Services/DemoService.cs).
Receiving messages happens in the background service [EventHubsListenerService](https://github.com/juunas11/Joonasw.ManagedIdentityDemos/blob/master/Joonasw.ManagedIdentityDemos/Background/EventHubsListenerService.cs).
