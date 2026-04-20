"""

Структура тестов
────────────────
  Блок 1 (TC-01–04)  — GET /api/key               Аутентификация
  Блок 2 (TC-05–08)  — GET /api/pets               Получение списка питомцев
  Блок 3 (TC-09–14)  — POST /api/create_pet_simple Создание без фото
  Блок 4 (TC-15–16)  — POST /api/pets              Создание с фото
  Блок 5 (TC-17–18)  — POST /api/pets/set_photo    Загрузка фото
  Блок 6 (TC-19)     — PUT  /api/pets/{id}         Обновление питомца
  Блок 7 (TC-20)     — DELETE /api/pets/{id}       Удаление питомца

Запуск:
  py -m pytest test_api.py -v
  py -m pytest test_api.py -v -m "positive"
  py -m pytest test_api.py -v -m "negative"
  py -m pytest test_api.py -v -r xfailed        # показать детали xfail

Примечания о поведении сервера
───────────────────────────────
  • При ошибках (403, 400, 500) сервер возвращает HTML, а не JSON.
    api.py обрабатывает это через _parse_response().
  • TC-11, TC-12, TC-13 помечены @pytest.mark.xfail: сервер принимает
    невалидные данные и возвращает 200 — это задокументированные дефекты API.
  • TC-06: поле user_id отсутствует в ответе my_pets — проверяем только
    структуру и HTTP-статус.
  • TC-08: сервер возвращает 500 при неверном фильтре — тест ожидает именно это.
"""

import os
import pytest
from api import PetFriends

# ═══════════════════════════════════════════════════════════════════════════════
# Конфигурация — замените на реальные учётные данные перед запуском
# ═══════════════════════════════════════════════════════════════════════════════

VALID_EMAIL    = "fds228@agu.ru" 
VALID_PASSWORD = "lbvf2004"

WRONG_EMAIL    = "nonexistent@fake.xyz"
WRONG_PASSWORD = "totally_wrong_pass_123"
WRONG_AUTH_KEY = "0" * 56                 # заведомо неверный ключ (56 нулей)

# Путь к тестовому изображению (JPG/PNG, небольшой файл)
TEST_PHOTO_PATH = os.path.join(os.path.dirname(__file__), "test_pet.jpg")

# ═══════════════════════════════════════════════════════════════════════════════
# Фикстуры
# ═══════════════════════════════════════════════════════════════════════════════

@pytest.fixture(scope="session")
def pf() -> PetFriends:
    """Создаёт единственный экземпляр клиента API на всю сессию."""
    return PetFriends()


@pytest.fixture(scope="session")
def auth_key(pf: PetFriends) -> str:
    """
    Получает валидный API-ключ один раз для всей тестовой сессии.
    Все тесты, которым нужна авторизация, используют именно эту фикстуру.
    """
    status, result = pf.get_api_key(VALID_EMAIL, VALID_PASSWORD)
    assert status == 200, (
        f"Не удалось получить auth_key при подготовке сессии. "
        f"Статус: {status}, тело: {result}"
    )
    return result["key"]


@pytest.fixture()
def temp_pet(pf: PetFriends, auth_key: str) -> dict:
    """
    Создаёт временного питомца перед тестом и удаляет его после.
    Используется там, где нужен существующий питомец (обновление, фото, удаление).
    """
    status, pet = pf.create_pet_simple(
        auth_key=auth_key,
        name="Временный",
        animal_type="Тест",
        age="1",
    )
    assert status == 200, f"Не удалось создать временного питомца: {status}"
    yield pet
    # Teardown — удаляем питомца даже если тест упал
    pf.delete_pet(auth_key=auth_key, pet_id=pet["id"])


# ═══════════════════════════════════════════════════════════════════════════════
# БЛОК 1 · Аутентификация  GET /api/key
# ═══════════════════════════════════════════════════════════════════════════════

class TestGetApiKey:
    """Тесты эндпоинта GET /api/key."""

    # ── Тест 1 ─────────────────────────────────────────────────────────────────
    @pytest.mark.positive
    def test_get_api_key_valid_credentials(self, pf: PetFriends):
        """
        [TC-01] Получение API-ключа с корректными учётными данными.

        Предусловия: зарегистрированный пользователь.
        Входные данные: валидные email и password.
        Ожидаемый результат:
            • HTTP 200
            • тело содержит поле "key"
            • значение "key" — непустая строка
        """
        status, result = pf.get_api_key(VALID_EMAIL, VALID_PASSWORD)

        assert status == 200, f"Ожидался статус 200, получен {status}"
        assert isinstance(result, dict), f"Ответ должен быть JSON-объектом, получен: {result}"
        assert "key" in result, f"Поле 'key' отсутствует в ответе: {result}"
        assert isinstance(result["key"], str) and len(result["key"]) > 0, (
            "Поле 'key' должно быть непустой строкой"
        )

    # ── Тест 2 ─────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    def test_get_api_key_wrong_password(self, pf: PetFriends):
        """
        [TC-02] Получение API-ключа с неверным паролем.

        Входные данные: корректный email, неверный пароль.
        Ожидаемый результат:
            • HTTP 403
        Примечание: сервер возвращает HTML при 403, не JSON — api.py обрабатывает это.
        """
        status, _ = pf.get_api_key(VALID_EMAIL, WRONG_PASSWORD)

        assert status == 403, (
            f"При неверном пароле ожидался статус 403, получен {status}"
        )

    # ── Тест 3 ─────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    def test_get_api_key_wrong_email(self, pf: PetFriends):
        """
        [TC-03] Получение API-ключа с несуществующим e-mail.

        Входные данные: несуществующий email, любой пароль.
        Ожидаемый результат:
            • HTTP 403
        """
        status, _ = pf.get_api_key(WRONG_EMAIL, VALID_PASSWORD)

        assert status == 403, (
            f"При несуществующем email ожидался статус 403, получен {status}"
        )

    # ── Тест 4 ─────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    def test_get_api_key_empty_credentials(self, pf: PetFriends):
        """
        [TC-04] Получение API-ключа с пустыми учётными данными (граничное значение).

        Входные данные: пустые строки для email и password.
        Ожидаемый результат:
            • HTTP 403 — пустые данные не должны проходить аутентификацию.
        """
        status, _ = pf.get_api_key("", "")

        assert status == 403, (
            f"При пустых учётных данных ожидался статус 403, получен {status}"
        )


# ═══════════════════════════════════════════════════════════════════════════════
# БЛОК 2 · Список питомцев  GET /api/pets
# ═══════════════════════════════════════════════════════════════════════════════

class TestGetListOfPets:
    """Тесты эндпоинта GET /api/pets."""

    # ── Тест 5 ─────────────────────────────────────────────────────────────────
    @pytest.mark.positive
    def test_get_all_pets_success(self, pf: PetFriends, auth_key: str):
        """
        [TC-05] Получение полного списка питомцев без фильтра.

        Входные данные: валидный auth_key, filter = "" (не передаётся).
        Ожидаемый результат:
            • HTTP 200
            • тело содержит ключ "pets"
            • значение "pets" является списком
        """
        status, result = pf.get_list_of_pets(auth_key, filter="")

        assert status == 200, f"Ожидался статус 200, получен {status}"
        assert isinstance(result, dict), f"Ответ должен быть JSON-объектом, получен: {result}"
        assert "pets" in result, f"Ключ 'pets' отсутствует в ответе: {result}"
        assert isinstance(result["pets"], list), (
            f"'pets' должен быть списком, получен {type(result['pets'])}"
        )

    # ── Тест 6 ─────────────────────────────────────────────────────────────────
    @pytest.mark.positive
    def test_get_my_pets_filter(self, pf: PetFriends, auth_key: str):
        """
        [TC-06] Получение списка питомцев с фильтром "my_pets".

        Входные данные: валидный auth_key, filter = "my_pets".
        Ожидаемый результат:
            • HTTP 200
            • тело содержит ключ "pets" со списком
        Примечание: поле user_id отсутствует в ответе сервера при использовании
        фильтра — фильтрация по владельцу происходит на стороне сервера.
        """
        status, result = pf.get_list_of_pets(auth_key, filter="my_pets")

        assert status == 200, f"Ожидался статус 200, получен {status}"
        assert isinstance(result, dict), f"Ответ должен быть JSON-объектом, получен: {result}"
        assert "pets" in result, f"Ключ 'pets' отсутствует в ответе: {result}"
        assert isinstance(result["pets"], list), "'pets' должен быть списком"

    # ── Тест 7 ─────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    def test_get_pets_invalid_auth_key(self, pf: PetFriends):
        """
        [TC-07] Получение списка питомцев с неверным auth_key.

        Входные данные: заведомо неверный auth_key.
        Ожидаемый результат:
            • HTTP 403
        Примечание: сервер возвращает HTML при 403 — api.py обрабатывает это.
        """
        status, _ = pf.get_list_of_pets(WRONG_AUTH_KEY)

        assert status == 403, (
            f"При неверном auth_key ожидался статус 403, получен {status}"
        )

    # ── Тест 8 ─────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    def test_get_pets_invalid_filter_value(self, pf: PetFriends, auth_key: str):
        """
        [TC-08] Получение списка питомцев с неподдерживаемым значением фильтра.

        Входные данные: валидный auth_key, filter = "unknown_filter".
        Ожидаемый результат:
            • HTTP 500 — сервер возвращает "Filter value is incorrect"
        Примечание: сервер возвращает HTML при 500 — api.py обрабатывает это.
        """
        status, _ = pf.get_list_of_pets(auth_key, filter="unknown_filter")

        assert status == 500, (
            f"При неверном фильтре ожидался статус 500, получен {status}"
        )


# ═══════════════════════════════════════════════════════════════════════════════
# БЛОК 3 · Добавление питомца без фото  POST /api/create_pet_simple
# ═══════════════════════════════════════════════════════════════════════════════

class TestCreatePetSimple:
    """Тесты эндпоинта POST /api/create_pet_simple."""

    # ── Тест 9 ─────────────────────────────────────────────────────────────────
    @pytest.mark.positive
    def test_create_pet_simple_success(self, pf: PetFriends, auth_key: str):
        """
        [TC-09] Создание питомца без фото с корректными данными.

        Входные данные: валидный auth_key, name="Барсик", animal_type="Кот", age="3".
        Ожидаемый результат:
            • HTTP 200
            • тело содержит поля: id, name, animal_type, age
            • name и animal_type совпадают с переданными
        Постусловие: созданный питомец удаляется.
        """
        status, result = pf.create_pet_simple(
            auth_key=auth_key,
            name="Барсик",
            animal_type="Кот",
            age="3",
        )

        assert status == 200, f"Ожидался статус 200, получен {status}"
        assert isinstance(result, dict), f"Ответ должен быть JSON-объектом: {result}"
        for field in ("id", "name", "animal_type", "age"):
            assert field in result, f"Поле '{field}' отсутствует в ответе"
        assert result["name"] == "Барсик", f"name не совпадает: {result['name']}"
        assert result["animal_type"] == "Кот", f"animal_type не совпадает: {result['animal_type']}"

        pf.delete_pet(auth_key=auth_key, pet_id=result["id"])

    # ── Тест 10 ────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    def test_create_pet_simple_invalid_auth(self, pf: PetFriends):
        """
        [TC-10] Создание питомца с неверным auth_key.

        Входные данные: неверный auth_key, иначе корректные данные.
        Ожидаемый результат:
            • HTTP 403
        """
        status, _ = pf.create_pet_simple(
            auth_key=WRONG_AUTH_KEY,
            name="Призрак",
            animal_type="Кот",
            age="1",
        )

        assert status == 403, (
            f"При неверном auth_key ожидался статус 403, получен {status}"
        )

    # ── Тест 11 ────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    @pytest.mark.xfail(
        reason="Дефект API: сервер принимает пустое имя и возвращает 200 вместо 400"
    )
    def test_create_pet_simple_empty_name(self, pf: PetFriends, auth_key: str):
        """
        [TC-11] Создание питомца с пустым именем (граничное значение).

        Входные данные: name = "" (пустая строка).
        Ожидаемый результат по спецификации:
            • HTTP 400 — name является обязательным полем.
        Фактическое поведение сервера:
            • HTTP 200 — сервер не валидирует обязательность поля.
            Это задокументированный дефект; тест помечен xfail.
        """
        status, result = pf.create_pet_simple(
            auth_key=auth_key,
            name="",
            animal_type="Кот",
            age="2",
        )

        if isinstance(result, dict) and "id" in result:
            pf.delete_pet(auth_key=auth_key, pet_id=result["id"])

        assert status == 400, (
            f"При пустом name ожидался статус 400, получен {status}"
        )

    # ── Тест 12 ────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    @pytest.mark.xfail(
        reason="Дефект API: сервер принимает отрицательный возраст и возвращает 200 вместо 400"
    )
    def test_create_pet_simple_negative_age(self, pf: PetFriends, auth_key: str):
        """
        [TC-12] Создание питомца с отрицательным возрастом (граничное значение).

        Входные данные: age = "-1".
        Ожидаемый результат по спецификации:
            • HTTP 400 — отрицательный возраст семантически некорректен.
        Фактическое поведение сервера:
            • HTTP 200 — сервер не валидирует диапазон значений.
            Это задокументированный дефект; тест помечен xfail.
        """
        status, result = pf.create_pet_simple(
            auth_key=auth_key,
            name="НегативВозраст",
            animal_type="Собака",
            age="-1",
        )

        if isinstance(result, dict) and "id" in result:
            pf.delete_pet(auth_key=auth_key, pet_id=result["id"])

        assert status == 400, (
            f"При отрицательном возрасте ожидался статус 400, получен {status}"
        )

    # ── Тест 13 ────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    @pytest.mark.xfail(
        reason="Дефект API: сервер принимает строку вместо числа в поле age и возвращает 200 вместо 400"
    )
    def test_create_pet_simple_age_as_string(self, pf: PetFriends, auth_key: str):
        """
        [TC-13] Создание питомца с нечисловым значением возраста (граничное значение).

        Входные данные: age = "abc" (строка вместо числа).
        Ожидаемый результат по спецификации:
            • HTTP 400 — поле age ожидает тип number.
        Фактическое поведение сервера:
            • HTTP 200 — сервер не валидирует тип поля.
            Это задокументированный дефект; тест помечен xfail.
        """
        status, result = pf.create_pet_simple(
            auth_key=auth_key,
            name="СтрАge",
            animal_type="Хомяк",
            age="abc",
        )

        if isinstance(result, dict) and "id" in result:
            pf.delete_pet(auth_key=auth_key, pet_id=result["id"])

        assert status == 400, (
            f"При нечисловом возрасте ожидался статус 400, получен {status}"
        )

    # ── Тест 14 ────────────────────────────────────────────────────────────────
    @pytest.mark.positive
    def test_create_pet_simple_very_long_name(self, pf: PetFriends, auth_key: str):
        """
        [TC-14] Создание питомца с очень длинным именем (граничное значение).

        Входные данные: name — строка из 255 символов.
        Ожидаемый результат:
            • HTTP 200 или 400 — оба варианта допустимы.
            Тест фиксирует фактическое поведение; падение на исключении недопустимо.
        Постусловие: питомец удаляется при успешном создании.
        """
        long_name = "А" * 255
        status, result = pf.create_pet_simple(
            auth_key=auth_key,
            name=long_name,
            animal_type="Черепаха",
            age="10",
        )

        assert status in (200, 400), (
            f"Ожидался статус 200 или 400, получен {status}"
        )
        if status == 200 and isinstance(result, dict) and "id" in result:
            pf.delete_pet(auth_key=auth_key, pet_id=result["id"])


# ═══════════════════════════════════════════════════════════════════════════════
# БЛОК 4 · Добавление питомца с фото  POST /api/pets
# ═══════════════════════════════════════════════════════════════════════════════

class TestAddNewPet:
    """Тесты эндпоинта POST /api/pets."""

    # ── Тест 15 ────────────────────────────────────────────────────────────────
    @pytest.mark.positive
    def test_add_new_pet_with_photo_success(self, pf: PetFriends, auth_key: str):
        """
        [TC-15] Создание питомца с корректным фото.

        Предусловие: файл TEST_PHOTO_PATH существует и является валидным JPG/PNG.
        Ожидаемый результат:
            • HTTP 200
            • поле pet_photo непустое (содержит base64-строку)
        Постусловие: созданный питомец удаляется.
        """
        if not os.path.isfile(TEST_PHOTO_PATH):
            pytest.skip(f"Тестовое фото не найдено: {TEST_PHOTO_PATH}")

        status, result = pf.add_new_pet(
            auth_key=auth_key,
            name="ФотоПёс",
            animal_type="Лабрадор",
            age="4",
            pet_photo=TEST_PHOTO_PATH,
        )

        assert status == 200, f"Ожидался статус 200, получен {status}"
        assert isinstance(result, dict), f"Ответ должен быть JSON-объектом: {result}"
        assert result.get("pet_photo", "") != "", (
            "Поле 'pet_photo' не должно быть пустым после загрузки фото"
        )

        pf.delete_pet(auth_key=auth_key, pet_id=result["id"])

    # ── Тест 16 ────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    def test_add_new_pet_invalid_auth(self, pf: PetFriends):
        """
        [TC-16] Создание питомца с фото при неверном auth_key.

        Ожидаемый результат:
            • HTTP 403
        Примечание: сервер возвращает HTML при 403 — api.py обрабатывает это.
        """
        if not os.path.isfile(TEST_PHOTO_PATH):
            pytest.skip(f"Тестовое фото не найдено: {TEST_PHOTO_PATH}")

        status, _ = pf.add_new_pet(
            auth_key=WRONG_AUTH_KEY,
            name="НеАвторизован",
            animal_type="Кот",
            age="1",
            pet_photo=TEST_PHOTO_PATH,
        )

        assert status == 403, (
            f"При неверном auth_key ожидался статус 403, получен {status}"
        )


# ═══════════════════════════════════════════════════════════════════════════════
# БЛОК 5 · Загрузка фото  POST /api/pets/set_photo/{pet_id}
# ═══════════════════════════════════════════════════════════════════════════════

class TestSetPhoto:
    """Тесты эндпоинта POST /api/pets/set_photo/{pet_id}."""

    # ── Тест 17 ────────────────────────────────────────────────────────────────
    @pytest.mark.positive
    def test_set_photo_success(
        self, pf: PetFriends, auth_key: str, temp_pet: dict
    ):
        """
        [TC-17] Загрузка фото к существующему питомцу.

        Предусловие: питомец без фото создан фикстурой temp_pet.
        Ожидаемый результат:
            • HTTP 200
            • поле pet_photo в ответе непустое
        """
        if not os.path.isfile(TEST_PHOTO_PATH):
            pytest.skip(f"Тестовое фото не найдено: {TEST_PHOTO_PATH}")

        status, result = pf.set_photo(
            auth_key=auth_key,
            pet_id=temp_pet["id"],
            pet_photo=TEST_PHOTO_PATH,
        )

        assert status == 200, f"Ожидался статус 200, получен {status}"
        assert isinstance(result, dict), f"Ответ должен быть JSON-объектом: {result}"
        assert result.get("pet_photo", "") != "", (
            "Поле 'pet_photo' должно быть заполнено после set_photo"
        )

    # ── Тест 18 ────────────────────────────────────────────────────────────────
    @pytest.mark.negative
    def test_set_photo_nonexistent_pet(self, pf: PetFriends, auth_key: str):
        """
        [TC-18] Загрузка фото к несуществующему питомцу.

        Входные данные: pet_id, которого нет в базе данных.
        Ожидаемый результат:
            • HTTP 400, 404 или 500 — питомец не найден.
        Примечание: сервер возвращает HTML при 500 — api.py обрабатывает это.
        """
        if not os.path.isfile(TEST_PHOTO_PATH):
            pytest.skip(f"Тестовое фото не найдено: {TEST_PHOTO_PATH}")

        fake_id = "00000000-0000-0000-0000-000000000000"
        status, _ = pf.set_photo(
            auth_key=auth_key,
            pet_id=fake_id,
            pet_photo=TEST_PHOTO_PATH,
        )

        assert status in (400, 404, 500), (
            f"При несуществующем pet_id ожидался 400/404/500, получен {status}"
        )


# ═══════════════════════════════════════════════════════════════════════════════
# БЛОК 6 · Обновление питомца  PUT /api/pets/{pet_id}
# ═══════════════════════════════════════════════════════════════════════════════

class TestUpdatePet:
    """Тесты эндпоинта PUT /api/pets/{pet_id}."""

    # ── Тест 19 ────────────────────────────────────────────────────────────────
    @pytest.mark.positive
    def test_update_pet_success(
        self, pf: PetFriends, auth_key: str, temp_pet: dict
    ):
        """
        [TC-19] Успешное обновление данных питомца.

        Предусловие: питомец создан фикстурой temp_pet.
        Ожидаемый результат:
            • HTTP 200
            • поля name и animal_type в ответе совпадают с переданными
        """
        new_name        = "ОбновлённыйПёс"
        new_animal_type = "Доберман"
        new_age         = "5"

        status, result = pf.update_pet(
            auth_key=auth_key,
            pet_id=temp_pet["id"],
            name=new_name,
            animal_type=new_animal_type,
            age=new_age,
        )

        assert status == 200, f"Ожидался статус 200, получен {status}"
        assert isinstance(result, dict), f"Ответ должен быть JSON-объектом: {result}"
        assert result.get("name") == new_name, (
            f"name не обновился: ожидалось '{new_name}', получено '{result.get('name')}'"
        )
        assert result.get("animal_type") == new_animal_type, (
            f"animal_type не обновился: ожидалось '{new_animal_type}'"
        )


# ═══════════════════════════════════════════════════════════════════════════════
# БЛОК 7 · Удаление питомца  DELETE /api/pets/{pet_id}
# ═══════════════════════════════════════════════════════════════════════════════

class TestDeletePet:
    """Тесты эндпоинта DELETE /api/pets/{pet_id}."""

    # ── Тест 20 ────────────────────────────────────────────────────────────────
    @pytest.mark.positive
    def test_delete_pet_success(self, pf: PetFriends, auth_key: str):
        """
        [TC-20] Успешное удаление питомца из базы данных.

        Шаги:
            1. Создаём питомца через create_pet_simple.
            2. Удаляем его через delete_pet.
            3. Запрашиваем список "my_pets" и проверяем, что питомца нет.
        Ожидаемый результат:
            • DELETE возвращает HTTP 200
            • В последующем GET /api/pets?filter=my_pets питомец отсутствует
        """
        _, pet = pf.create_pet_simple(
            auth_key=auth_key,
            name="НаУдаление",
            animal_type="Попугай",
            age="2",
        )
        pet_id = pet["id"]

        del_status, _ = pf.delete_pet(auth_key=auth_key, pet_id=pet_id)
        assert del_status == 200, (
            f"Ожидался статус 200 при удалении, получен {del_status}"
        )

        _, pets_result = pf.get_list_of_pets(auth_key=auth_key, filter="my_pets")
        ids = [p["id"] for p in pets_result.get("pets", [])]
        assert pet_id not in ids, (
            f"Питомец {pet_id} всё ещё присутствует в списке после удаления"
        )


# ═══════════════════════════════════════════════════════════════════════════════
# ► КОНЕЦ ФАЙЛА — все 20 тест-кейсов реализованы
#
#   Запуск всех тестов:           py -m pytest test_api.py -v
#   Только позитивные:            py -m pytest test_api.py -v -m positive
#   Только негативные:            py -m pytest test_api.py -v -m negative
#   Включая xfail-детали:         py -m pytest test_api.py -v -r xfailed
# ═══════════════════════════════════════════════════════════════════════════════
