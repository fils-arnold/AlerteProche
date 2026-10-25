from .conftest import OWNER, RELATIVE


def add_contact(client, auth, contact_id="c1a2b3c4", name="Maman", phone=RELATIVE):
    return client.post("/contacts", json={"id": contact_id, "fullName": name, "phone": phone}, headers=auth)


def test_add_contact_returns_invite_link(client, auth):
    response = add_contact(client, auth)
    assert response.status_code == 200
    assert response.get_json()["inviteUrl"].startswith("http://serveur.test/i/")


def test_contact_validation(client, auth):
    assert add_contact(client, auth, name="  ").status_code == 400
    assert add_contact(client, auth, phone="698765432").status_code == 400
    assert add_contact(client, auth, contact_id="a b").status_code == 400


def test_invitation_flow_updates_status(client, auth):
    url = add_contact(client, auth).get_json()["inviteUrl"]
    path = url.replace("http://serveur.test", "")
    assert client.post("/contacts/status", json=["c1a2b3c4"], headers=auth).get_json() == {"c1a2b3c4": 0}
    page = client.get(path)
    assert page.status_code == 200
    assert "Maman" in page.get_data(as_text=True)
    answer = client.post(path, data={"action": "accept"})
    assert answer.status_code == 200
    assert "Invitation acceptée" in answer.get_data(as_text=True)
    assert client.post("/contacts/status", json=["c1a2b3c4"], headers=auth).get_json() == {"c1a2b3c4": 1}


def test_refusing_invitation_marks_contact_removed(client, auth):
    path = add_contact(client, auth).get_json()["inviteUrl"].replace("http://serveur.test", "")
    client.post(path, data={"action": "refuse"})
    assert client.post("/contacts/status", json=["c1a2b3c4"], headers=auth).get_json() == {"c1a2b3c4": 2}


def test_invalid_invitation_action(client, auth):
    path = add_contact(client, auth).get_json()["inviteUrl"].replace("http://serveur.test", "")
    assert client.post(path, data={"action": "autre"}).status_code == 400


def test_unknown_invitation_is_404(client):
    assert client.get("/i/inconnu").status_code == 404


def test_changing_phone_resets_invitation(client, auth):
    first = add_contact(client, auth).get_json()["inviteUrl"]
    client.post(first.replace("http://serveur.test", ""), data={"action": "accept"})
    second = add_contact(client, auth, phone="+237677000111").get_json()["inviteUrl"]
    assert first != second
    assert client.post("/contacts/status", json=["c1a2b3c4"], headers=auth).get_json() == {"c1a2b3c4": 0}


def test_renaming_keeps_invitation(client, auth):
    first = add_contact(client, auth).get_json()["inviteUrl"]
    second = add_contact(client, auth, name="Maman Julie").get_json()["inviteUrl"]
    assert first == second


def test_delete_contact(client, auth):
    path = add_contact(client, auth).get_json()["inviteUrl"].replace("http://serveur.test", "")
    assert client.delete("/contacts/c1a2b3c4", headers=auth).status_code == 204
    assert client.get(path).status_code == 404
    assert client.post("/contacts/status", json=["c1a2b3c4"], headers=auth).get_json() == {}


def test_status_only_for_own_contacts(client, auth):
    add_contact(client, auth)
    client.post("/auth/otp", json={"phone": RELATIVE})
    token = client.post("/auth/verify", json={"phone": RELATIVE, "code": "123456"}).get_json()["token"]
    other = {"Authorization": "Bearer " + token}
    assert client.post("/contacts/status", json=["c1a2b3c4"], headers=other).get_json() == {}


def test_status_requires_list(client, auth):
    assert client.post("/contacts/status", json={"a": 1}, headers=auth).status_code == 400


def test_account_deletion_removes_everything(client, auth):
    path = add_contact(client, auth).get_json()["inviteUrl"].replace("http://serveur.test", "")
    assert client.delete("/account?phone=" + OWNER, headers=auth).status_code == 204
    assert client.get(path).status_code == 404
    assert client.post("/contacts/status", json=[], headers=auth).status_code == 401
