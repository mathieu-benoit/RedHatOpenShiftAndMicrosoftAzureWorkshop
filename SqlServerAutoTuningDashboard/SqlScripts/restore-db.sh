USER=$1
PASSWORD=$2
cd /var/opt/mssql
mkdir -p backup
cd backup
wget https://github.com/Microsoft/sql-server-samples/releases/download/wide-world-importers-v1.0/WideWorldImporters-Full.bak
wget https://raw.githubusercontent.com/erickangMSFT/sqldevops/master/docker_cluster/aks/restore.sql
/opt/mssql-tools/bin/sqlcmd -U $USER -P $PASSWORD -i restore.sql