#!/bin/bash
set -e

# Путь к sqlcmd: в образе 2022 это mssql-tools18, оставляем запасной вариант.
SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
[ -x "$SQLCMD" ] || SQLCMD="/opt/mssql-tools/bin/sqlcmd"

PASS="${MSSQL_SA_PASSWORD:-$SA_PASSWORD}"

# Ждём готовности SQL Server принимать подключения (-C доверяем сертификату).
echo "MMIS init: ожидание запуска SQL Server..."
for i in $(seq 1 90); do
  if "$SQLCMD" -S localhost -U sa -P "$PASS" -C -l 1 -Q "SELECT 1" >/dev/null 2>&1; then
    echo "MMIS init: SQL Server готов."
    break
  fi
  sleep 2
done

# Применяем скрипты по порядку (-b — прерываться на ошибке, -f 65001 — UTF-8).
for f in /usr/src/app/sql/*.sql; do
  echo "MMIS init: выполняется $(basename "$f")"
  "$SQLCMD" -S localhost -U sa -P "$PASS" -C -b -f 65001 -i "$f"
done

echo "MMIS init: инициализация завершена."
