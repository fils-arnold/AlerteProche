import pytest

from alerteproche_api import create_app

OWNER = "+237612345678"
RELATIVE = "+237698765432"


@pytest.fixture()
def app(tmp_path):
    return create_app(
        {
            "DATABASE": str(tmp_path / "test.db"),
            "FIXED_OTP": "123456",
            "DEV_MODE": True,
            "PUBLIC_BASE_URL": "http://serveur.test",
            "EMERGENCY_NUMBERS_FILE": str(tmp_path / "absent.json"),
        }
    )


@pytest.fixture()
def client(app):
    return app.test_client()


@pytest.fixture()
def auth(client):
    client.post("/auth/otp", json={"phone": OWNER})
    response = client.post("/auth/verify", json={"phone": OWNER, "code": "123456"})
    return {"Authorization": "Bearer " + response.get_json()["token"]}
