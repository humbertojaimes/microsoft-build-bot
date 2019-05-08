echo 'Creating DB'

( /opt/mssql/bin/sqlservr --accept-eula & ) | grep -q "Service Broker manager has started" \
    && /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P 'StrongHJaimes46*' -i /usr/src/app/setup.sql \
    && pkill sqlservr 

echo 'Finish DB'