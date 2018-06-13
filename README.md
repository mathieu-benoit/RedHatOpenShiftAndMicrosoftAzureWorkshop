# Context

This repository has been built to showcase some integrations between RedHat and Microsoft Azure: RHEL74 VM, .NET Core, SQL Server on Linux, Docker, Azure Container Registry (ACR), OpenShift Container Platform (OCP), Visual Studio Team Services (VSTS), Open Service Broker for Azure (OSBA), etc.

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

High level steps:
- Replace tokens in **/*.yml
  - Root directory = `$(System.DefaultWorkingDirectory)/SqlAutoTuneDashboard-ACR-CI/k8s-ymls/SqlServerAutoTuningDashboard`
  - Target files = `**/*.yml`
- kubectl apply - sql
  - Command = `apply`
  - Arguments = `-f $(System.DefaultWorkingDirectory)/SqlAutoTuneDashboard-ACR-CI/k8s-ymls/SqlServerAutoTuningDashboard/k8s-sql.yml`
  - Secrets
    - Azure Container Registry = your-acr
    - Secret name = `acr-secret`
- kubectl apply - web
  - Command = `apply`
  - Arguments = `-f $(System.DefaultWorkingDirectory)/SqlAutoTuneDashboard-ACR-CI/k8s-ymls/SqlServerAutoTuningDashboard/k8s-web.yml`
  - Secrets
    - Azure Container Registry = your-acr
    - Secret name = `acr-secret`

![VSTS CD](./imgs/VSTS_CD.PNG)

Once this Release succesfully deployed/exececuted and for the purpose of this demo you should manually run this command to initialize properly the database:
```
kubectl get pods
kubectl exec \
  <name-of-the-sql-pod> \
  /usr/share/wwi-db-setup/init-and-restore-db.sh
```

You could then browse the web dashboard app by its public `EXTERNAL IP` retrieved from:
```
kubectl get svc
```

# OSBA

Prerequisities:
- You need an OpenShift Origin or Container Platform cluster
- You need to [install OSBA on your OpenShift Kubernetes cluster](https://github.com/Azure/open-service-broker-azure/blob/master/docs/quickstart-aks.md)


# Resources

- [Install SQL Server 2017 on RedHat 7.4](https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-red-hat?view=sql-server-linux-2017)
- [Enhancing DevOps with SQL Server on Linux](https://alwaysupalwayson.blogspot.com/2018/06/enhancing-devops-with-sql-server-on.html)
- [OpenShift on Azure installation](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/openshift-get-started)
- [Open Service Broker for Azure](https://osba.sh/)
