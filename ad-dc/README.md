# Active Directory (Samba AD DC)

Контейнер-контроллер домена для проверки LDAP-аутентификации бэкенда.
При первом старте автоматически разворачивает домен и создаёт **одну** учётную
запись в группе «Бюро расписаний».

## Параметры по умолчанию

| Параметр            | Значение                       |
|---------------------|--------------------------------|
| Realm (домен)       | `TEST.MC`                      |
| NetBIOS-домен       | `TEST`                         |
| Администратор       | `Administrator` / `P@ssw0rd2024!` |
| Группа              | `ScheduleBureau` (описание — «Бюро расписаний») |
| Пользователь        | `i.petrov` / `Schedule2024!`   |
| Хост контроллера    | `dc1.test.mc`                  |

Название группы в AD задано латиницей (`ScheduleBureau`) без пробелов — так его
удобно использовать в LDAP-фильтрах и в настройке `RequiredGroup`. Человеко-читаемое
имя «Бюро расписаний» хранится в атрибуте `description`.

Все значения переопределяются переменными окружения (см. `.env.example` в корне:
`AD_REALM`, `AD_DOMAIN`, `AD_GROUP`, `AD_USER`, `AD_USER_PASSWORD` и т.д.).

## Запуск

```bash
docker compose up -d ad-dc
docker compose logs -f ad-dc      # дождаться "Домен развёрнут"
```

## Проверка

```bash
# Список пользователей и групп домена
docker compose exec ad-dc samba-tool user list
docker compose exec ad-dc samba-tool group listmembers ScheduleBureau

# LDAP-поиск пользователя
docker compose exec ad-dc ldbsearch -H /var/lib/samba/private/sam.ldb \
    "(sAMAccountName=i.petrov)" memberOf

# Проверка bind'а (аутентификации) тем же пользователем.
# Контроллер отклоняет simple bind по незашифрованному LDAP
# ("Transport encryption required"), поэтому используем LDAPS (636).
docker compose exec -e LDAPTLS_REQCERT=never ad-dc ldapsearch -x -LLL \
    -H ldaps://localhost -D "i.petrov@test.mc" -w 'Schedule2024!' \
    -b "DC=test,DC=mc" "(sAMAccountName=i.petrov)" sAMAccountName memberOf
```

Подключение бэкенда внутри сети compose: хост `ad-dc` (или `dc1`),
base DN `DC=test,DC=mc`. Для проверки пароля (simple bind) используйте
LDAPS — порт `636`; порт `389` подходит для анонимных/SASL-запросов, но
simple bind по нему запрещён политикой `ldap server require strong auth`.

> Состояние домена хранится в volume'ах `ad-data` и `ad-conf`. Чтобы развернуть
> домен заново: `docker compose down -v` (удалит и остальные данные) либо
> `docker volume rm <project>_ad-data <project>_ad-conf`.
