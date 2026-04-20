"""
PetFriends API Client
=====================
Реализация всех методов REST API сервиса PetFriends.

Документация API: https://petfriends.skillfactory.ru/api/
Swagger-спецификация описывает следующие эндпоинты:
  GET    /api/key                        — получение API-ключа
  GET    /api/pets                       — список питомцев
  POST   /api/pets                       — добавить питомца с фото
  POST   /api/create_pet_simple          — добавить питомца без фото
  POST   /api/pets/set_photo/{pet_id}    — загрузить фото к существующему питомцу
  PUT    /api/pets/{pet_id}              — обновить данные питомца
  DELETE /api/pets/{pet_id}              — удалить питомца

Примечание о поведении сервера:
  При ошибках (403, 400, 500) сервер возвращает HTML-страницу, а не JSON.
  Метод _parse_response() безопасно обрабатывает оба формата ответа.
"""

import requests
from requests import Response


class PetFriends:
    """Клиент для работы с API сервиса PetFriends."""

    BASE_URL = "https://petfriends.skillfactory.ru"

    # ------------------------------------------------------------------
    # Вспомогательный метод
    # ------------------------------------------------------------------

    @staticmethod
    def _parse_response(response: Response) -> dict | str:
        """
        Безопасно разбирает тело ответа.

        Сервер PetFriends при ошибках (403, 400, 500) возвращает HTML,
        а не JSON. Этот метод пробует распарсить JSON; если не удаётся —
        возвращает сырой текст, чтобы вызов не падал с JSONDecodeError.

        :param response: объект ответа requests
        :return: dict (если тело — JSON) или str (если HTML / пустое тело)
        """
        try:
            return response.json()
        except Exception:
            return response.text

    # ------------------------------------------------------------------
    # 1. Аутентификация
    # ------------------------------------------------------------------

    def get_api_key(self, email: str, password: str) -> tuple[int, dict | str]:
        """
        GET /api/key
        Возвращает API-ключ зарегистрированного пользователя.

        :param email:    e-mail зарегистрированного пользователя
        :param password: пароль пользователя
        :return:         (status_code, тело ответа — dict или str)
        """
        response: Response = requests.get(
            url=f"{self.BASE_URL}/api/key",
            headers={"email": email, "password": password},
        )
        return response.status_code, self._parse_response(response)

    # ------------------------------------------------------------------
    # 2. Получение списка питомцев
    # ------------------------------------------------------------------

    def get_list_of_pets(
        self, auth_key: str, filter: str = ""
    ) -> tuple[int, dict | str]:
        """
        GET /api/pets
        Возвращает список питомцев.

        :param auth_key: API-ключ авторизации
        :param filter:   необязательный фильтр; поддерживаемое значение — "my_pets"
        :return:         (status_code, тело ответа — dict или str)
        """
        params = {}
        if filter:
            params["filter"] = filter

        response: Response = requests.get(
            url=f"{self.BASE_URL}/api/pets",
            headers={"auth_key": auth_key},
            params=params,
        )
        return response.status_code, self._parse_response(response)

    # ------------------------------------------------------------------
    # 3. Добавление питомца с фото
    # ------------------------------------------------------------------

    def add_new_pet(
        self,
        auth_key: str,
        name: str,
        animal_type: str,
        age: str,
        pet_photo: str,
    ) -> tuple[int, dict | str]:
        """
        POST /api/pets
        Создаёт нового питомца и сразу загружает его фотографию.

        :param auth_key:    API-ключ авторизации
        :param name:        имя питомца
        :param animal_type: порода / вид питомца
        :param age:         возраст питомца (строка, т.к. передаётся в form-data)
        :param pet_photo:   путь к файлу изображения (JPG / JPEG / PNG)
        :return:            (status_code, тело ответа — dict или str)
        """
        data = {"name": name, "animal_type": animal_type, "age": age}

        with open(pet_photo, "rb") as photo_file:
            files = {"pet_photo": photo_file}
            response: Response = requests.post(
                url=f"{self.BASE_URL}/api/pets",
                headers={"auth_key": auth_key},
                data=data,
                files=files,
            )

        return response.status_code, self._parse_response(response)

    # ------------------------------------------------------------------
    # 4. Добавление питомца без фото
    # ------------------------------------------------------------------

    def create_pet_simple(
        self,
        auth_key: str,
        name: str,
        animal_type: str,
        age: str,
    ) -> tuple[int, dict | str]:
        """
        POST /api/create_pet_simple
        Создаёт нового питомца без фотографии.

        :param auth_key:    API-ключ авторизации
        :param name:        имя питомца
        :param animal_type: порода / вид питомца
        :param age:         возраст питомца
        :return:            (status_code, тело ответа — dict или str)
        """
        data = {"name": name, "animal_type": animal_type, "age": age}

        response: Response = requests.post(
            url=f"{self.BASE_URL}/api/create_pet_simple",
            headers={"auth_key": auth_key},
            data=data,
        )
        return response.status_code, self._parse_response(response)

    # ------------------------------------------------------------------
    # 5. Загрузка / обновление фото питомца
    # ------------------------------------------------------------------

    def set_photo(
        self, auth_key: str, pet_id: str, pet_photo: str
    ) -> tuple[int, dict | str]:
        """
        POST /api/pets/set_photo/{pet_id}
        Устанавливает (или обновляет) фотографию существующего питомца.

        :param auth_key:  API-ключ авторизации
        :param pet_id:    идентификатор питомца
        :param pet_photo: путь к файлу изображения (JPG / JPEG / PNG)
        :return:          (status_code, тело ответа — dict или str)
        """
        with open(pet_photo, "rb") as photo_file:
            files = {"pet_photo": photo_file}
            response: Response = requests.post(
                url=f"{self.BASE_URL}/api/pets/set_photo/{pet_id}",
                headers={"auth_key": auth_key},
                files=files,
            )

        return response.status_code, self._parse_response(response)

    # ------------------------------------------------------------------
    # 6. Обновление данных питомца
    # ------------------------------------------------------------------

    def update_pet(
        self,
        auth_key: str,
        pet_id: str,
        name: str,
        animal_type: str,
        age: str,
    ) -> tuple[int, dict | str]:
        """
        PUT /api/pets/{pet_id}
        Обновляет имя, вид и возраст существующего питомца.

        :param auth_key:    API-ключ авторизации
        :param pet_id:      идентификатор питомца
        :param name:        новое имя питомца
        :param animal_type: новый вид/порода питомца
        :param age:         новый возраст питомца
        :return:            (status_code, тело ответа — dict или str)
        """
        data = {"name": name, "animal_type": animal_type, "age": age}

        response: Response = requests.put(
            url=f"{self.BASE_URL}/api/pets/{pet_id}",
            headers={"auth_key": auth_key},
            data=data,
        )
        return response.status_code, self._parse_response(response)

    # ------------------------------------------------------------------
    # 7. Удаление питомца
    # ------------------------------------------------------------------

    def delete_pet(self, auth_key: str, pet_id: str) -> tuple[int, dict | str]:
        """
        DELETE /api/pets/{pet_id}
        Удаляет питомца из базы данных.

        :param auth_key: API-ключ авторизации
        :param pet_id:   идентификатор питомца
        :return:         (status_code, тело ответа — dict или str)
        """
        response: Response = requests.delete(
            url=f"{self.BASE_URL}/api/pets/{pet_id}",
            headers={"auth_key": auth_key},
        )
        return response.status_code, self._parse_response(response)
