# Context

TODO - Intro/Context

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

Prerequesities:
- A **SQL Server 2017 Enterprise on Red Hat Enterprise Linux 7.4 (RHEL)** VM
- Two "Inbound port rule" on the associated "Azure Network Security Group", one for the port 1433 and the other for the port 88 to allow external connections to the web app and to the database endpoint.
- On your local machine, a "[SQL Operations Studio](https://docs.microsoft.com/en-us/sql/sql-operations-studio/download?)" installed

*Note: for more details about the manual installation of SQL Server 2017 over Red Hat Enterprise Linux, please see [here](https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-red-hat?view=sql-server-linux-2017).*

## Database

From within the RHEL74 VM, run the following command:
```
cd TODO
wget https://github.com/Microsoft/sql-server-samples/releases/download/wide-world-importers-v1.0/WideWorldImporters-Full.bak
```

Using SQL Ops Studio on your local machine, connect to SQL Server 2017 on your virtual machine and run:
- this [restore.sql Script](https://raw.githubusercontent.com/erickangMSFT/sqldevops/master/docker_cluster/aks/restore.sql).
- and this [init-db.sql Script](./SqlServerAutoTuningDashboard/SqlScripts/init-db.sh).

## Web

*Note: for now and for the purpose of this demo, you need to have Docker CE installed on this RHEL74 VM.*

```
sudo docker run \
-e 'ConnectionStrings:Wwi=Server=10.1.1.4,1433;Database=WideWorldImporters;User Id=SA;Password=SQL2017R0ck5;' \
-p 88:80 \
--restart \
--name webdashboard1 \
-d mabenoit/sql-autotune-dashboard:latest
```
From your local machine, just point your browser to the URL http://ip_address_of_your_virtual_machine:88/
There is few features to demonstrate from this web dashboard page:
- Click on the Red "Regression" Button to trigger a degredation in performance and notice the impact on the gauge and the number of requests per second.
- Click on the On radio button below the gauge to activate SQL Server 2017's Automatic Tuning capability and notice the impact on the gauge and the number of requests per second that goes back up again automatically!

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
