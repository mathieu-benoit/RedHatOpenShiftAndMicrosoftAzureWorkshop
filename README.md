# Context

TODO

TOC:
- [Context](#context)
- [Docker](#docker)
  - [Database](#database)
  - [Web](#web)
- [VSTS](#vsts)
  - [Build](#build)
  - [Release](#release)
- [OSBA](#osba)

# Docker

## Database

The database is SQL Server 2017 to illustrate its support on Linux and especially on Linux Containers.

Run the Docker image from the public image:
```
docker run \
  -e 'ACCEPT_EULA=Y' \
  -e 'SA_PASSWORD=<sa-password>' \
  -p 1433:1433 \
  --name <container-name> \
  -d mabenoit/my-mssql-linux
```

Open a bash session within this container:
```
docker exec \
  -it <container-name> \
  bash
```

Go to the folder containing the scripts to run and execute them:
```
cd usr/share/wwi-db-setup/
./restore-db.sh SA <sa-password>
./init-db.sh SA <sa-password>
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

TODO TODO

The web application is a simple dashboard to interact with Sql Server 2017 to demonstrate the AutoTuning feature.

This web application is coming from this repository below + few updates with ASP.NET Core 2.0 and some simplifications + Docker support.

https://github.com/Microsoft/sql-server-samples/tree/master/samples/features/automatic-tuning/force-last-good-plan

Build the Docker image locally:
- Git clone
- Docker run

Run the Docker image from the public image:
- Docker run mabenoit/...

# VSTS

## Build

Prerequisities:
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
