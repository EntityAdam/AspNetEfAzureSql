# Problem-Framing

This quick start is based on a common migration effort for U.S. Air Force projects.
The common stack is a .NET Framework Application connected to an on-premesis SQL Server. 
When migrating this type of workload from on-premesis SQL Server to Azure SQL I have found it is vital to first ensure the applicaiton is running locally with no external connections.
Sometimes the LocalDB of SQL Server Express is enough, but there are cases where an environment with full SQL Server Developer Edition and IIS need to be set up.
This scenario beings with an application that will run locally on your developer machine provided you have VS2017+ and the Web Development workload installed.

# Running the Application locally

This application is a simple ASP.NET Application targeting .NET Framework 4.8.  It is also using Entity Framework 6 for data access. EF6 uses the same underlying SQL provider as ADO.NET (`System.Data.SqlClient`)and they are practically synonymous.

> Aside: If the application you are migrating is targeting version 4.7.2 or lower, in my experience, you will run into significant problems.

1. The application is initially configured to run locally using IIS Express and SQL Server Express

2. Provided your SQL Express configuration has not been changed from it's initial state, continuing in the application should cause EF6 to create the database without any intervention. This operation may take a minute or two. If you recieve an error, check your configuration and update the connection string as necessary in the `Web.config` file.


### Web.Config 
```
<connectionStrings>
    <add name="BorkContext" connectionString="Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ISBORKED;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False" providerName="System.Data.SqlClient" />
</connectionStrings>
```

3. After viewing the application works as expected locally. Deploy the Azure Resources in the `Azure Setup` section.

# Azure Setup

## Resoure Deployment 

```sh
#!/bin/bash
###################
## CONFIGURATION ##
###################
# Select location
location="East US"

# Pick a few random characters to generate globally unique resource names
randomString="hlnzy"

# Edit if you want, just make sure it's not a common admin name eg; admin, administrator, root, etc.
sqlUser="aspnet_admin_user"

# Generate a secure password using your favorite password manager
sqlPass="{generate a password}"

# Friendly workload name. eg; MyApp
wl="myapp"

# Enter your IP address, startIP and endIP can be the same value.
# curl https://api.ipify.org
startIP=0.0.0.0
endIP=0.0.0.0

# No need to change these; Generates resource names based on the info above.
groupName="$wl-rg-$randomString"
sqlName="$wl-sql-$randomString"
aspName="$wl-pl-$randomString"
appName="$wl-wa-$randomString"
dbName="${wl}db"

############
## DEPLOY ##
############

# Authenticate to Azure
az login

# create resource group
az group create --name "$groupName" --location "$location"

# create app service plan
az appservice plan create --name "$aspName" -g "$groupName"

# create webapp
az webapp create -g "$groupName" --plan "$aspName" --name "$appName"

# assign identity to webapp 
az webapp identity assign --name "$appName" -g "$groupName"

# create sql server
az sql server create -l "$location" -g "$groupName" -n "$sqlName" -u "$sqlUser" -p "$sqlPass"

# configure firewall
az sql server firewall-rule create -g "$groupName" --server "$sqlName" -n AllowYourIp --start-ip-address $startIP --end-ip-address $endIP

# create database
az sql db create -g "$groupName" --server "$sqlName" --name "$dbName" --edition Basic --zone-redundant false
```

## Configuring SQL Server

1. Head the the SQL Server Resource
2. In the Overview blade, on the right, you will see `Active Directory admin: Not configured`
3. Click the `Not Configured` link
4. Click `Set admin`
5. Select a user to set as the Active Directory Admin

## Grant SQL Access to the Web App Identity

1. Open up SQL Server Management Studio (SSMS)
2. In the connect to Server dialog use the following to log in as the Active Directory Admin
   - Server name: {myapp}.database.windows.net
   - Authentication: Azure Active Directory - Universal with MFA
   - User name: {user@domain}
3. This should provide a familliar Azure Single Sign On prompt, log in to continue
4. Start a new query
5. Execute the below query

```sql
CREATE USER [<YOUR WEB APP NAME>] FROM EXTERNAL PROVIDER;

--Example
--CREATE USER [myapp-wa-hlnzy] FROM EXTERNAL PROVIDER;
```

## Allow Azure Resources through the SQL Firewall

1. Navigate to the SQL Server resource
2. Open Security -> Firewalls and virtual networks blade
3. Select 'Yes' for Allow Azure services and resources to access this server

# Configuring the application to run when deployed to Azure

## Authentication provider

```xml
<configSections>
    <section name="SqlAuthenticationProviders" type="System.Data.SqlClient.SqlAuthenticationProviderConfigurationSection, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" />
    <section name="entityFramework" type="System.Data.Entity.Internal.ConfigFile.EntityFrameworkSection, EntityFramework, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" requirePermission="false" />
</configSections>
```

At the top of the `Web.config` file, the first section in the `<configSections>` is the authentication provider which will allow the `SqlClient` to authenticate to Azure based on the Managed Identity assigned to the Azure App Service resource.

## Connection strings

Earlier in the scenario I mentioned the application is initially configured for local development.  To deploy this to our App Service, we will need to swap the connection strings by commenting the local configuration, and uncommenting the Azure configuration.  We will also have to supply the correct values for the `Server`, `Initial Catalog`, and `UID` parameters

 - Server=tcp:{sql-server-name}.database.windows.net
 - Initial Catalog={database-name}
 - UID={app-name}

### Web.config should look like this:
```
<!-- Local Development Configuration -->
<!--
<connectionStrings>
    <add name="BorkContext" connectionString="Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ISBORKED;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False" providerName="System.Data.SqlClient" />
</connectionStrings>
-->

<!-- Azure Configuration -->
<connectionStrings>
    <add name="BorkContext" connectionString="Server=tcp:myapp-sql-hlnzy.database.windows.net,1433;database=myappdb;UID=myapp-sql-hlnzy;Authentication=Active Directory Interactive" providerName="System.Data.SqlClient" />
</connectionStrings>
```

# Publishing the Application

1. Right click on the project in solution explorer
2. Publish
3. Create folder profile
4. publish to `bin\app.publish\`


# Deploying the Application

We can deploy a web app using the `az cli` using the `webapp deployment` feature.


```s
#!/bin/bash
#####################
### Configuration ###
#####################

# Re-using the earlier variables from the resource deployment, re-run these if you are not in the same session 
randomString="hlnzy"
wl="myapp"
appName="$wl-wa-$randomString"
groupName="$wl-rg-$randomString"

az webapp deployment source config-zip -g "$groupName" -n "$appName" --src "AspNetEfAzureSql/bin/app.publish/app.publish.zip"
```

# Results!

Navigate to the App Service's public URL and everything should be working

> https://{app-name}.azurewebsites.net/