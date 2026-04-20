import requests
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import pytest
import time

API_URL = "https://petfriends.skillfactory.ru/api"
EMAIL = "yuritos6@mail.ru"
PASSWORD = "Myxtap6600!!"


def login(driver):
    driver.get("https://petfriends.skillfactory.ru/login")
    wait = WebDriverWait(driver, 10)

    wait.until(EC.visibility_of_element_located((By.ID, "email"))).send_keys(EMAIL)
    driver.find_element(By.ID, "pass").send_keys(PASSWORD)
    driver.find_element(By.CSS_SELECTOR, "button[type='submit']").click()

    wait.until(EC.url_contains("all_pets"))


def get_pet_row_by_name(driver, pet_name):
    """Вспомогательная функция для поиска строки питомца по имени"""
    table_rows = WebDriverWait(driver, 10).until(
        EC.presence_of_all_elements_located((By.CSS_SELECTOR, "table tbody tr"))
    )

    for row in table_rows:
        if pet_name in row.text:
            return row
    return None


# ==================== ЗАДАНИЕ 1 ====================
def test_create_pet_api_then_ui(auth_key, driver):
    """API -> UI: Создание питомца через API и проверка всех полей в UI"""
    unique_suffix = int(time.time())
    pet_data = {
        "name": f"Buddy_API_{unique_suffix}", 
        "animal_type": "dog", 
        "age": "4"
    }
    headers = {"auth_key": auth_key}

    # 1. Создаём питомца через API
    response = requests.post(f"{API_URL}/create_pet_simple", headers=headers, data=pet_data)
    assert response.status_code == 200, f"API создание питомца вернуло {response.status_code}"
    pet = response.json()

    # 2. Заходим в UI
    login(driver)
    driver.get("https://petfriends.skillfactory.ru/my_pets")

    # 3. Ждём таблицу питомцев
    WebDriverWait(driver, 10).until(EC.presence_of_element_located((By.TAG_NAME, "table")))

    # 4. Проверка всех полей
    pet_row = get_pet_row_by_name(driver, pet["name"])
    assert pet_row is not None, f"Питомец {pet['name']} не найден в UI"
    
    row_text = pet_row.text
    assert pet["name"] in row_text, f"Имя {pet['name']} не найдено"
    assert pet["animal_type"] in row_text, f"Тип {pet['animal_type']} не найден"
    assert pet["age"] in row_text, f"Возраст {pet['age']} не найден"
    
    print(f"✓ Задание 1: Питомец {pet['name']} успешно найден в UI со всеми полями")


# ==================== ЗАДАНИЕ 2 ====================
def test_create_pet_ui_then_api(auth_key, driver):
    """UI -> API: Создание питомца через UI и проверка всех полей через API"""
    unique_suffix = int(time.time())
    name = f"UI_Pet_{unique_suffix}"
    animal = "cat"
    age = "5"

    login(driver)
    driver.get("https://petfriends.skillfactory.ru/my_pets")

    # 1. Открываем "Добавить питомца"
    add_button = WebDriverWait(driver, 10).until(
        EC.element_to_be_clickable((By.CSS_SELECTOR, "button.btn.btn-outline-success[data-toggle='modal']"))
    )
    add_button.click()

    # 2. Ждём форму
    WebDriverWait(driver, 10).until(EC.visibility_of_element_located((By.ID, "name")))

    # 3. Заполняем форму
    driver.find_element(By.ID, "name").send_keys(name)
    driver.find_element(By.ID, "animal_type").send_keys(animal)
    driver.find_element(By.ID, "age").send_keys(age)

    # 4. Отправляем форму
    submit_button = WebDriverWait(driver, 10).until(
        EC.element_to_be_clickable((By.CSS_SELECTOR, "button.btn.btn-success"))
    )
    submit_button.click()
    
    time.sleep(1)  # Небольшая пауза для сохранения данных

    # 5. Проверяем через API
    headers = {"auth_key": auth_key}
    response = requests.get(f"{API_URL}/pets", headers=headers)
    assert response.status_code == 200, f"API вернул {response.status_code}"
    pets = response.json()["pets"]

    # Ищем питомца в списке
    match = next((p for p in pets if p["name"] == name), None)
    assert match is not None, "Питомец не найден в API"
    
    # Проверяем корректность ВСЕХ полей, включая id
    assert match["id"] is not None, "ID питомца отсутствует"
    assert match["name"] == name, f"Имя не совпадает: {match['name']} != {name}"
    assert match["animal_type"] == animal, f"Тип не совпадает: {match['animal_type']} != {animal}"
    assert match["age"] == age, f"Возраст не совпадает: {match['age']} != {age}"
    
    print(f"✓ Задание 2: Питомец {name} успешно найден в API")
    print(f"  ID: {match['id']}, Имя: {match['name']}, Тип: {match['animal_type']}, Возраст: {match['age']}")


# ==================== ЗАДАНИЕ 3 ====================
def test_update_pet_age_api_then_ui(auth_key, driver):
    """Изменение возраста через API и проверка в UI"""
    headers = {"auth_key": auth_key}

    # 1. Создаём питомца через API 
    unique_suffix = int(time.time())
    pet_data = {
        "name": f"AgeTest_{unique_suffix}", 
        "animal_type": "dog", 
        "age": "2"
    }
    create = requests.post(f"{API_URL}/create_pet_simple", headers=headers, data=pet_data)
    assert create.status_code == 200
    pet = create.json()
    pet_id = pet["id"]

    # 2. Меняем возраст
    new_age = "10"
    update_data = {"name": pet["name"], "animal_type": pet["animal_type"], "age": new_age}
    update = requests.put(f"{API_URL}/pets/{pet_id}", headers=headers, data=update_data)
    assert update.status_code == 200

    # 3. Проверяем в UI
    login(driver)
    driver.get("https://petfriends.skillfactory.ru/my_pets")
    
    # Обновляем страницу для гарантии
    driver.refresh()

    # Ищем строку питомца по имени
    pet_row = get_pet_row_by_name(driver, pet["name"])
    assert pet_row is not None, "Питомец не найден в UI"
    assert new_age in pet_row.text, f"Возраст питомца не обновился, ожидали {new_age}"
    
    print(f"✓ Задание 3: Возраст питомца {pet['name']} успешно обновлён с 2 на {new_age}")


# ==================== ЗАДАНИЕ 4 ====================
@pytest.mark.parametrize("pet_number", range(1, 11))
def test_create_multiple_pets_performance(auth_key, pet_number):
    """
    Задание 4:
    Параметризованный тест, который:
    1. Создаёт 10 питомцев подряд
    2. Проверяет время ответа API
    3. Убеждается, что система не возвращает 5xx ошибки
    """
    headers = {"auth_key": auth_key}
    unique_suffix = int(time.time())
    pet_data = {
        "name": f"Performance_Pet_{pet_number}_{unique_suffix}",
        "animal_type": f"Type_{pet_number}",
        "age": str(pet_number % 15 + 1)  # Возраст от 1 до 15
    }
    
    # Измеряем время выполнения запроса
    start_time = time.time()
    response = requests.post(f"{API_URL}/create_pet_simple", headers=headers, data=pet_data)
    end_time = time.time()
    
    response_time = (end_time - start_time) * 1000  # в миллисекундах
    
    # Проверяем, что нет 5xx ошибок
    assert response.status_code < 500, f"Сервер вернул ошибку {response.status_code} для питомца #{pet_number}"
    
    # Проверяем, что запрос успешен
    assert response.status_code == 200, f"API вернул {response.status_code} для питомца #{pet_number}"
    
    # Проверяем время ответа (не более 2 секунд)
    assert response_time < 2000, f"Слишком долгий ответ: {response_time:.2f} мс для питомца #{pet_number}"
    
    # Проверяем структуру ответа
    pet = response.json()
    assert "id" in pet, "Ответ не содержит id питомца"
    assert pet["name"] == pet_data["name"], "Имя питомца не совпадает"
    
    print(f"✓ Задание 4 - Питомец #{pet_number}: создан за {response_time:.2f} мс, статус: {response.status_code}")