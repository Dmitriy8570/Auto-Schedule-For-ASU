# Контроллер домена Active Directory на базе Samba AD DC.
# Используется как тестовый AD для проверки LDAP-аутентификации бэкенда
# (домен TEST.MC, проверка членства в группе через RequiredGroup).
FROM debian:bookworm-slim

ENV DEBIAN_FRONTEND=noninteractive

# Samba AD DC + клиентские/диагностические утилиты (smbclient, ldb-tools, krb5).
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        samba \
        samba-ad-provision \
        samba-dsdb-modules \
        samba-vfs-modules \
        smbclient \
        ldap-utils \
        ldb-tools \
        winbind \
        libnss-winbind \
        krb5-user \
        ca-certificates \
        procps \
        iproute2 \
        dnsutils \
    && rm -rf /var/lib/apt/lists/* \
    # Конфиг по умолчанию мешает provision'у — удаляем, его создаст entrypoint.
    && rm -f /etc/samba/smb.conf

COPY entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

# DNS, Kerberos, LDAP(S), SMB, Global Catalog.
EXPOSE 53 53/udp 88 88/udp 135 389 389/udp 445 464 464/udp 636 3268 3269

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
