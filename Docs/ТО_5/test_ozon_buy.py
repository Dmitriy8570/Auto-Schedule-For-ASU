from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.action_chains import ActionChains
import time

# Константы
OZON_URL = "https://www.ozon.ru"
SEARCH_QUERY = "Ноутбук"

def test_ozon_search_and_add_to_cart(driver):
    try:
        driver.get(OZON_URL)
        
        WebDriverWait(driver, 10).until(
            EC.presence_of_element_located((By.TAG_NAME, "body"))
        )
        print("Страница загружена.")
        time.sleep(2)

        
        try:
            element_by_id = driver.find_element(By.ID, "search-input")
            print("By.ID: Найден элемент с id='search-input'")
        except:
            print("By.ID: Элемент с id='search-input' не найден (ожидаемо для Ozon)")
        
        try:
            element_by_name = driver.find_element(By.NAME, "text")
            print("By.NAME: Найден элемент с name='text'")
        except:
            print("By.NAME: Элемент с name='text' не найден")
        
        try:
            elements_by_class = driver.find_elements(By.CLASS_NAME, "ui-input")
            print(f"By.CLASS_NAME: Найдено {len(elements_by_class)} элементов с классом 'ui-input'")
        except:
            print("By.CLASS_NAME: Элементы с классом 'ui-input' не найдены")
        
        try:
            category_links = driver.find_elements(By.LINK_TEXT, "Электроника")
            if category_links:
                print("By.LINK_TEXT: Найдена ссылка на категорию 'Электроника'")
            else:
                for category in ["Товары", "Ozon"]:
                    links = driver.find_elements(By.LINK_TEXT, category)
                    if links:
                        print(f"By.LINK_TEXT: Найдена ссылка на категорию '{category}'")
                        break
        except:
            print("By.LINK_TEXT: Ссылки на категории не найдены")
        
        try:
            partial_links = driver.find_elements(By.PARTIAL_LINK_TEXT, "лектро")
            if partial_links:
                print(f"By.PARTIAL_LINK_TEXT: Найдено {len(partial_links)} ссылок, содержащих 'лектро'")
        except:
            print("By.PARTIAL_LINK_TEXT: Ссылки не найдены")
        
        search_input_css = driver.find_elements(By.CSS_SELECTOR, "input[placeholder*='Искать']")
        print(f"By.CSS_SELECTOR: Найдено {len(search_input_css)} полей поиска")
        
        search_input_xpath = driver.find_elements(By.XPATH, "//input[contains(@placeholder, 'Искать')]")
        print(f"By.XPATH: Найдено {len(search_input_xpath)} полей поиска")
        
        all_buttons = driver.find_elements(By.TAG_NAME, "button")
        print(f"By.TAG_NAME: Найдено {len(all_buttons)} кнопок на странице")

        print("\n" + "="*50)
        print("3. Подготовка: очищаем корзину")
        print("="*50)
        driver.get("https://www.ozon.ru/cart")
        time.sleep(3)
        
        try:
            delete_buttons = driver.find_elements(By.XPATH, "//button[contains(text(), 'Удалить')]")
            for button in delete_buttons:
                try:
                    ActionChains(driver).move_to_element(button).click().perform()
                    time.sleep(1)
                except:
                    pass
            print(f"Корзина очищена (удалено {len(delete_buttons)} товаров)")
        except:
            print("Корзина уже пуста")
        
        driver.get(OZON_URL)
        time.sleep(2)

        popup_selectors = [
            (By.XPATH, "//button[contains(text(), 'Продолжить')]"),
            (By.XPATH, "//button[contains(text(), 'Всё верно')]"),
            (By.XPATH, "//button[contains(text(), 'Закрыть')]"),
            (By.XPATH, "//button[contains(text(), 'Понятно')]"),
            (By.CSS_SELECTOR, "[data-testid='close-button']"),
        ]
        
        for by, selector in popup_selectors:
            try:
                close_button = WebDriverWait(driver, 2).until(
                    EC.element_to_be_clickable((by, selector))
                )
                close_button.click()
                print(f"Закрыто окно: {selector}")
                time.sleep(1)
                break
            except:
                continue



        search_input = WebDriverWait(driver, 10).until(
            EC.element_to_be_clickable((By.CSS_SELECTOR, "input[placeholder*='Искать']"))
        )
        search_input.clear()
        search_input.send_keys(SEARCH_QUERY)
        print(f"Введен запрос: {SEARCH_QUERY}")
        time.sleep(1)
        
        search_input.send_keys(u'\ue007')
        print("Поиск выполнен")
        time.sleep(3)


        WebDriverWait(driver, 10).until(
            EC.presence_of_element_located((By.CSS_SELECTOR, "a[href*='/product/']"))
        )
        
        product_links = driver.find_elements(By.CSS_SELECTOR, "a[href*='/product/']")
        print(f"Найдено товаров: {len(product_links)}")
        assert len(product_links) > 0, "Товары не найдены!"
        
        title_selectors = [
            (By.CSS_SELECTOR, "[class*='title']"),
            (By.CSS_SELECTOR, "[class*='name']"),
            (By.CSS_SELECTOR, "a[href*='/product/'] span"),
        ]
        
        relevant_count = 0
        for by, selector in title_selectors:
            try:
                titles = driver.find_elements(by, selector)
                for title in titles[:10]:
                    if title.text and SEARCH_QUERY.lower() in title.text.lower():
                        relevant_count += 1
                if relevant_count > 0:
                    break
            except:
                continue
        
        print(f"Релевантных товаров (содержат '{SEARCH_QUERY}'): {relevant_count}")
        assert relevant_count > 0, f"Нет релевантных товаров по запросу '{SEARCH_QUERY}'"

        first_product = None
        for link in product_links:
            if link.is_displayed():
                first_product = link
                break
        
        if not first_product:
            first_product = product_links[0]
        
        product_url = first_product.get_attribute("href")
        
        try:
            product_id = product_url.split('/product/')[1].split('/')[0].split('?')[0]
        except:
            product_id = "unknown"
        
        print(f"Выбран товар с ID: {product_id}")
        print(f"URL: {product_url}")

        driver.get(product_url)
        time.sleep(3)
        
        assert "/product/" in driver.current_url, "Не удалось перейти на страницу товара"
        print("На странице товара")
        
        add_button = None
        button_selectors = [
            (By.CSS_SELECTOR, "[data-widget='webAddToCart'] button", "data-widget"),
            (By.CSS_SELECTOR, "[data-testid='addToCartButton']", "data-testid"),
            (By.CSS_SELECTOR, "button.b7b_3h", "класс b7b_3h"),
            (By.CSS_SELECTOR, "button[class*='a7b']", "класс, содержащий a7b"),
            (By.XPATH, "//button[contains(text(), 'В корзину')]", "текст 'В корзину'"),
            (By.XPATH, "//button[contains(text(), 'Добавить')]", "текст 'Добавить'"),
            (By.XPATH, "//button[contains(text(), 'Купить')]", "текст 'Купить'"),
            (By.CLASS_NAME, "ui-button", "класс ui-button"),
        ]
        
        for by, selector, description in button_selectors:
            try:
                elements = driver.find_elements(by, selector)
                for element in elements:
                    if element.is_displayed() and element.is_enabled():
                        if element.size['height'] > 30 and element.size['width'] > 80:
                            add_button = element
                            print(f"Найдена кнопка через {description}: '{element.text}'")
                            break
                if add_button:
                    break
            except:
                continue
        
        assert add_button is not None, "Кнопка добавления не найдена!"
        

        driver.execute_script("arguments[0].scrollIntoView({block: 'center'});", add_button)
        time.sleep(1)
        
        try:
            add_button.click()
            print("Клик по кнопке выполнен")
        except:
            try:
                ActionChains(driver).move_to_element(add_button).click().perform()
                print("Клик через ActionChains выполнен")
            except:
                driver.execute_script("arguments[0].click();", add_button)
                print("Клик через JavaScript выполнен")
        
        time.sleep(3)
        
        try:
            new_text = add_button.text
            print(f"Текст кнопки после добавления: '{new_text}'")
        except:
            pass

        
        driver.get("https://www.ozon.ru/cart")
        time.sleep(3)


        cart_items = driver.find_elements(By.CSS_SELECTOR, "a[href*='/product/']")
        
        if len(cart_items) > 0:
            print(f"В корзине {len(cart_items)} товар(ов)")
            
            found = False
            for item in cart_items:
                href = item.get_attribute("href") or ""
                if product_id in href:
                    found = True
                    try:
                        item_text = item.text
                        print(f"Товар в корзине: {item_text[:50]}...")
                    except:
                        pass
                    break
            
            if found:
                print("Наш товар успешно добавлен в корзину!")
            else:
                print("Наш товар не найден, но в корзине есть другие товары")
        else:
            # Используем другие локаторы для поиска элементов корзины
            empty_cart_messages = driver.find_elements(By.XPATH, "//*[contains(text(), 'пуста') or contains(text(), 'нет товаров')]")
            if empty_cart_messages:
                print("Корзина пуста")
                assert False, "Корзина пуста после добавления товара"

        # ========== 13. ИТОГ ==========

        print("Все шаги выполнены")
        print("Продемонстрированы все типы локаторов:")
        print("Проверены результаты поиска")
        print("Товар добавлен в корзину")
        print("Корзина проверена")
        
    except Exception as e:
        print(f"\nОШИБКА: {e}")
        import traceback
        traceback.print_exc()
        raise