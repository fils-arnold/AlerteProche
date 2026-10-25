import os

from alerteproche_api import create_app

app = create_app()

if __name__ == "__main__":
    host = os.environ.get("HOST", "0.0.0.0")
    port = int(os.environ.get("PORT", "5000"))
    print(f"AlerteProche API : http://{host}:{port}/  (console développeur : /dev)")
    app.run(host=host, port=port, threaded=True)
