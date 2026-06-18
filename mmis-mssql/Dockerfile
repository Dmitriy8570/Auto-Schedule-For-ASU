# Образ MS SQL Server 2022, имитирующий исходную БД MMIS.
FROM mcr.microsoft.com/mssql/server:2022-latest

ENV ACCEPT_EULA=Y \
    MSSQL_PID=Developer

USER root

# SQL-скрипты и скрипты инициализации.
COPY sql/ /usr/src/app/sql/
COPY entrypoint.sh configure-db.sh /usr/src/app/

RUN chmod +x /usr/src/app/entrypoint.sh /usr/src/app/configure-db.sh \
    && chown -R mssql:root /usr/src/app

USER mssql
WORKDIR /usr/src/app

# Параллельно: фоновая инициализация БД + сам SQL Server.
ENTRYPOINT ["/bin/bash", "/usr/src/app/entrypoint.sh"]
