import time
from functools import wraps

from flask import Flask, abort, g, jsonify, render_template, request

from . import db
from .config import Settings
from .security import (
    hash_value,
    new_code,
    new_salt,
    new_token,
    now_iso,
    safe_equal,
    to_float,
    valid_id,
    valid_phone,
)
from .sms import ConsoleSmsProvider
from .states import (
    CONTACT_ACCEPTED,
    CONTACT_PENDING,
    CONTACT_REMOVED,
    DELIVERY_CONFIRMED,
    DELIVERY_SEEN,
)


def create_app(overrides=None):
    settings = Settings(overrides)
    app = Flask(__name__)
    app.config["JSON_AS_ASCII"] = False
    app.config["ALERTEPROCHE"] = settings

    conn = db.connect(settings.database)
    db.init_db(conn)
    sms = ConsoleSmsProvider(conn)
    app.config["DB"] = conn

    def base_url():
        if settings.public_base_url:
            return settings.public_base_url.rstrip("/")
        return request.host_url.rstrip("/")

    def json_body():
        data = request.get_json(silent=True)
        return data if data is not None else {}

    def error(message, status):
        return jsonify({"error": message}), status

    def require_auth(view):
        @wraps(view)
        def wrapper(*args, **kwargs):
            header = request.headers.get("Authorization", "")
            if not header.startswith("Bearer "):
                return error("non_authentifie", 401)
            token = header[7:].strip()
            row = conn.execute(
                "SELECT phone FROM sessions WHERE token_hash = ?", (hash_value(token),)
            ).fetchone()
            if row is None:
                return error("non_authentifie", 401)
            g.phone = row["phone"]
            return view(*args, **kwargs)

        return wrapper

    @app.after_request
    def secure_headers(response):
        response.headers["X-Content-Type-Options"] = "nosniff"
        response.headers["Referrer-Policy"] = "no-referrer"
        if response.mimetype in ("text/html", "application/json"):
            response.headers["Cache-Control"] = "no-store"
        return response

    @app.get("/")
    def index():
        return jsonify({"service": "AlerteProche API", "status": "ok"})

    @app.get("/health")
    def health():
        conn.execute("SELECT 1").fetchone()
        return jsonify({"status": "ok"})

    @app.post("/auth/otp")
    def auth_otp():
        phone = json_body().get("phone")
        if not valid_phone(phone):
            return error("numero_invalide", 400)
        now = time.time()
        recent = conn.execute(
            "SELECT COUNT(*) AS n FROM otp_codes WHERE phone = ? AND created_at > ?",
            (phone, now - settings.otp_window_seconds),
        ).fetchone()["n"]
        if recent >= settings.otp_max_sends:
            return error("trop_de_demandes", 429)
        code = settings.fixed_otp or new_code()
        salt = new_salt()
        conn.execute(
            "INSERT INTO otp_codes (phone, code_hash, salt, created_at, expires_at) VALUES (?, ?, ?, ?, ?)",
            (phone, hash_value(code, salt), salt, now, now + settings.otp_ttl_seconds),
        )
        conn.commit()
        if not sms.send(phone, f"AlerteProche : votre code de vérification est {code}"):
            return error("envoi_impossible", 502)
        return jsonify({"sent": True})

    @app.post("/auth/verify")
    def auth_verify():
        data = json_body()
        phone = data.get("phone")
        code = str(data.get("code", "")).strip()
        if not valid_phone(phone) or not code:
            return error("requete_invalide", 400)
        row = conn.execute(
            "SELECT * FROM otp_codes WHERE phone = ? AND used = 0 ORDER BY id DESC LIMIT 1",
            (phone,),
        ).fetchone()
        if row is None or row["expires_at"] < time.time() or row["attempts"] >= settings.otp_max_attempts:
            return error("code_invalide", 400)
        if not safe_equal(hash_value(code, row["salt"]), row["code_hash"]):
            conn.execute("UPDATE otp_codes SET attempts = attempts + 1 WHERE id = ?", (row["id"],))
            conn.commit()
            return error("code_invalide", 400)
        conn.execute("UPDATE otp_codes SET used = 1 WHERE id = ?", (row["id"],))
        conn.execute("INSERT OR IGNORE INTO users (phone, created_at) VALUES (?, ?)", (phone, now_iso()))
        token = new_token()
        conn.execute(
            "INSERT INTO sessions (token_hash, phone, created_at) VALUES (?, ?, ?)",
            (hash_value(token), phone, now_iso()),
        )
        conn.commit()
        return jsonify({"token": token})

    @app.get("/config/emergency-numbers")
    def emergency_numbers():
        return jsonify(settings.emergency_numbers())

    @app.post("/contacts")
    @require_auth
    def upsert_contact():
        data = json_body()
        contact_id = data.get("id")
        full_name = str(data.get("fullName", "")).strip()
        phone = data.get("phone")
        if not valid_id(contact_id) or not full_name or not valid_phone(phone):
            return error("requete_invalide", 400)
        full_name = full_name[:80]
        existing = conn.execute(
            "SELECT * FROM contacts WHERE owner_phone = ? AND id = ?", (g.phone, contact_id)
        ).fetchone()
        if existing is None:
            token = new_token()
            conn.execute(
                "INSERT INTO contacts (id, owner_phone, full_name, phone, status, invite_token, created_at)"
                " VALUES (?, ?, ?, ?, ?, ?, ?)",
                (contact_id, g.phone, full_name, phone, CONTACT_PENDING, token, now_iso()),
            )
        else:
            token = existing["invite_token"]
            if existing["phone"] != phone:
                token = new_token()
                conn.execute(
                    "UPDATE contacts SET full_name = ?, phone = ?, status = ?, invite_token = ?, responded_at = NULL"
                    " WHERE owner_phone = ? AND id = ?",
                    (full_name, phone, CONTACT_PENDING, token, g.phone, contact_id),
                )
            else:
                conn.execute(
                    "UPDATE contacts SET full_name = ? WHERE owner_phone = ? AND id = ?",
                    (full_name, g.phone, contact_id),
                )
        conn.commit()
        return jsonify({"inviteUrl": f"{base_url()}/i/{token}"})

    @app.delete("/contacts/<contact_id>")
    @require_auth
    def delete_contact(contact_id):
        conn.execute("DELETE FROM contacts WHERE owner_phone = ? AND id = ?", (g.phone, contact_id))
        conn.commit()
        return "", 204

    @app.post("/contacts/status")
    @require_auth
    def contacts_status():
        ids = request.get_json(silent=True)
        if not isinstance(ids, list):
            return error("requete_invalide", 400)
        ids = [i for i in ids if isinstance(i, str)][:50]
        result = {}
        for contact_id in ids:
            row = conn.execute(
                "SELECT status FROM contacts WHERE owner_phone = ? AND id = ?", (g.phone, contact_id)
            ).fetchone()
            if row is not None:
                result[contact_id] = row["status"]
        return jsonify(result)

    @app.post("/alerts")
    @require_auth
    def register_alert():
        data = json_body()
        alert_id = data.get("alertId")
        if not valid_id(alert_id):
            return error("requete_invalide", 400)
        lat = to_float(data.get("lat"), -90, 90)
        lon = to_float(data.get("lon"), -180, 180)
        if lat is None or lon is None:
            lat = lon = None
        started_at = str(data.get("startedAt") or now_iso())[:40]
        owner_name = str(data.get("userName") or "").strip()[:80]
        is_test = 1 if data.get("isTest") else 0
        existing = conn.execute("SELECT owner_phone FROM alerts WHERE id = ?", (alert_id,)).fetchone()
        if existing is not None and existing["owner_phone"] != g.phone:
            return error("interdit", 403)
        if existing is None:
            conn.execute(
                "INSERT INTO alerts (id, owner_phone, owner_name, is_test, started_at, lat, lon, position_at, created_at)"
                " VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)",
                (
                    alert_id,
                    g.phone,
                    owner_name,
                    is_test,
                    started_at,
                    lat,
                    lon,
                    now_iso() if lat is not None else None,
                    now_iso(),
                ),
            )
        conn.commit()
        return jsonify({"trackingUrl": f"{base_url()}/t/{alert_id}"})

    def owned_alert(alert_id):
        row = conn.execute("SELECT * FROM alerts WHERE id = ?", (alert_id,)).fetchone()
        if row is None:
            return None, error("alerte_inconnue", 404)
        if row["owner_phone"] != g.phone:
            return None, error("interdit", 403)
        return row, None

    @app.post("/alerts/<alert_id>/position")
    @require_auth
    def alert_position(alert_id):
        row, failure = owned_alert(alert_id)
        if failure:
            return failure
        data = json_body()
        lat = to_float(data.get("lat"), -90, 90)
        lon = to_float(data.get("lon"), -180, 180)
        if lat is None or lon is None:
            return error("position_invalide", 400)
        if row["closed_at"] is None:
            conn.execute(
                "UPDATE alerts SET lat = ?, lon = ?, position_at = ? WHERE id = ?",
                (lat, lon, now_iso(), alert_id),
            )
            conn.commit()
        return jsonify({"ok": True})

    @app.post("/alerts/<alert_id>/close")
    @require_auth
    def alert_close(alert_id):
        row, failure = owned_alert(alert_id)
        if failure:
            return failure
        if row["closed_at"] is None:
            conn.execute("UPDATE alerts SET closed_at = ? WHERE id = ?", (now_iso(), alert_id))
            conn.commit()
        return jsonify({"closed": True})

    @app.get("/alerts/<alert_id>/status")
    @require_auth
    def alert_status(alert_id):
        row, failure = owned_alert(alert_id)
        if failure:
            return failure
        events = conn.execute(
            "SELECT contact_id, MAX(state) AS state FROM alert_events WHERE alert_id = ? GROUP BY contact_id",
            (alert_id,),
        ).fetchall()
        return jsonify({e["contact_id"]: e["state"] for e in events})

    @app.delete("/account")
    @require_auth
    def delete_account():
        phone = g.phone
        conn.execute("DELETE FROM users WHERE phone = ?", (phone,))
        conn.execute("DELETE FROM otp_codes WHERE phone = ?", (phone,))
        conn.execute("DELETE FROM outbox WHERE phone = ?", (phone,))
        conn.commit()
        return "", 204

    def record_event(alert_id, owner_phone, contact_id, state):
        current = conn.execute(
            "SELECT MAX(state) AS state FROM alert_events WHERE alert_id = ? AND contact_id = ?",
            (alert_id, contact_id),
        ).fetchone()["state"]
        if current is not None and current >= state:
            return
        conn.execute(
            "INSERT INTO alert_events (alert_id, owner_phone, contact_id, state, at) VALUES (?, ?, ?, ?, ?)",
            (alert_id, owner_phone, contact_id, state, now_iso()),
        )
        conn.commit()

    def tracked_contact(alert, contact_id):
        if not contact_id:
            return None
        return conn.execute(
            "SELECT * FROM contacts WHERE owner_phone = ? AND id = ?", (alert["owner_phone"], contact_id)
        ).fetchone()

    def alert_view(alert, contact_id):
        confirmed = False
        if contact_id:
            state = conn.execute(
                "SELECT MAX(state) AS state FROM alert_events WHERE alert_id = ? AND contact_id = ?",
                (alert["id"], contact_id),
            ).fetchone()["state"]
            confirmed = state is not None and state >= DELIVERY_CONFIRMED
        return {
            "name": alert["owner_name"] or alert["owner_phone"],
            "phone": alert["owner_phone"],
            "isTest": bool(alert["is_test"]),
            "startedAt": alert["started_at"],
            "closed": alert["closed_at"] is not None,
            "lat": alert["lat"],
            "lon": alert["lon"],
            "positionAt": alert["position_at"],
            "confirmed": confirmed,
        }

    @app.get("/t/<alert_id>")
    def tracking_page(alert_id):
        alert = conn.execute("SELECT * FROM alerts WHERE id = ?", (alert_id,)).fetchone()
        if alert is None:
            abort(404)
        contact_id = request.args.get("c", "")
        contact = tracked_contact(alert, contact_id)
        if contact is not None:
            record_event(alert["id"], alert["owner_phone"], contact["id"], DELIVERY_SEEN)
        view = alert_view(alert, contact["id"] if contact is not None else "")
        return render_template(
            "tracking.html",
            alert_id=alert_id,
            contact_id=contact["id"] if contact is not None else "",
            contact_name=contact["full_name"] if contact is not None else "",
            view=view,
        )

    @app.get("/t/<alert_id>/data")
    def tracking_data(alert_id):
        alert = conn.execute("SELECT * FROM alerts WHERE id = ?", (alert_id,)).fetchone()
        if alert is None:
            return error("alerte_inconnue", 404)
        contact = tracked_contact(alert, request.args.get("c", ""))
        return jsonify(alert_view(alert, contact["id"] if contact is not None else ""))

    @app.post("/t/<alert_id>/confirm")
    def tracking_confirm(alert_id):
        alert = conn.execute("SELECT * FROM alerts WHERE id = ?", (alert_id,)).fetchone()
        if alert is None:
            return error("alerte_inconnue", 404)
        contact = tracked_contact(alert, request.args.get("c", ""))
        if contact is None:
            return error("proche_inconnu", 403)
        record_event(alert["id"], alert["owner_phone"], contact["id"], DELIVERY_CONFIRMED)
        return jsonify(alert_view(alert, contact["id"]))

    def invitation_row(token):
        return conn.execute(
            "SELECT c.*, u.phone AS owner FROM contacts c JOIN users u ON u.phone = c.owner_phone"
            " WHERE c.invite_token = ?",
            (token,),
        ).fetchone()

    def owner_label(row):
        latest = conn.execute(
            "SELECT owner_name FROM alerts WHERE owner_phone = ? AND owner_name != '' ORDER BY created_at DESC LIMIT 1",
            (row["owner_phone"],),
        ).fetchone()
        return latest["owner_name"] if latest is not None else row["owner_phone"]

    @app.get("/i/<token>")
    def invitation_page(token):
        row = invitation_row(token)
        if row is None:
            abort(404)
        return render_template(
            "invitation.html",
            token=token,
            contact_name=row["full_name"],
            owner=owner_label(row),
            owner_phone=row["owner_phone"],
            status=row["status"],
            result=None,
        )

    @app.post("/i/<token>")
    def invitation_answer(token):
        row = invitation_row(token)
        if row is None:
            abort(404)
        action = request.form.get("action", "")
        if action not in ("accept", "refuse"):
            abort(400)
        status = CONTACT_ACCEPTED if action == "accept" else CONTACT_REMOVED
        conn.execute(
            "UPDATE contacts SET status = ?, responded_at = ? WHERE invite_token = ?",
            (status, now_iso(), token),
        )
        conn.commit()
        return render_template(
            "invitation.html",
            token=token,
            contact_name=row["full_name"],
            owner=owner_label(row),
            owner_phone=row["owner_phone"],
            status=status,
            result=action,
        )

    @app.get("/dev")
    def dev_console():
        if not settings.dev_mode:
            abort(404)
        messages = conn.execute("SELECT * FROM outbox ORDER BY id DESC LIMIT 50").fetchall()
        alerts = conn.execute("SELECT * FROM alerts ORDER BY created_at DESC LIMIT 20").fetchall()
        contacts = conn.execute("SELECT * FROM contacts ORDER BY created_at DESC LIMIT 50").fetchall()
        return render_template(
            "dev.html",
            messages=messages,
            alerts=alerts,
            contacts=contacts,
            base=base_url(),
        )

    @app.errorhandler(404)
    def not_found(_):
        if request.accept_mimetypes.accept_html and not request.path.startswith(("/auth", "/alerts", "/contacts")):
            return render_template("message.html", title="Page introuvable", text="Ce lien n'est pas valide ou a expiré."), 404
        return error("introuvable", 404)

    @app.errorhandler(400)
    def bad_request(_):
        return error("requete_invalide", 400)

    return app
