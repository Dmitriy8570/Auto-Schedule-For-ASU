#!/bin/bash
# Точка входа контейнера контроллера домена.
# При первом запуске разворачивает домен Active Directory (Samba AD DC),
# создаёт группу и одного пользователя, далее просто стартует samba.
set -euo pipefail

# ----- Параметры домена (переопределяются переменными окружения) -----
REALM="${AD_REALM:-TEST.MC}"
DOMAIN="${AD_DOMAIN:-TEST}"
ADMIN_PASSWORD="${AD_ADMIN_PASSWORD:-P@ssw0rd2024!}"

# ----- Группа «Бюро расписаний» -----
# samAccountName латиницей без пробелов — удобно для LDAP-фильтров и RequiredGroup.
GROUP_NAME="${AD_GROUP:-ScheduleBureau}"
GROUP_DESC="${AD_GROUP_DESC:-Бюро расписаний}"

# ----- Учётная запись пользователя -----
USER_NAME="${AD_USER:-i.petrov}"
USER_PASSWORD="${AD_USER_PASSWORD:-Schedule2024!}"
USER_GIVEN="${AD_USER_GIVEN:-Иван}"
USER_SURNAME="${AD_USER_SURNAME:-Петров}"

PROVISION_MARKER="/var/lib/samba/.provisioned"
REALM_LC="$(echo "$REALM" | tr '[:upper:]' '[:lower:]')"

if [ ! -f "$PROVISION_MARKER" ]; then
    echo "[AD] Разворачиваю домен $REALM (NetBIOS: $DOMAIN)..."

    rm -f /etc/samba/smb.conf

    samba-tool domain provision \
        --use-rfc2307 \
        --realm="$REALM" \
        --domain="$DOMAIN" \
        --server-role=dc \
        --dns-backend=SAMBA_INTERNAL \
        --adminpass="$ADMIN_PASSWORD"

    # Kerberos-конфиг, сгенерированный provision'ом, делаем системным.
    cp -f /var/lib/samba/private/krb5.conf /etc/krb5.conf

    echo "[AD] Создаю группу '$GROUP_NAME' ($GROUP_DESC)..."
    samba-tool group add "$GROUP_NAME" --description="$GROUP_DESC"

    echo "[AD] Создаю пользователя '$USER_NAME'..."
    samba-tool user create "$USER_NAME" "$USER_PASSWORD" \
        --given-name="$USER_GIVEN" \
        --surname="$USER_SURNAME" \
        --mail-address="${USER_NAME}@${REALM_LC}"

    # В лабораторной среде пароль не должен истекать.
    samba-tool user setexpiry "$USER_NAME" --noexpiry

    echo "[AD] Добавляю '$USER_NAME' в группу '$GROUP_NAME'..."
    samba-tool group addmembers "$GROUP_NAME" "$USER_NAME"

    touch "$PROVISION_MARKER"
    echo "[AD] Домен развёрнут. Пользователь: ${USER_NAME}@${REALM_LC}, группа: ${GROUP_NAME}."
else
    echo "[AD] Домен уже развёрнут — пропускаю инициализацию."
fi

# Запуск Samba в foreground, чтобы контейнер жил.
exec samba --foreground --no-process-group -s /etc/samba/smb.conf
