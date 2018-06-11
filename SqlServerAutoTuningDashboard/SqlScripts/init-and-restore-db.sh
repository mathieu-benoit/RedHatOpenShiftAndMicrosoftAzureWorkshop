#!/bin/bash

#Restore db
cd /var/opt/mssql
mkdir -p backup
cd backup
wget https://github.com/Microsoft/sql-server-samples/releases/download/wide-world-importers-v1.0/WideWorldImporters-Full.bak
wget https://raw.githubusercontent.com/erickangMSFT/sqldevops/master/docker_cluster/aks/restore.sql
/opt/mssql-tools/bin/sqlcmd -U SA -P $SA_PASSWORD -i restore.sql

#Init db
cd /usr/share/wwi-db-setup
/opt/mssql-tools/bin/sqlcmd -U SA -P $SA_PASSWORD -i init-db.sql