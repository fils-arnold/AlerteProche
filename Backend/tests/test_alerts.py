from .conftest import RELATIVE


def setup_alert(client, auth, alert_id="alert0001abc"):
    client.post("/contacts", json={"id": "c1a2b3c4", "fullName": "Maman", "phone": RELATIVE}, headers=auth)
    response = client.post(
        "/alerts",
        json={
            "alertId": alert_id,
            "userPhone": "+237612345678",
            "lat": 4.0511,
            "lon": 9.7679,
            "startedAt": "2026-10-25T14:03:11.123+01:00",
            "isTest": False,
            "userName": "Arnold",
        },
        headers=auth,
    )
    return response


def test_register_alert_returns_tracking_url(client, auth):
    response = setup_alert(client, auth)
    assert response.status_code == 200
    assert response.get_json()["trackingUrl"] == "http://serveur.test/t/alert0001abc"


def test_register_alert_validation(client, auth):
    assert client.post("/alerts", json={"alertId": "x"}, headers=auth).status_code == 400
    assert client.post("/alerts", json={}, headers=auth).status_code == 400


def test_alert_cannot_be_taken_over(client, auth):
    setup_alert(client, auth)
    client.post("/auth/otp", json={"phone": RELATIVE})
    token = client.post("/auth/verify", json={"phone": RELATIVE, "code": "123456"}).get_json()["token"]
    other = {"Authorization": "Bearer " + token}
    assert client.post("/alerts", json={"alertId": "alert0001abc"}, headers=other).status_code == 403
    assert client.get("/alerts/alert0001abc/status", headers=other).status_code == 403
    assert client.post("/alerts/alert0001abc/close", headers=other).status_code == 403


def test_tracking_page_marks_seen_and_confirm(client, auth):
    setup_alert(client, auth)
    assert client.get("/alerts/alert0001abc/status", headers=auth).get_json() == {}
    page = client.get("/t/alert0001abc?c=c1a2b3c4")
    assert page.status_code == 200
    html = page.get_data(as_text=True)
    assert "Arnold" in html
    assert "Je confirme" in html
    assert client.get("/alerts/alert0001abc/status", headers=auth).get_json() == {"c1a2b3c4": 4}
    confirm = client.post("/t/alert0001abc/confirm?c=c1a2b3c4")
    assert confirm.status_code == 200
    assert confirm.get_json()["confirmed"] is True
    assert client.get("/alerts/alert0001abc/status", headers=auth).get_json() == {"c1a2b3c4": 5}


def test_seen_does_not_downgrade_confirmed(client, auth):
    setup_alert(client, auth)
    client.post("/t/alert0001abc/confirm?c=c1a2b3c4")
    client.get("/t/alert0001abc?c=c1a2b3c4")
    assert client.get("/alerts/alert0001abc/status", headers=auth).get_json() == {"c1a2b3c4": 5}


def test_tracking_without_contact_does_not_record(client, auth):
    setup_alert(client, auth)
    assert client.get("/t/alert0001abc").status_code == 200
    assert client.get("/t/alert0001abc?c=inconnu").status_code == 200
    assert client.get("/alerts/alert0001abc/status", headers=auth).get_json() == {}
    assert client.post("/t/alert0001abc/confirm").status_code == 403
    assert client.post("/t/alert0001abc/confirm?c=inconnu").status_code == 403


def test_tracking_unknown_alert(client):
    assert client.get("/t/inconnue123").status_code == 404
    assert client.get("/t/inconnue123/data").status_code == 404
    assert client.post("/t/inconnue123/confirm?c=x").status_code == 404


def test_position_update_and_data(client, auth):
    setup_alert(client, auth)
    update = client.post("/alerts/alert0001abc/position", json={"lat": 4.06, "lon": 9.77}, headers=auth)
    assert update.status_code == 200
    data = client.get("/t/alert0001abc/data?c=c1a2b3c4").get_json()
    assert data["lat"] == 4.06
    assert data["lon"] == 9.77
    assert data["name"] == "Arnold"
    assert data["closed"] is False
    assert data["confirmed"] is False


def test_position_validation(client, auth):
    setup_alert(client, auth)
    assert client.post("/alerts/alert0001abc/position", json={"lat": 200, "lon": 9}, headers=auth).status_code == 400
    assert client.post("/alerts/alert0001abc/position", json={"lat": "a", "lon": 9}, headers=auth).status_code == 400


def test_close_alert_freezes_position(client, auth):
    setup_alert(client, auth)
    assert client.post("/alerts/alert0001abc/close", headers=auth).get_json() == {"closed": True}
    client.post("/alerts/alert0001abc/position", json={"lat": 1.0, "lon": 1.0}, headers=auth)
    data = client.get("/t/alert0001abc/data?c=c1a2b3c4").get_json()
    assert data["closed"] is True
    assert data["lat"] == 4.0511


def test_alert_without_position(client, auth):
    client.post("/alerts", json={"alertId": "nopos000001"}, headers=auth)
    data = client.get("/t/nopos000001/data").get_json()
    assert data["lat"] is None
    assert data["lon"] is None
    assert data["name"] == "+237612345678"


def test_health_and_index(client):
    assert client.get("/health").get_json() == {"status": "ok"}
    assert client.get("/").get_json()["status"] == "ok"
