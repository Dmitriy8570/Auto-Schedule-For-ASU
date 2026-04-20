import pytest
from selenium import webdriver
from selenium.webdriver.chrome.service import Service
import requests

API_URL = "https://petfriends.skillfactory.ru/api"
EMAIL = "yuritos6@mail.ru"
PASSWORD = "Myxtap6600!!"

@pytest.fixture(scope="session")
def auth_key():
    """Получение API ключа"""
    headers = {"email": EMAIL, "password": PASSWORD}
    response = requests.get(f"{API_URL}/key", headers=headers)

    assert response.status_code == 200
    return response.json()['key']


@pytest.fixture(scope="session")
def driver():
    """Selenium WebDriver"""
    service = Service("./drivers/chromedriver.exe")
    driver = webdriver.Chrome(service=service)
    driver.maximize_window()
    driver.implicitly_wait(10)

    yield driver
    driver.quit()