import sys

from .security import now_iso


class ConsoleSmsProvider:
    def __init__(self, conn):
        self.conn = conn

    def send(self, phone, body):
        self.conn.execute(
            "INSERT INTO outbox (phone, body, created_at) VALUES (?, ?, ?)",
            (phone, body, now_iso()),
        )
        self.conn.commit()
        print(f"[SMS simulé] vers {phone} : {body}", file=sys.stdout, flush=True)
        return True
