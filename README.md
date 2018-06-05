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

TODO

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
