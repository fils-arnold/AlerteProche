# AlerteProche - Back-end (serveur API et pages web)

Projet de l'Activité N°5, ENSPD. Auteur : NGOK FILS ARNOLD.

Ce dossier contient le serveur utilisé par l'application mobile **AlerteProche** (dossier AlerteProche_Frontend, livré dans l'autre fichier).

## 1. À quoi sert ce serveur

L'application envoie les SMS d'alerte elle-même, depuis la carte SIM du téléphone. Le serveur apporte tout le reste :

- **Vérification du numéro** : génération et contrôle d'un code à 6 chiffres, puis remise d'un jeton de session.
- **Invitations** : chaque proche ajouté reçoit un lien. Sa page web lui permet d'accepter ou de refuser d'être alerté.
- **Page de suivi** : en cas d'alerte, le proche ouvre un lien, voit la position sur une carte mise à jour en direct, peut appeler la personne et confirmer qu'il s'en occupe.
- **États en direct** : le serveur indique à l'application si un proche a vu l'alerte (« Vu ») ou l'a confirmée (« Confirmé »).
- **Numéros d'urgence** modifiables à distance, sans mise à jour de l'application.
- **Suppression du compte** : efface toutes les données de la personne.

Technologies : Python 3, Flask, SQLite (fichier local). Aucun service externe n'est nécessaire.

## 2. Contenu du dossier

```
AlerteProche_Backend/
  run.py                    démarre le serveur
  requirements.txt          dépendances Python
  emergency_numbers.json    numéros d'urgence servis à l'application
  alerteproche_api/
    app.py                  routes de l'API et des pages web
    config.py               réglages (variables d'environnement)
    db.py                   schéma de la base SQLite
    security.py             codes, jetons, empreintes, validations
    sms.py                  envoi des SMS (simulé : console et base)
    states.py               valeurs des états
    templates/              pages web (invitation, suivi, console développeur)
  tests/                    35 tests automatisés
  data/                     créé au premier démarrage : alerteproche.db
```

## 3. Procédure complète d'ouverture

### Ce qu'il faut

- Python 3.10 ou plus (`py --version` sous Windows, `python3 --version` sous Linux ou macOS).
- Un éditeur au choix : VS Code (extension Python) ou simplement un terminal.

### Windows (PowerShell)

```powershell
cd C:\Projets\AlerteProche_Backend
py -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
python run.py
```

Si PowerShell refuse d'activer l'environnement, lancez d'abord `Set-ExecutionPolicy -Scope Process Bypass`.

### Linux ou macOS

```bash
cd AlerteProche_Backend
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
python run.py
```

### Vérifier que le serveur tourne

Le terminal affiche : `AlerteProche API : http://0.0.0.0:5000/ (console développeur : /dev)`.

Ouvrez `http://localhost:5000/health` dans le navigateur. La réponse attendue est `{"status":"ok"}`.

### Le rendre joignable depuis le téléphone

1. Le PC et le téléphone doivent être sur le même Wi-Fi.
2. Trouvez l'adresse IPv4 du PC avec `ipconfig` (par exemple `192.168.1.23`).
3. Autorisez le port 5000 dans le pare-feu Windows. Acceptez la fenêtre qui s'ouvre au premier démarrage, ou, dans PowerShell en administrateur :

```powershell
New-NetFirewallRule -DisplayName "AlerteProche API" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

4. Depuis le navigateur du téléphone, ouvrez `http://192.168.1.23:5000/health` (avec votre adresse). Si la page répond, indiquez la même adresse dans `ApiBaseUrl` côté application (voir le README du front-end).

## 4. Comment l'utiliser

1. Laissez la fenêtre du serveur ouverte pendant toute l'utilisation de l'application.
2. Quand l'application demande un code de vérification, il apparaît dans cette fenêtre, sur une ligne `[SMS simulé] vers +237... : AlerteProche : votre code de vérification est 123456`.
3. Le même code est visible sur la **console développeur** `http://ADRESSE:5000/dev`. Cette page liste aussi les SMS simulés, les proches enregistrés (avec leur lien d'invitation) et les alertes (avec leur lien de suivi).
4. Les proches ouvrent les liens `/i/...` (invitation) et `/t/...` (suivi) reçus par SMS : aucune installation n'est nécessaire de leur côté.
5. Arrêt du serveur : `Ctrl + C` dans la fenêtre.

Les données sont dans `data/alerteproche.db`. Pour repartir de zéro, arrêtez le serveur et supprimez ce fichier.

## 5. Lancer les tests

```powershell
python -m pytest
```

Les 35 tests couvrent la vérification du numéro (limites d'essais et de demandes), les jetons, les invitations, les alertes, les pages de suivi, la suppression du compte et le blocage de l'accès aux données d'autrui.

## 6. Réglages (variables d'environnement)

| Variable | Rôle | Valeur par défaut |
|---|---|---|
| `HOST` | adresse d'écoute | `0.0.0.0` |
| `PORT` | port d'écoute | `5000` |
| `PUBLIC_BASE_URL` | adresse publique utilisée dans les liens des SMS | adresse reçue dans la requête |
| `DEV_MODE` | `1` active la page `/dev`, `0` la désactive | `1` |
| `FIXED_OTP` | impose un code de vérification fixe (démonstration) | vide, code aléatoire |
| `OTP_TTL_SECONDS` | durée de validité d'un code | `300` |
| `OTP_MAX_SENDS` | codes maximum par numéro dans la fenêtre | `5` |
| `OTP_WINDOW_SECONDS` | durée de cette fenêtre | `600` |
| `OTP_MAX_ATTEMPTS` | essais maximum par code | `5` |
| `ALERTEPROCHE_DB` | chemin du fichier de base | `data/alerteproche.db` |
| `EMERGENCY_NUMBERS_FILE` | fichier JSON des numéros d'urgence | `emergency_numbers.json` |

Exemple sous PowerShell :

```powershell
$env:FIXED_OTP = "123456"
$env:PUBLIC_BASE_URL = "https://mon-serveur.example"
python run.py
```

Pour changer les numéros d'urgence servis à l'application, modifiez `emergency_numbers.json` (clés : `police`, `gendarmerie`, `pompiers`, `samu`) et redémarrez.

## 7. Les routes

API utilisée par l'application (JSON). Les routes marquées « jeton » exigent l'en-tête `Authorization: Bearer <jeton>`, obtenu par `/auth/verify`.

| Méthode et route | Jeton | Rôle |
|---|---|---|
| `POST /auth/otp` | non | envoie un code au numéro `{phone}` |
| `POST /auth/verify` | non | contrôle `{phone, code}` et renvoie `{token}` |
| `GET /config/emergency-numbers` | non | numéros d'urgence |
| `POST /contacts` | oui | crée ou met à jour un proche, renvoie `{inviteUrl}` |
| `DELETE /contacts/<id>` | oui | supprime un proche |
| `POST /contacts/status` | oui | états d'invitation d'une liste d'identifiants (0 en attente, 1 accepté, 2 retiré) |
| `POST /alerts` | oui | enregistre une alerte, renvoie `{trackingUrl}` |
| `POST /alerts/<id>/position` | oui | met à jour la position `{lat, lon}` |
| `POST /alerts/<id>/close` | oui | clôture l'alerte |
| `GET /alerts/<id>/status` | oui | états par proche (4 Vu, 5 Confirmé) |
| `DELETE /account` | oui | supprime le compte et toutes ses données |
| `GET /health` | non | état du serveur |

Pages web pour les proches :

| Route | Rôle |
|---|---|
| `GET /i/<jeton>` et `POST /i/<jeton>` | page d'invitation : accepter ou refuser |
| `GET /t/<alerte>?c=<proche>` | page de suivi (marque l'alerte comme « Vue ») |
| `GET /t/<alerte>/data?c=<proche>` | données rafraîchies par la page toutes les 5 secondes |
| `POST /t/<alerte>/confirm?c=<proche>` | confirmation « Je m'en occupe » |
| `GET /dev` | console développeur (si `DEV_MODE=1`) |

## 8. Sécurité et confidentialité

- Les codes de vérification sont stockés sous forme d'empreinte salée, valables 5 minutes, limités à 5 essais et à 5 envois par numéro et par 10 minutes.
- Les jetons de session ne sont jamais stockés en clair : seule leur empreinte est conservée.
- Chaque donnée est rattachée à son propriétaire : un compte ne peut lire ni modifier les alertes ou les proches d'un autre.
- La position n'est conservée que pour l'alerte concernée, et elle n'est plus mise à jour après la clôture.
- La suppression du compte efface le compte, les proches, les alertes, les événements et les SMS simulés du numéro.

## 9. Mettre le serveur en ligne (pour que les proches l'ouvrent depuis leur réseau mobile)

Sur un réseau local, seuls les téléphones connectés au même Wi-Fi peuvent ouvrir les liens des SMS. Pour un usage réel :

1. Hébergez le dossier sur un serveur Python (VPS, PythonAnywhere, Render, etc.) derrière HTTPS.
2. Définissez `PUBLIC_BASE_URL` avec l'adresse publique et `DEV_MODE=0`.
3. Lancez l'application avec un serveur de production (par exemple `waitress-serve --call run:create_app` ou `gunicorn "alerteproche_api:create_app()"`, à installer avec `pip`).
4. Côté application, mettez cette adresse dans `ApiBaseUrl` et supprimez `android:usesCleartextTraffic` du manifeste.

Pour une démonstration rapide avec des proches hors Wi-Fi, un tunnel comme Cloudflare Tunnel ou ngrok fournit une adresse HTTPS temporaire : utilisez-la dans `PUBLIC_BASE_URL` et dans `ApiBaseUrl`.

## 10. Ce qui n'est pas inclus

- L'envoi réel du code de vérification par SMS : le fournisseur est simulé (console et page `/dev`). Pour l'utiliser en production, il faut remplacer la classe `ConsoleSmsProvider` de `alerteproche_api/sms.py` par un fournisseur réel (Twilio, Africa's Talking ou un opérateur local).
- Les accusés de réception SMS « Délivré » : les états remontés par le serveur sont « Vu » et « Confirmé ».
- Le Plan Famille et la relance quotidienne « Ça va ? ».
