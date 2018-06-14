# Context

This repository has been built to showcase some integrations between RedHat and Microsoft Azure: RHEL74 VM, .NET Core, SQL Server on Linux, Docker, Azure Container Registry (ACR), OpenShift Container Platform (OCP), Visual Studio Team Services (VSTS), Open Service Broker for Azure (OSBA), etc.

The goal is demonstrate a typical flow of a modernization journey: from on-premise, to public cloud IaaS, then going more agile with Containers, to leverage more platform capabilities with OCP and then finally take advantage of your SQL database as a Service:

![Modernization Journey Workflow](./imgs/Modernization_Journey_Workflow.png)

TOC:
- [Context](#context)
- [VM](#vm)
- [Docker](#docker)
- [VSTS](#vsts)
  - [Build](#build)
  - [Release](#release)
- [OSBA](#osba)
- [Resources](#resources)

# VM

Prerequisities:
- A **Red Hat Enterprise Linux 7.4 (RHEL)** VM
- ASP.NET Core installed
- Two "Inbound port rule" on the associated "Azure Network Security Group", one for the port 1433 and the other for the port 88 to allow external connections to the web app and to the database endpoint.
- On your local machine, a "[SQL Operations Studio](https://docs.microsoft.com/en-us/sql/sql-operations-studio/download?)" installed

From your local machine, connect to the RHEL74 VM using SSH:
```	
ssh yourAdminUsername@ip_address_of_your_virtual_machine	
```
Download the Microsoft SQL Server Red Hat repository configuration file:	
```	
sudo curl -o /etc/yum.repos.d/mssql-server.repo https://packages.microsoft.com/config/rhel/7/mssql-server-2017.repo	
```
Install SQL Server 2017:	
```	
sudo yum install -y mssql-server	
```	
Configure SQL Server 2017:	
```	
sudo /opt/mssql/bin/mssql-conf setup	
```	
Once the configuration is done, verify that the service is running:	
```	
systemctl status mssql-server	
```	
To allow remote connections, open the SQL Server port on the firewall on RHEL (default port is TCP 1433):	
```	
sudo firewall-cmd --zone=public --add-port=1433/tcp --permanent	
sudo firewall-cmd --reload	
```	
	
*At this point*, SQL Server 2017 is running on your RHEL machine and is ready to use!

To setup the database for the purpose of this demo, from within the RHEL74 VM, run the following commands:
```
cd /var/opt/mssql/backup
wget https://github.com/Microsoft/sql-server-samples/releases/download/wide-world-importers-v1.0/WideWorldImporters-Full.bak
```

Using SQL Ops Studio on your local machine, connect to SQL Server 2017 on your virtual machine and run:
- this [restore.sql Script](https://raw.githubusercontent.com/erickangMSFT/sqldevops/master/docker_cluster/aks/restore.sql).
- and this [init-db.sql Script](./SqlServerAutoTuningDashboard/SqlScripts/init-db.sql).

![SQL Ops Studio](./imgs/SQL_OPS_Studio.png)

Now let's setup the web application.

The web application is a simple dashboard to interact with Sql Server 2017 to demonstrate the AutoTuning feature.

This web application is coming from [this repository](https://github.com/Microsoft/sql-server-samples/tree/master/samples/features/automatic-tuning/force-last-good-plan) + few updates with ASP.NET Core 2.0 and some simplifications + Docker support.

Now run the ASP.NET Core application from withing your RHEL74 VM, bu executing this command:
```
cd ~
git clone https://github.com/mathieu-benoit/RedHatOpenShiftAndMicrosoftAzureWorkshop.git
cd RedHatOpenShiftAndMicrosoftAzureWorkshop/SqlServerAutoTuningDashboard
dotnet restore
dotnet build
dotnet run
```

From your local machine, just point your browser to the URL http://rhel74_ip_address:88/.

There is few features to demonstrate from this web dashboard page:
- Click on the Red "Regression" Button to trigger a degredation in performance and notice the impact on the gauge and the number of requests per second.
- Click on the On radio button below the gauge to activate SQL Server 2017's Automatic Tuning capability and notice the impact on the gauge and the number of requests per second that goes back up again automatically!

![Dashboard Web App](./imgs/SQL2017AutoTuning.png)

# Docker

Prerequisities:
- Docker CE has to be installed on the RHEL74 VM

*General remark: for the purpose of this demo, all the base images (sql-server-linux and dot-net-core) are Ubuntu. It works on RHEL. But for security,compliance and support you should use the RHEL based image for your own workload.*

Let's now illustrate SQL Server 2017 support on Linux Containers. For that we created and pushed a [public Docker image here](https://hub.docker.com/r/mabenoit/my-mssql-linux/) which will contain the scripts for the purpose of this demo.

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
  --name sql \
  -d mabenoit/my-mssql-linux:latest
```

And execute this command to initialize the database:
```
docker exec \
  sql \
  /usr/share/wwi-db-setup/init-and-restore-db.sh
```

*Optional - if you would like you could build the Docker image locally:*
```
cd SqlServerAutoTuningDashboard
docker build \
  -t m-mssql-linux \
  -f Dockerfile-Sql \
  .
```

Now, let's run the web application presented earlier in a Docker container. For the purpose of this demonstration, we've exposed its associated [public Docker image here](https://hub.docker.com/r/mabenoit/sql-autotune-dashboard/).

Pull the lastest version of the Docker image from the public image:
```
docker pull mabenoit/sql-autotune-dashboard:latest
```

Run the Docker image from the public image:
```
docker run \
  -e 'ConnectionStrings_Wwi=Server=<server-address>,1433;Database=WideWorldImporters;User Id=SA;Password=<sa-password>;' \
  -p 80:80 \
  --name web \
  -d mabenoit/sql-autotune-dashboard:latest
```

From your local machine, just point your browser to the URL http://rhel74_ip_address:80/, where we could demonstrate the same features than previously.

*Optional - if you would like you could build the Docker image locally:*
```
cd SqlServerAutoTuningDashboard
docker build \
  -t sql-autotune-dashboard \
  -f Dockerfile-Web \
  .
```

# VSTS

## Build

[![Build Status](https://mabenoit-ms.visualstudio.com/_apis/public/build/definitions/f2b899c8-a46f-4300-a9fd-cc3bd7f6f15e/52/badge)](https://mabenoit-ms.visualstudio.com/_apis/public/build/definitions/f2b899c8-a46f-4300-a9fd-cc3bd7f6f15e/52/badge)

The goal here is to build and push both images: SQL and Web in a private Azure Container Registry via VSTS and more specifically with VSTS Build.

Prerequisities:
- You need a VSTS account and project
- You need a Connection endpoint in VSTS to your Azure Container Registry to be able to push your images built

High level steps:
- .NET Core - Restore packages
- .NET Core - Build Web app
- .NET Core - Package Web app
- Docker - Build Web image
- Docker - Push Web image
- Docker - Build Sql image
- Docker - Push Sql image
- Copy and publish Kubernetes' yml files as Build artifact for further deployments

See the details of this [build definition in YAML file here](./SqlServerAutoTuningDashboard/VSTS-CI.yml).

![VSTS CI](./imgs/VSTS_CI.PNG)

## Release

PRE-PROD: [![PRE-PROD Status](https://rmsprodscussu1.vsrm.visualstudio.com/Ae373a2ff-a162-446f-b7ec-415465e9e56c/_apis/public/Release/badge/f2b899c8-a46f-4300-a9fd-cc3bd7f6f15e/4/6)](https://rmsprodscussu1.vsrm.visualstudio.com/Ae373a2ff-a162-446f-b7ec-415465e9e56c/_apis/public/Release/badge/f2b899c8-a46f-4300-a9fd-cc3bd7f6f15e/4/6)

PROD: [![PROD Status](https://rmsprodscussu1.vsrm.visualstudio.com/Ae373a2ff-a162-446f-b7ec-415465e9e56c/_apis/public/Release/badge/f2b899c8-a46f-4300-a9fd-cc3bd7f6f15e/4/8)](https://rmsprodscussu1.vsrm.visualstudio.com/Ae373a2ff-a162-446f-b7ec-415465e9e56c/_apis/public/Release/badge/f2b899c8-a46f-4300-a9fd-cc3bd7f6f15e/4/8)

The goal here is to deploy both images: SQL and Web on a given OpenShift Cluster via VSTS and more specifically with VSTS Release. The first environment `PRE-PROD` will be automatically provisioned in continuous integration/delivery whereas then the `PROD` environment will need manual approval.

![VSTS CD PIPELINE](./imgs/VSTS_CD_PIPELINE.PNG)

Prerequisities:
- You need an OpenShift Origin or Container Platform cluster
- You need a VSTS account and project
- You need a Connection endpoint in VSTS to your OpenShift Kubernetes cluster to be able to deploy your Docker images
- You need a Connection endpoint in VSTS to your Azure Container Registry to be able to create the associated secret in your OpenShift Kubernetes cluster

Variables:
- SA_PASSWORD = your-sa-password
- SqlDeployName = sql
- WebDeployName = dotnetcore
- ACR_SERVER = your-acr-name.azurecr.io
- CONNECTIONSTRINGS_WWI = SERVER=sql;DATABASE=WideWorldImporters;UID=SA;PWD=your-sa-password;
- K8sNamespace = your-ocp/k8s-project/namspace

High level steps:
- Replace tokens in **/*.yml
  - Root directory = `$(System.DefaultWorkingDirectory)/SqlAutoTuneDashboard-ACR-CI/k8s-ymls/SqlServerAutoTuningDashboard`
  - Target files = `**/*.yml`
- kubectl apply - sql
  - Command = `apply`
  - Arguments = `-f $(System.DefaultWorkingDirectory)/SqlAutoTuneDashboard-ACR-CI/k8s-ymls/SqlServerAutoTuningDashboard/k8s-sql.yml`
  - Namespace = $(K8sNamespace)
  - Secrets
    - Azure Container Registry = your-acr
    - Secret name = `acr-secret`
- kubectl apply - web
  - Command = `apply`
  - Arguments = `-f $(System.DefaultWorkingDirectory)/SqlAutoTuneDashboard-ACR-CI/k8s-ymls/SqlServerAutoTuningDashboard/k8s-web.yml`
  - Namespace = $(K8sNamespace)
  - Secrets
    - Azure Container Registry = your-acr
    - Secret name = `acr-secret`

![VSTS CD](./imgs/VSTS_CD.PNG)

Once this Release succesfully deployed/exececuted and for the purpose of this demo you should manually run this command to initialize properly the database:
```
oc login
kubectl get pods --namespace <your-ocp/k8s-project/namspace>
kubectl exec \
  <name-of-the-sql-pod> \
  /usr/share/wwi-db-setup/init-and-restore-db.sh
```

You could now expose the web dashboard app by creating a `Route` and then hitting it's associated/generated `HOST/PORT`:
```
oc expose svc/dotnetcore --name=dotnetcore --namespace <your-namespace>
oc get route --namespace <your-namespace>
```

*Note: for the purpose of this demo we deployed both images as Ubuntu based images. For production workload on OpenShift/RedHat and for better support, more performance and security, you will modify the based images to target rhel based images.

Furthermore, you should "[Enable Images to Run with USER in the Dockerfile](https://docs.openshift.com/container-platform/3.9/admin_guide/manage_scc.html#enable-images-to-run-with-user-in-the-dockerfile)" per namespace/project to have these images running properly.*

# OSBA

Prerequisities:
- You need an OpenShift Origin or Container Platform cluster
- You need to [install OSBA in your OpenShift cluster](https://github.com/Azure/open-service-broker-azure#openshift-project-template)

From the OCP Service Catalog you should be able to browse and use the different Azure APIs:
- Azure SQL Database
- Azure Cosmos DB
- Azure Database for PostgreSQL
- Azure Database for MySQL
- Azure KeyVault
- Azure Service Bus
- Azure Event Hubs
- Azure Redis Cache
- Azure Search
- Azure Container Instances
- Azure Storage

![OSBA in OCP](./imgs/OSBA_OCP.PNG)

From there you could provision for example an Azure SQL Database (Server + Database). After providing some information like the Azure location, resource group, the plan to use, the firewall rules to set up, etc. you will have the choice to generate and bind the associated `Secret` of this Azure SQL Database which will be provisioned in Azure for you. With this `Secret` info you will be able then to map the different keys within this `Secret` to associated environment variables of your web app container/pod/deployment.

# Resources

- [Install SQL Server 2017 on RedHat 7.4](https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-red-hat?view=sql-server-linux-2017)
- [Enhancing DevOps with SQL Server on Linux](https://alwaysupalwayson.blogspot.com/2018/06/enhancing-devops-with-sql-server-on.html)
- [OpenShift on Azure installation](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/openshift-get-started)
- [Open Service Broker for Azure](https://osba.sh/)
- [Remotely Debug an ASP.NET Core Container Pod on OpenShift with Visual Studio](https://developers.redhat.com/blog/2018/06/13/remotely-debug-asp-net-core-container-pod-on-openshift-with-visual-studio/)
