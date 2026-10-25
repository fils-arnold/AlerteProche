from alerteproche_api import create_app

from .conftest import OWNER


def test_otp_requires_valid_phone(client):
    assert client.post("/auth/otp", json={"phone": "612345678"}).status_code == 400
    assert client.post("/auth/otp", json={}).status_code == 400


def test_otp_and_verify_return_token(client):
    assert client.post("/auth/otp", json={"phone": OWNER}).status_code == 200
    response = client.post("/auth/verify", json={"phone": OWNER, "code": "123456"})
    assert response.status_code == 200
    assert len(response.get_json()["token"]) > 20


def test_verify_wrong_code_is_rejected(client):
    client.post("/auth/otp", json={"phone": OWNER})
    assert client.post("/auth/verify", json={"phone": OWNER, "code": "000000"}).status_code == 400


def test_code_cannot_be_reused(client):
    client.post("/auth/otp", json={"phone": OWNER})
    assert client.post("/auth/verify", json={"phone": OWNER, "code": "123456"}).status_code == 200
    assert client.post("/auth/verify", json={"phone": OWNER, "code": "123456"}).status_code == 400


def test_code_locked_after_too_many_attempts(client):
    client.post("/auth/otp", json={"phone": OWNER})
    for _ in range(5):
        client.post("/auth/verify", json={"phone": OWNER, "code": "999999"})
    assert client.post("/auth/verify", json={"phone": OWNER, "code": "123456"}).status_code == 400


def test_otp_rate_limit(client):
    statuses = [client.post("/auth/otp", json={"phone": OWNER}).status_code for _ in range(7)]
    assert statuses[:5] == [200] * 5
    assert statuses[5:] == [429, 429]


def test_random_code_is_logged_in_outbox(tmp_path):
    app = create_app({"DATABASE": str(tmp_path / "r.db"), "DEV_MODE": True})
    client = app.test_client()
    client.post("/auth/otp", json={"phone": OWNER})
    page = client.get("/dev")
    assert page.status_code == 200
    assert "code de vérification" in page.get_data(as_text=True)


def test_dev_console_hidden_when_disabled(tmp_path):
    app = create_app({"DATABASE": str(tmp_path / "d.db"), "DEV_MODE": False})
    assert app.test_client().get("/dev").status_code == 404


def test_protected_routes_need_token(client):
    assert client.post("/alerts", json={"alertId": "abcd1234"}).status_code == 401
    assert client.post("/contacts/status", json=[]).status_code == 401
    assert client.delete("/account").status_code == 401
    bad = {"Authorization": "Bearer inconnu"}
    assert client.post("/contacts/status", json=[], headers=bad).status_code == 401


def test_emergency_numbers_are_public(client):
    data = client.get("/config/emergency-numbers").get_json()
    assert data["police"] == "117"
    assert set(data) == {"police", "gendarmerie", "pompiers", "samu"}


def test_emergency_numbers_file_overrides(tmp_path):
    path = tmp_path / "n.json"
    path.write_text('{"police": "1500"}', encoding="utf-8")
    app = create_app({"DATABASE": str(tmp_path / "e.db"), "EMERGENCY_NUMBERS_FILE": str(path)})
    data = app.test_client().get("/config/emergency-numbers").get_json()
    assert data["police"] == "1500"
    assert data["samu"] == "119"
