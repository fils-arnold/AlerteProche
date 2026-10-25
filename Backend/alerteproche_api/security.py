import hashlib
import hmac
import re
import secrets
from datetime import datetime, timezone

PHONE_RE = re.compile(r"^\+\d{8,15}$")
ID_RE = re.compile(r"^[A-Za-z0-9_-]{4,64}$")


def now_iso():
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def hash_value(value, salt=""):
    return hashlib.sha256((salt + value).encode("utf-8")).hexdigest()


def new_salt():
    return secrets.token_hex(8)


def new_token():
    return secrets.token_urlsafe(32)


def new_code():
    return f"{secrets.randbelow(1000000):06d}"


def safe_equal(a, b):
    return hmac.compare_digest(a.encode("utf-8"), b.encode("utf-8"))


def valid_phone(value):
    return isinstance(value, str) and bool(PHONE_RE.match(value))


def valid_id(value):
    return isinstance(value, str) and bool(ID_RE.match(value))


def to_float(value, low, high):
    if value is None:
        return None
    try:
        number = float(value)
    except (TypeError, ValueError):
        return None
    if number != number or number < low or number > high:
        return None
    return number
