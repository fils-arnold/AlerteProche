import json
import os
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parent.parent

DEFAULT_EMERGENCY_NUMBERS = {
    "police": "117",
    "gendarmerie": "113",
    "pompiers": "118",
    "samu": "119",
}


class Settings:
    def __init__(self, overrides=None):
        overrides = overrides or {}
        self.database = overrides.get("DATABASE") or os.environ.get("ALERTEPROCHE_DB") or str(BASE_DIR / "data" / "alerteproche.db")
        self.public_base_url = overrides.get("PUBLIC_BASE_URL") or os.environ.get("PUBLIC_BASE_URL", "")
        self.dev_mode = overrides.get("DEV_MODE", os.environ.get("DEV_MODE", "1") == "1")
        self.otp_ttl_seconds = int(overrides.get("OTP_TTL_SECONDS", os.environ.get("OTP_TTL_SECONDS", 300)))
        self.otp_max_sends = int(overrides.get("OTP_MAX_SENDS", os.environ.get("OTP_MAX_SENDS", 5)))
        self.otp_window_seconds = int(overrides.get("OTP_WINDOW_SECONDS", os.environ.get("OTP_WINDOW_SECONDS", 600)))
        self.otp_max_attempts = int(overrides.get("OTP_MAX_ATTEMPTS", os.environ.get("OTP_MAX_ATTEMPTS", 5)))
        self.fixed_otp = overrides.get("FIXED_OTP", os.environ.get("FIXED_OTP", ""))
        self.emergency_numbers_file = overrides.get("EMERGENCY_NUMBERS_FILE") or os.environ.get(
            "EMERGENCY_NUMBERS_FILE", str(BASE_DIR / "emergency_numbers.json")
        )

    def emergency_numbers(self):
        numbers = dict(DEFAULT_EMERGENCY_NUMBERS)
        path = Path(self.emergency_numbers_file)
        if path.is_file():
            try:
                data = json.loads(path.read_text(encoding="utf-8"))
                for key, value in data.items():
                    if isinstance(key, str) and isinstance(value, str) and value.strip():
                        numbers[key] = value.strip()
            except (OSError, ValueError):
                pass
        return numbers
