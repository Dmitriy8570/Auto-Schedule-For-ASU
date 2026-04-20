import pytest
import requests
import time

API_URL = "https://petfriends.skillfactory.ru/api/create_pet_simple"


@pytest.mark.parametrize("i", range(10))
def test_create_multiple_pets(auth_key, i):

    headers = {"auth_key": auth_key}

    pet_data = {
        "name": f"PerfPet{i}",
        "animal_type": "dog",
        "age": f"{i}"
    }

    start_time = time.time()

    response = requests.post(
        API_URL,
        headers=headers,
        data=pet_data
    )

    elapsed = time.time() - start_time

    assert response.status_code == 200
    assert response.status_code < 500
    assert elapsed < 5