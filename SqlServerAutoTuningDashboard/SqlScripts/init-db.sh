USER=$1
PASSWORD=$2
/opt/mssql-tools/bin/sqlcmd -U $USER -P $PASSWORD -i init-db.sql