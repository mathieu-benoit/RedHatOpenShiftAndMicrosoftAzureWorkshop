- [Context](#context)
- [VM](#vm)
- [Ansible](#ansible)
- [Docker](#docker)
- [Azure DevOps, ACR and Helm](#azure-devops-acr-and-helm)
  - [Build](#build)
  - [Release](#release)
- [OSBA](#osba)
- [Resources](#resources)

# Context

This repository has been built to showcase some integrations between RedHat and Microsoft Azure: RHEL75 VM, ASP.NET Core 2.1, SQL Server on Linux, Docker, Azure Container Registry (ACR), OpenShift Container Platform (OCP), Azure DevOps (formerly known as VSTS), Open Service Broker for Azure (OSBA), etc.

Here are the associated presentations we have delivered with this content:
- [Montreal, Canada - September, 19 2018](http://bit.ly/19-09-2018) (in English)
- [Quebec city, Canada - June 14 2018](http://bit.ly/14juin2018) (in French)

The goal is demonstrate a typical flow of a modernization journey: from on-premise, to public cloud IaaS, then going more agile with Containers, to leverage more platform capabilities with Kubernetes and a step further with OCP; and then finally take advantage of your SQL database as a Service:

![Modernization Journey Workflow](./imgs/Modernization_Journey_Workflow.png)

Remark: the intent here is to use as much as possible Kubernetes API (CLI and manifest files) to interact with the OCP cluster to demonstrate how easily you could be up to speed with OCP with your Kubernetes skills. Furthermore, some extra OCP setup and config will be illustrated as needed.

# VM

Prerequisities:
- A **Red Hat Enterprise Linux 7.5 (RHEL)** VM
- ASP.NET Core 2.1 installed
- Two "Inbound port rule" on the associated "Azure Network Security Group", one for the port 1433 and the other for the port 88 to allow external connections to the web app and to the database endpoint.
- On your local machine, a "[SQL Operations Studio](https://docs.microsoft.com/en-us/sql/sql-operations-studio/download?)" installed

From your local machine, connect to the RHEL7 VM using SSH:
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

To setup the database for the purpose of this demo, from within the RHEL7 VM, run the following commands:
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

*Note: This web application is coming from [this repository](https://github.com/Microsoft/sql-server-samples/tree/master/samples/features/automatic-tuning/force-last-good-plan). We migrated it to ASP.NET Core 2.1 and we made some simplifications and customizations, finally we enabled the Docker support.*

Now run the ASP.NET Core application from withing your RHEL7 VM, bu executing this command:
```
cd ~
git clone https://github.com/mathieu-benoit/RedHatOpenShiftAndMicrosoftAzureWorkshop.git
cd RedHatOpenShiftAndMicrosoftAzureWorkshop/SqlServerAutoTuningDashboard
dotnet restore
dotnet build
dotnet run
```

From your local machine, just point your browser to the URL http://rhel_ip_address:88/.

There is few features to demonstrate from this web dashboard page:
- Click on the Red "Regression" Button to trigger a degredation in performance and notice the impact on the gauge and the number of requests per second.
- Click on the On radio button below the gauge to activate SQL Server 2017's Automatic Tuning capability and notice the impact on the gauge and the number of requests per second that goes back up again automatically!

![Dashboard Web App](./imgs/SQL2017AutoTuning.png)

# Ansible

Are you looking for more automation to install SQL Server on your RHEL VM and also creating a SQL Database? Here you are! [Checkout these Ansible/Tower scripts by Michael Lessard](https://github.com/michaellessard/demoTower).

You could also look at [Michael's MSSQL role on Ansible Galaxy](https://galaxy.ansible.com/michaellessard/mssql/).

# Docker

Prerequisities:
- A Docker CE installed on the RHEL7 VM

*Important note: for the purpose of this demo we deployed both images SQL and Web as Ubuntu based images. It works on RHEL. But for production workload on OpenShift/RedHat and for better support, more performance and security compliance, you will modify the based images to target rhel based images.*

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
  -t my-mssql-linux \
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
  -e 'ConnectionStrings:DefaultConnection:Server=FIXME' \
  -e 'ConnectionStrings:DefaultConnection:Port=1433' \
  -e 'ConnectionStrings:DefaultConnection:Database=WideWorldImporters' \
  -e 'ConnectionStrings:DefaultConnection:UserId=SA' \
  -e 'ConnectionStrings:DefaultConnection:Password=<sa-password>' \
  -p 80:80 \
  --name web \
  -d mabenoit/sql-autotune-dashboard:latest
```

From your local machine, just point your browser to the URL http://rhel_ip_address:80/, where we could demonstrate the same features than previously.

*Optional - if you would like you could build the Docker image locally:*
```
cd SqlServerAutoTuningDashboard
docker build \
  -t sql-autotune-dashboard \
  -f Dockerfile-Web \
  .
```

# Azure DevOps, ACR and Helm

In this section you will see how you could build and deploy your web and sql Docker images into your OpenShift Container Platform (OCP) cluster via Azure DevOps.

## Build

[![Build status](https://dev.azure.com/mabenoit-ms/OpenShiftOnAzure/_apis/build/status/SqlAutoTuneDashboard-ACR-CI)](https://dev.azure.com/mabenoit-ms/OpenShiftOnAzure/_build/latest?definitionId=52)

The goal here is to build and push both images: SQL and Web in a private Azure Container Registry via Azure DevOps Pipelines.

Prerequisities:
- An Azure DevOps account and project
- A Connection endpoint in Azure DevOps to your Azure Container Registry (ACR) to be able to push your images built

High level steps:
- Agent queue: `Hosted Linux Preview`
- Docker - Build Web image
- Docker - Push Web image
- Docker - Build Sql image
- Docker - Push Sql image
- Install Helm
- Helm - init --client-only
- Helm - package
- Publish Helm chart as Artifact

See the details of this [build definition in YAML file here](./SqlServerAutoTuningDashboard/AzureDevOps-CI.yml).

![Azure DevOps CI](./imgs/AzureDevOps_CI.PNG)

## Release

PRE-PROD: [![PRE-PROD Status](https://vsrm.dev.azure.com/mabenoit-ms/_apis/public/Release/badge/f2b899c8-a46f-4300-a9fd-cc3bd7f6f15e/4/6)](https://vsrm.dev.azure.com/mabenoit-ms/_apis/public/Release/badge/f2b899c8-a46f-4300-a9fd-cc3bd7f6f15e/4/6)

PROD: [![PROD Status](https://vsrm.dev.azure.com/mabenoit-ms/_apis/public/Release/badge/f2b899c8-a46f-4300-a9fd-cc3bd7f6f15e/4/8)](https://vsrm.dev.azure.com/mabenoit-ms/_apis/public/Release/badge/f2b899c8-a46f-4300-a9fd-cc3bd7f6f15e/4/8)

The goal here is to deploy both images from ACR: SQL and Web on a given OpenShift Cluster via Azure DevOps Pipelines (Release). The first environment `PRE-PROD` will be automatically provisioned in continuous integration/delivery whereas then the `PROD` environment will need manual approval.

![AzureDevOps CD PIPELINE](./imgs/AzureDevOps_CD_PIPELINE.PNG)

Prerequisities:
- An OpenShift Origin or Container Platform cluster
- A Azure DevOps account and project
- A Connection endpoint in Azure DevOps to your OpenShift Kubernetes cluster to be able to deploy your Docker images

**TIPS in OCP**: for the last prerequisities above, *Connection endpoint in Azure DevOps to your OpenShift Kubernetes cluster*, here are the steps to achieve this:
- On your local machine, install the [OpenShift command line interface (CLI)](https://docs.openshift.com/container-platform/3.9/cli_reference/get_started_cli.html)
- Run `oc login <server-url>`, and provide either a token or your username/password
- Get the associated kube config file: `cat ~.kube/config` and keep the entire content, especially the one regarding this specific cluster if you have more than one.
- Go to to your Azure DevOps project and navigate to your `/_admin/_services` page. There, add a new "Kubernetes service endpoint" filling out the `Server URL field` and the `KubeConfig`. You need also to enable the `Accept Untrusted Certificates` checkbox.
  - *Important remark: by default this token will be valid for 24h, so you will have to repeat these 3 previous commands then.*
- If you try out the "Verify connection" action you will get an error, ignore it, and click on the `OK` button

We need to [grant the Kubernetes cluster to have access to the Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-auth-aks). For the purpose of this demonstration, here are the command lines you have to run:
```
ACR=<your-acr-name>
SERVICE_PRINCIPAL_NAME=acr-sp
RegistryLoginServer=$(az acr show -n $ACR --query loginServer --output tsv)
ACR_REGISTRY_ID=$(az acr show -n $ACR --query id --output tsv)
RegistryPassword=$(az ad sp create-for-rbac --name $SERVICE_PRINCIPAL_NAME --role Reader --scopes $ACR_REGISTRY_ID --query password --output tsv)
RegistryUserName=$(az ad sp show --id http://$SERVICE_PRINCIPAL_NAME --query appId --output tsv)
```
You will then map the corresponding `Registry*` values with the variables described just below.

Here is the setup of the Azure DevOps Pipelines (Release) Definition:

Variables:
- RegistryLoginServer = your-acr-name.azurecr.io
- RegistryUserName = your-acr-username-or-sp-clientid
- RegistryPassword = your-acr-password-or-sp-password
- HelmDeploymentName = autotuningdashboard
- K8sNamespace = your-ocp-project
- SqlPassword = your-sql-password

High level steps:
- Install Helm 2.10.0
  - Helm Version Spec = `2.10.0`
  - Check for latest version of Helm = `true`
- Helm - init
  - Command = `init`
  - Upgrade Tiller = `true`
- Helm - install charts
  - Namespace = $(K8sNamespace)
  - Command = `upgrade`
  - Chart Type = `File Path`
  - Chart Path = `$(System.DefaultWorkingDirectory)/**/autotuningdashboard-*.tgz`
  - Release Name = $(HelmDeploymentName)-$(K8sNamespace)
  - Install if release not present = `true`
  - Force = `true`
  - Wait = `true`
  - Arguments = `--set sql.password=$(SqlPassword) --set imageCredentials.registry=$(RegistryLoginServer) --set imageCredentials.username=$(RegistryUserName) --set imageCredentials.password=$(RegistryPassword) --set web.image.tag=$(Build.BuildId)`

![Azure DevOps CD](./imgs/AzureDevOps_CD.PNG)

**TIPS in OCP**: to achieve that and for the purpose of this demo, you should:
- "[Enable Images to Run with `USER` in the `Dockerfile`](https://docs.openshift.com/container-platform/3.9/admin_guide/manage_scc.html#enable-images-to-run-with-user-in-the-dockerfile)" per namespace/project to have these images running properly
- "[Grant `cluster-admin` rights to the `kube-system` namespace’s default `kube-system` service account](https://blog.openshift.com/from-templates-to-openshift-helm-charts/)" - which is need for Tiller to work properly - by running this command: `oc adm policy add-cluster-role-to-user cluster-admin -z default --namespace kube-system`. You could also read [this other article](https://blog.openshift.com/getting-started-helm-openshift/) which mentions how to be granular and only add proper rights per namespace/project.

Once this Release is succesfully deployed/exececuted and for the purpose of this demo you should manually run this command to initialize properly the database:
```
SQL_POD=$(kubectl get pods -l app=sql -n <your-namespace> -o jsonpath='{.items[0].metadata.name}')
kubectl exec \
  $SQL_POD \
  /usr/share/wwi-db-setup/init-and-restore-db.sh
```

**TIPS in OCP**: to be able to publicly browse your web app just deployed you will have to create a `Route` and then hitting it's associated/generated `HOST/PORT`. For that you will have to run these commands:
```
oc expose svc/web --name=web --namespace <your-namespace>
oc get route --namespace <your-namespace>
```

![Deployments in OCP](./imgs/Deployments_OCP.PNG)

# OSBA

Prerequisities:
- An OpenShift Origin or Container Platform cluster
- To [install OSBA in your OpenShift cluster](https://github.com/Azure/open-service-broker-azure#openshift-project-template)

From the OCP Service Catalog you should be able to browse and use the different Azure APIs:

![OSBA in OCP](./imgs/OSBA_OCP.PNG)

From there let's provision an `Azure SQL Database` (Server + Database). After providing some information like the Azure location, the resource group, the plan to use, the firewall rules to set up, etc. you will have the choice to generate and bind the associated `Secret` of this Azure SQL Database which will be provisioned in Azure for you. With this `Secret` info you will be able then to map the different keys within this `Secret` to associated environment variables of your web app deployment:

![OSBA Secrets in OCP](./imgs/OSBA_Secrets_OCP.PNG)

# Resources

- [How to prepare a Red Hat-based virtual machine for Azure](https://azure.microsoft.com/en-us/resources/how-to-prepare-a-red-hat-based-virtual-machine-for-azure)
- [Why switch to SQL Server 2017 on Linux?](https://info.microsoft.com/top-six-reasons-companies-make-the-move-to-sql-server-2017-register.html)
- [Install SQL Server 2017 on RedHat 7](https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-red-hat?view=sql-server-linux-2017)
- [Running Microsoft SQL Server on Red Hat OpenShift](https://developers.redhat.com/blog/2018/09/25/sql-server-on-openshift/)
- [Enhancing DevOps with SQL Server on Linux Container](https://alwaysupalwayson.blogspot.com/2018/06/enhancing-devops-with-sql-server-on.html)
- [OpenShift on Azure installation](https://docs.microsoft.com/en-us/azure/virtual-machines/linux/openshift-get-started)
- [Provisioning OCP on Azure with Ansible](https://galaxy.ansible.com/michaellessard/mssql/)
- [From Templates to Openshift Helm Charts](https://blog.openshift.com/from-templates-to-openshift-helm-charts/)
- [Open Service Broker for Azure](https://osba.sh/)
- [Announcing .NET Core 2.1 for Red Hat Platforms](https://developers.redhat.com/blog/2018/06/14/announcing-net-core-2-1-for-red-hat-platforms/)
- [Remotely Debug an ASP.NET Core Container Pod on OpenShift with Visual Studio](https://developers.redhat.com/blog/2018/06/13/remotely-debug-asp-net-core-container-pod-on-openshift-with-visual-studio/)
- [Securing .NET Core on OpenShift using HTTPS](https://developers.redhat.com/blog/2018/10/12/securing-net-core-on-openshift-using-https/)
- [SQL Server on Linux and Containers labs](https://github.com/Microsoft/sqllinuxlabs)
