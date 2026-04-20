# conftest.py
# Регистрация пользовательских маркеров pytest.
# Без этого файла pytest выдаёт PytestUnknownMarkWarning для @pytest.mark.positive
# и @pytest.mark.negative.

import pytest


def pytest_configure(config):
    config.addinivalue_line("markers", "positive: позитивные тест-кейсы (ожидаемый успех)")
    config.addinivalue_line("markers", "negative: негативные тест-кейсы (ожидаемая ошибка)")
