FROM mcr.microsoft.com/mssql/server:2017-latest

ARG SA_PASSWORD
ENV SA_PASSWORD=${SA_PASSWORD} 
ARG ACCEPT_EULA
ENV ACCEPT_EULA=${ACCEPT_EULA} 

COPY sql_server/import-data.sh /usr/src/app/
COPY sql_server/setup.sql /usr/src/app/

# Grant permissions for the import-data script to be executable
RUN ( /opt/mssql/bin/sqlservr --accept-eula & ) | grep -q "Service Broker manager has started" \
    && /opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P $SA_PASSWORD -i /usr/src/app/setup.sql \
    && pkill sqlservr 
