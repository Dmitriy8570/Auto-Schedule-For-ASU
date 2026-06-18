#!/bin/bash
set -e

# Фоновая инициализация (создание схемы и сидинг) после старта сервера.
/usr/src/app/configure-db.sh &

# SQL Server в основном процессе контейнера.
exec /opt/mssql/bin/sqlservr
