# Context

TODO

TOC:
- [Context](#context)
- [VM](#vm)
- [Docker](#docker)
  - [Database](#database)
  - [Web](#web)
- [VSTS](#vsts)
  - [Build](#build)
  - [Release](#release)
- [OSBA](#osba)
- [Resources](#resources)

# VM

To host this demo, a virtual machine running RedHat Enterprise Linux 7.4 was created in Azure using the following overall process:

1 (#1) - Provision the Red Hat Enterprise Linux 7.4 (RHEL74) virtual machine
2 (#2) - Connect to the RHEL74 VM using SSH.
3 (#3) - Install SQL Server 2017 using RHEL's YUM package manager.
4 (#4) - Restore the WideWorldImporters Full Database Backup 
5 (#5) - Run init-db.sh script to prime the database for the demo.
6 (#6) - Deploy the Docker container hosting the "Web Dashboard Application" that will connect to the SQL Server 2017 deployed natively on RHEL.
7 (#7) - Configure the Network Security Group to allow incoming network traffic on the port exposed by the "Web Dashboard Application", (88 in our case).
8 (#8) - Access and use the "Web Dashboard Application" to demo SQL Server 2017 new Automatic Tuning capability.

## 1 - Provision the Red Hat Enterprise Linux 7.4 (RHEL74) virtual machine using the following details on the Azure Portal (https://portal.azure.com/):
  1) Search the Azure Market Place tamplates for "RedHat Enterprise Linux 7.4" 
  2) Depending on the experience you are looking for, you can either 
    a) select the "RedHat Enterprise Linux 7.4" virtual machine template from Red Hat if you want to manually install SQL Server 2017 over RHEL 7.4 yourself (instructions below as this is what we selected for the first demo to showcase how easy it is to install SQL Server 2017 over RHEL), 
    or 
    b) select the "SQL Server 2017 Enterprise on Red Hat Enterprise Linux 7.4 (RHEL)" virtual machine template from Microsoft if you wat to have SQL Server 2017 Enterprise pre-installed for you on RHEL 7.4.
  3) Create the virtual machine from the selected Azure Market Place virtual machine template using the Resorce Manager deployment model.
  4) On the base configuration panel, provide the following information elements:
    a) Unique name for the machine, all in lowercase. For this demo, we used "sql2017rhel74". A green check mark will appear on the left once a unique name has been provided.
    b) Administrator username. We used mt1admin for our demo.
    c) Either an SSH Public Key or a password. We used a password for our demo.
    d) Select your Azure Subscription.
    e) Create a new resource group, or select an existing one if you prefer. Resource Groups are logical grouping to help compartimentalize, manage and delete resources in Azure once they are no longer needed.
    f) Select a location on where to run your virtual machine. We selected Canada East for our demo.
  5) On the sizing panel, you can select the machine configuration that matches your requirements and budgets. For our demo, selected the 
D16s_V3 configuration to comfortably run SQL Server 2017 natively on RHEL as well as 3 Docker containers (2 Web Application Containers & 1 Container to showcase SQL Server 2017 on Docker).
  6) On the parameters panel, we chose to specify SSH as an open public port (22) and we accepted all other proposed defaults but feel free to select different choices depending on the levels of high availability, storage, networking, management that you require.
  7) On the summary panel, we carefully reviewed our selected configuration and confirmed our selection to start the provisionning of our Demo VM. This process may take a few minutes.
  8) Once the virtual machine has been provisioned, select the Overview and click on the IP address of your virtual machine to configure a DNS name for it. In our case, we used sql2017rhel74 which maps to sql2017rhel74.canadaeast.cloudapp.azure.com. This is sometimes easier to remember and use from remote clients like SQL OPS Studio and SQL Server Management Studio to manage the database.
  9) To allow remote tools to connect and manage SQL Server 2017, select Networking from the Left Panel to create an INPUT PORT RULES using the "Add inbound port rule" to create a rule to allow inbound connection to port 1433.

## 2 - Connect to the RHEL74 VM using SSH. 
  1) On the Overview page of our provisionned VM, we selected the "Connect" button and copied the proposed SSH connection command from the panel appearing on the right.
  2) Either using an SSH GUI Client such as Putty or from the command line using OpenSSH connect to the virtual machine.
  
```
ssh yourAdminUsername@ip_address_of_your_virtual_machine
```
## 3 - Install SQL Server 2017 using RHEL's YUM package manager.

  1) Download the Microsoft SQL Server Red Hat repository configuration file:
  ```
  sudo curl -o /etc/yum.repos.d/mssql-server.repo https://packages.microsoft.com/config/rhel/7/mssql-server-2017.repo
  ```
  2) Install SQL Server 2017:
  ```
  sudo yum install -y mssql-server
  ```
  3) Configure SQL Server 2017:
  ```
  sudo /opt/mssql/bin/mssql-conf setup
  ```
  4) Once the configuration is done, verify that the service is running:
  ```
  systemctl status mssql-server
  ```
  5) To allow remote connections, open the SQL Server port on the firewall on RHEL (default port is TCP 1433):
  ```
  sudo firewall-cmd --zone=public --add-port=1433/tcp --permanent
  sudo firewall-cmd --reload
  ```

  At this point, SQL Server 2017 is running on your RHEL machine and is ready to use!
  
  For more details on the installation of SQL Server 2017 over Red Hat Enterprise Linux, please see 
    https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-red-hat?view=sql-server-linux-2017 

## 4 - Restore the WideWorldImporters Full Database Backup

  1) Download the WideWorldImporters Full Database Backup using the following command:
  ```
  wget https://github.com/Microsoft/sql-server-samples/releases/download/wide-world-importers-v1.0/WideWorldImporters-Full.bak
  ```
  2) Using SQL Ops Studio (https://docs.microsoft.com/en-us/sql/sql-operations-studio/download?view=sql-server-2017), connect to SQL Server 2017 on your virtual machine and run the following SQL Script:
  ```
  restore database WideWorldImporters from disk = '/var/opt/mssql/WideWorldImporters-Full.bak' with
  move 'WWI_Primary' to '/var/opt/mssql/data/WideWorldImporters.mdf',
  move 'WWI_UserData' to '/var/opt/mssql/data/WideWorldImporters_UserData.ndf',
  move 'WWI_Log' to '/var/opt/mssql/data/WideWorldImporters.ldf',
  move 'WWI_InMemory_Data_1' to '/var/opt/mssql/data/WideWorldImporters_InMemory_Data_1',
  stats=5
  go
  ```
  
## 5 - Run the following script to prime the database for the demo:

  1) Using SQL Ops Studio (https://docs.microsoft.com/en-us/sql/sql-operations-studio/download?view=sql-server-2017), connect to SQL Server 2017 on your virtual machine and run the following SQL Script:
  ```
  USE WideWorldImporters;
  -- Insert one OrderLine that with PackageTypeID=(0) will cause regression
  INSERT INTO Warehouse.PackageTypes (PackageTypeID, PackageTypeName, LastEditedBy)
  VALUES (0, 'FLGP', 1);

  INSERT INTO Sales.OrderLines(OrderId, StockItemID, Description, PAckageTypeID, quantity, unitprice, taxrate, PickedQuantity,LastEditedBy)
  SELECT TOP 1 OrderID, StockItemID, Description, PackageTypeID = 0, Quantity, UnitPrice, taxrate , PickedQuantity,LastEditedBy
  FROM Sales.OrderLines;

  -- Add PackageTypeID column into the NCCI index on Sales.OrderLines table
  DROP INDEX IF EXISTS [NCCX_Sales_OrderLines] ON [Sales].[OrderLines]

  CREATE NONCLUSTERED COLUMNSTORE INDEX [NCCX_Sales_OrderLines] ON [Sales].[OrderLines]
  (
    [OrderID],
    [StockItemID],
    [Description],
    [Quantity],
    [UnitPrice],
    [PickedQuantity],
    [PackageTypeID] -- adding package type id for demo purpose
  )WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0) 
  GO

  CREATE OR ALTER PROCEDURE [dbo].[initialize]
  AS BEGIN

    ALTER DATABASE SCOPED CONFIGURATION CLEAR PROCEDURE_CACHE;
    ALTER DATABASE current SET QUERY_STORE CLEAR ALL;

  END
  GO


  CREATE OR ALTER PROCEDURE [dbo].[report] (@packagetypeid int)
  AS BEGIN

  EXEC sp_executesql N'select avg([UnitPrice]*[Quantity])
              from Sales.OrderLines
              where PackageTypeID = @packagetypeid', N'@packagetypeid int', @packagetypeid;

  END
  GO


  CREATE OR ALTER PROCEDURE [dbo].[regression]
  AS BEGIN

  ALTER DATABASE SCOPED CONFIGURATION CLEAR PROCEDURE_CACHE;
  BEGIN
         declare @packagetypeid int = 0;
         exec report @packagetypeid;
  END

  END
  GO

  CREATE OR ALTER PROCEDURE [dbo].[auto_tuning_on]
  AS BEGIN

    ALTER DATABASE current SET AUTOMATIC_TUNING ( FORCE_LAST_GOOD_PLAN = ON);
    ALTER DATABASE SCOPED CONFIGURATION CLEAR PROCEDURE_CACHE;
    ALTER DATABASE current SET QUERY_STORE CLEAR ALL;

  END
  GO


  CREATE OR ALTER PROCEDURE [dbo].[auto_tuning_off]
  AS BEGIN

    ALTER DATABASE current SET AUTOMATIC_TUNING ( FORCE_LAST_GOOD_PLAN = OFF);
    ALTER DATABASE SCOPED CONFIGURATION CLEAR PROCEDURE_CACHE;
    ALTER DATABASE current SET QUERY_STORE CLEAR ALL;

  END
  GO
  ```

## 6 - Deploy the Docker container hosting the "Web Dashboard Application" that will connect to the SQL Server 2017 deployed natively on RHEL.

  ```
  sudo docker run \
  -e 'ConnectionStrings:Wwi=Server=10.1.1.4,1433;Database=WideWorldImporters;User Id=SA;Password=SQL2017R0ck5;' \
  -p 88:80 \
  --restart \
  --name webdashboard1 \
  -d mabenoit/sql-autotune-dashboard:latest
  ```
  
## 7 - Configure the Network Security Group to allow incoming network traffic on the port exposed by the "Web Dashboard Application", (88 in our case).
  1) To allow your browser to connect to the demo Web Dashboard that showcases SQL Server 2017's Automatic Tuning capability, select Networking from the Left Panel to create an INPUT PORT RULES using the "Add inbound port rule" to create a rule to allow inbound connection to port 88.
  
## 8 - Access and use the "Web Dashboard Application" to demo SQL Server 2017 new Automatic Tuning capability.

  1) Just point your browser to the URL http://ip_address_of_your_virtual_machine:88/
  
  2) Notice the big gauge indicating a high number of requests per second. 
  
  3) Notice that Automatic Tuning is OFF at first.
  
  4) Notice on the top right of the page the SQL query that is being repeated sent to the SQL Server 2017 in order to generate the workload.
  ```
  SELECT AVG( UnitPrice * Quantity )
  FROM Sales.OrderLines
  WHERE PackageTypeID = @packagetypeid;
  ```  
  5) Click on the Red "Regression" Button to trigger a degredation in performance and notice the impact on the gauge and the number of requests per second.
  
  6) Notice on the bottom right of the page the SQL Syntax needed to activate SQL Server 2017's Automatic Tuning capability.
  
  7) Click on the On radio button below the gauge to activate SQL Server 2017's Automatic Tuning capability and notice the impact on the gauge and the number of requests per second that goes back up again automatically!
  
Now for the next demo segment, we will show that SQL Server 2017 can just as easily be deployed in a Docker container. We will use another copy of the same Web Dashboard Application to connect to that Docker contained SQL Server 2017 Database and generate the same workload and showcase the same Automatic Tuning capability.

# Docker

## Database

The database is SQL Server 2017 to illustrate its support on Linux and especially on Linux Containers.

Pull the lastest version of the Docker image from the public image:
```
docker pull mabenoit/my-mssql-linux:latest
```

Run the Docker image from the public image:
```
docker run \
  -e 'ACCEPT_EULA=Y' \
  -e 'SA_PASSWORD=<sa-password>' \
  -p 1433:1433 \
  --name <container-name> \
  -d mabenoit/my-mssql-linux:latest
```

Open a bash session within this container:
```
docker exec \
  -it <container-name> \
  bash
```

And execute this command to initialize the database:
```
usr/share/wwi-db-setup/init-db.sh
```

Optional - if you would like you could build the Docker image locally:
```
cd SqlServerAutoTuningDashboard
docker build \
  -t <image-name> \
  -f Dockerfile-Sql \
  .
```

## Web

The web application is a simple dashboard to interact with Sql Server 2017 to demonstrate the AutoTuning feature.

This web application is coming from [this repository](https://github.com/Microsoft/sql-server-samples/tree/master/samples/features/automatic-tuning/force-last-good-plan) + few updates with ASP.NET Core 2.0 and some simplifications + Docker support.

Pull the lastest version of the Docker image from the public image:
```
docker pull mabenoit/sql-autotune-dashboard:latest
```

Run the Docker image from the public image:
```
docker run \
  -e 'ConnectionStrings:Wwi=Server=<server-address>,1433;Database=WideWorldImporters;User Id=SA;Password=<sa-password>;' \
  -p 80:80 \
  --name webdashboard \
  -d mabenoit/sql-autotune-dashboard:latest
```

Optional - if you would like you could build the Docker image locally:
```
cd SqlServerAutoTuningDashboard
docker build \
  -t <image-name> \
  -f Dockerfile-Web \
  .
```

# VSTS

## Build

Prerequisities:
- You need a VSTS account and project
- You need a Connection endpoint in VSTS to your Container Registry (ACR, DockerHub, etc.) to be able to push your image built

High level steps:
- .NET Core - Restore packages
- .NET Core - Build Web app
- .NET Core - Package Web app
- Docker - Build Web image
- Docker - Push Web image
- Docker - Build Sql image
- Docker - Push Sql image

See the details of this [build definition in YAML file here](./SqlServerAutoTuningDashboard/VSTS-CI.yml).

## Release

Prerequisities:
- You need an OpenShift cluster...

# OSBA

TODO

# Resources
- [Enhancing DevOps with SQL Server on Linux](https://alwaysupalwayson.blogspot.com/2018/06/enhancing-devops-with-sql-server-on.html)
