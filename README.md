# RedHatOpenShiftAndMicrosoftAzureWorkshop

## Context

## Database

The database is SQL Server 2017 to illustrate its support on Linux and especially on Linux Containers.

Build the Docker image locally:
- Git clone
- Docker build

Run the Docker image from the public image:
- Docker run mabenoit/...

`kubectl exec -ti sql bash`

## Web

The web application is a simple dashboard to interact with Sql Server 2017 to demonstrate the AutoTuning feature.

This web application is coming from this repository below + few updates with ASP.NET Core 2.0 and some simplifications + Docker support.

https://github.com/Microsoft/sql-server-samples/tree/master/samples/features/automatic-tuning/force-last-good-plan

Build the Docker image locally:
- Git clone
- Docker run

Run the Docker image from the public image:
- Docker run mabenoit/...

## OpenShift

TODO

## VSTS

TODO

## OSBA

TODO
