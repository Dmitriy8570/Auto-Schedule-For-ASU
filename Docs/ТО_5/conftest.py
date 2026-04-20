import pytest
from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from webdriver_manager.chrome import ChromeDriverManager

@pytest.fixture
def driver():
    """Фикстура для Chrome браузера с автоматической загрузкой драйвера."""
    
    # Автоматически скачивает и устанавливает подходящий ChromeDriver
    service = Service(ChromeDriverManager().install())
    
    # Настройки браузера
    options = webdriver.ChromeOptions()
    options.add_argument("--start-maximized")  # Открывать на весь экран
    options.add_argument("--disable-blink-features=AutomationControlled")
    options.add_experimental_option("excludeSwitches", ["enable-automation"])
    
    # Запуск браузера
    driver = webdriver.Chrome(service=service, options=options)
    driver.implicitly_wait(5)
    
    yield driver
    
    # Закрытие браузера после теста
    driver.quit()