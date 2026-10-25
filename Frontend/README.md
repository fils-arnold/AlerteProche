# AlerteProche - Front-end (application mobile .NET MAUI)

Projet de l'Activité N°5, ENSPD. Auteur : NGOK FILS ARNOLD.

Ce dossier contient l'application mobile Android. Elle fonctionne avec le dossier **AlerteProche_Backend** (le serveur), livré dans l'autre fichier.

## 1. À quoi sert l'application

AlerteProche permet de prévenir en un seul toucher des proches de confiance quand on se sent en danger. Le marché visé est le Cameroun.

- Un grand bouton sur l'écran d'accueil déclenche l'alerte. Un compte à rebours de 5 secondes laisse le temps d'annuler en cas de fausse manœuvre.
- Un SMS part directement depuis le téléphone, sans passer par l'application Messages, avec un lien de suivi qui contient la position GPS. Le SMS fonctionne même sans internet.
- Le proche ouvre le lien : il voit la position sur une carte, mise à jour toutes les quelques secondes, peut appeler la personne et confirmer qu'il s'en occupe.
- La personne en alerte voit l'état de chaque proche en direct : Envoyé, Vu, Confirmé.
- Si le contact principal ne confirme pas en 60 secondes, les contacts secondaires sont prévenus, puis l'application propose d'appeler les secours.
- « Je rentre » : on choisit une durée de trajet. Si l'arrivée n'est pas confirmée à temps, l'alerte part toute seule.
- Protection renforcée facultative : un code PIN pour clôturer et un code de contrainte qui fait semblant de clôturer l'alerte alors qu'elle continue en silence.
- Interface en français et en anglais, taille du texte réglable, contraste élevé, retour vibration et voix.

## 2. Comment les deux fichiers fonctionnent ensemble

```
Téléphone Android (AlerteProche_Frontend)  <---- HTTP / JSON ---->  Serveur (AlerteProche_Backend)
   - envoie les SMS d'alerte (SIM du téléphone)                        - codes de vérification
   - envoie sa position                                                - invitations des proches
   - lit les états Vu / Confirmé                                       - pages web de suivi
                                                                       - base SQLite
Téléphone du proche  ------------ navigateur web ------------------>  pages /i/... et /t/...
```

Le serveur est indispensable pour : le code de vérification du numéro, l'acceptation des invitations, la page de suivi et les états « Vu » et « Confirmé ». Sans serveur, l'alerte par SMS fonctionne quand même, avec un simple lien de carte (mode démonstration, voir la section 8).

## 3. Ce qu'il faut installer

Sur le PC (Windows) :

| Élément | Version | Remarque |
|---|---|---|
| SDK .NET | 9 | `dotnet --version` doit afficher 9.x |
| Charge de travail MAUI | maui-android | `dotnet workload install maui-android` |
| JDK | 17 ou plus (21 convient) | Microsoft OpenJDK par exemple |
| Android SDK | API 34 ou plus | par Android Studio ou par Visual Studio |
| Python | 3.10 ou plus | pour le serveur, voir le README du back-end |
| Éditeur | Visual Studio 2022 (17.12 ou plus) ou VS Code | voir l'étape 4 |

Pour tester : un téléphone Android 8.0 ou plus avec le mode développeur et le débogage USB activés, ou un émulateur Android.

Si votre SDK n'est pas .NET 9, changez `TargetFrameworks` dans `AlerteProche.csproj` (`net8.0-android` ou `net10.0-android`).

## 4. Procédure complète d'ouverture

### Étape 1 - Décompresser les deux fichiers

Décompressez `AlerteProche_Backend.zip` et `AlerteProche_Frontend.zip` côte à côte, par exemple dans `C:\Projets\`.

### Étape 2 - Démarrer le serveur (back-end)

Dans PowerShell :

```powershell
cd C:\Projets\AlerteProche_Backend
py -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
python run.py
```

Le serveur écoute sur le port 5000. Laissez cette fenêtre ouverte : c'est là qu'apparaissent les codes de vérification. Si Windows demande d'autoriser Python sur le réseau, acceptez pour les réseaux privés.

Vérification : ouvrez `http://localhost:5000/health` dans le navigateur du PC, la réponse doit être `{"status":"ok"}`.

### Étape 3 - Indiquer l'adresse du serveur à l'application

Ouvrez `Helpers/AppConfig.cs` et réglez `ApiBaseUrl` (elle doit se terminer par `/`) :

| Où tourne l'application | Valeur de `ApiBaseUrl` |
|---|---|
| Téléphone réel, connecté au même Wi-Fi que le PC | `http://ADRESSE_IP_DU_PC:5000/` (l'adresse IPv4 s'obtient avec la commande `ipconfig`, par exemple `http://192.168.1.23:5000/`) |
| Émulateur Android | `http://10.0.2.2:5000/` |
| Téléphone réel branché en USB | lancez `adb reverse tcp:5000 tcp:5000` puis utilisez `http://localhost:5000/` |

Le réglage `UseDemoBackend` doit rester à `false` pour utiliser le vrai serveur. Il faut recompiler après chaque changement de ce fichier.

Si le téléphone n'arrive pas à joindre le serveur, ouvrez PowerShell en administrateur et autorisez le port :

```powershell
New-NetFirewallRule -DisplayName "AlerteProche API" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

Test rapide : ouvrez `http://ADRESSE_IP_DU_PC:5000/health` dans le navigateur du téléphone. Si la page répond, l'application y arrivera aussi.

### Étape 4 - Ouvrir le projet

**Visual Studio 2022** : installer la charge de travail « Développement d'applications mobiles multiplateformes avec .NET » (cochez aussi « Développement Android »), puis Fichier, Ouvrir, Projet/Solution, et choisir `AlerteProche.csproj`.

**VS Code** : ouvrir le dossier `AlerteProche_Frontend`, installer les extensions « C# Dev Kit » et « .NET MAUI », puis choisir l'appareil Android dans la barre d'état.

**Ligne de commande** : rien à ouvrir, passez à l'étape 5.

### Étape 5 - Compiler et lancer

Avec Visual Studio ou VS Code, choisissez le téléphone ou l'émulateur et lancez en débogage (F5).

En ligne de commande, téléphone branché ou émulateur démarré :

```powershell
cd C:\Projets\AlerteProche_Frontend
dotnet workload install maui-android
dotnet build -t:Run -f net9.0-android
```

La première compilation télécharge les paquets NuGet et peut durer plusieurs minutes.

### Étape 6 - Premier lancement sur le téléphone

Au premier démarrage, Android demande des autorisations : position, envoi de SMS, notifications. Acceptez-les, elles sont nécessaires à une vraie alerte.

## 5. Comment utiliser l'application

### Configuration au premier lancement (une seule fois)

1. **Lancement** : écran de démarrage.
2. **Langue** : choisissez français ou anglais. Les boutons permettent aussi d'agrandir le texte et d'activer le contraste élevé.
3. **Ce que fait AlerteProche** : lisez la promesse, puis continuez.
4. **Numéro de téléphone** : saisissez votre numéro camerounais (9 chiffres, il commence par 6), puis « Recevoir le code ».
5. **Code reçu par SMS** : le code à 6 chiffres s'affiche dans la fenêtre du serveur (ligne « SMS simulé ») et sur la page `http://ADRESSE_IP_DU_PC:5000/dev`. Saisissez-le.
6. **Vos droits et consentements** : acceptez les consentements. Ils sont conservés avec leur date.
7. **Message préenregistré** : indiquez votre prénom et le message envoyé en cas d'alerte.
8. **Autorisations** : accordez la position et les SMS (obligatoires), les notifications sont facultatives. Le bouton « Continuer » s'active ensuite.
9. **Ajout d'un proche** : saisissez son nom et son numéro, puis « Envoyer l'invitation SMS ». Le proche reçoit un SMS avec un lien.
10. **Testez votre alerte** : envoie un SMS de test à votre contact principal, sans vraie alerte. Vous pouvez aussi passer.

### Utilisation courante

- **Accueil** : le grand bouton « ALERTER » déclenche l'alerte. Les raccourcis mènent à « Je rentre », à « Mes proches » et au menu (réglages). Les numéros d'urgence sont accessibles depuis l'alerte active et les réglages.
- **Compte à rebours** : pendant 5 secondes, un grand bouton « ANNULER » arrête tout. Le bouton retour du téléphone annule aussi.
- **Alerte active** : la liste des proches montre l'état de chacun (Envoyé, Vu, Confirmé). Si personne ne confirme, l'application propose d'appeler les secours, de relancer tout le monde ou d'ajouter un proche.
- **Clôturer** : « Je suis en sécurité » ferme l'alerte et prévient les proches. Avec la protection renforcée, il faut saisir le PIN. Le code de contrainte donne l'impression de clôturer mais l'alerte continue en silence.
- **Qualifier** : après la clôture, l'alerte peut être marquée vraie, fausse ou test dans l'historique.
- **Je rentre** : choisissez une durée, partez, puis touchez « Je suis arrivé(e) ». Au bout du temps, une question apparaît. Sans réponse en 60 secondes, l'alerte part automatiquement. On peut prolonger de 15 minutes.
- **Mes proches** (3 au maximum) : ajouter, modifier, supprimer, choisir le contact principal. Le statut passe à « Accepté » quand le proche a validé l'invitation.
- **Historique** : toutes les alertes avec date, durée, type et proches ayant confirmé. Filtres et suppression complète possibles.
- **Réglages** : langue, taille du texte, contraste élevé, mode discret, durée du compte à rebours, protection renforcée, numéros d'urgence modifiables, consentements, récupération du compte et suppression du compte.

### Côté proche (aucune application à installer)

1. Le proche reçoit le SMS d'invitation et ouvre le lien : une page lui présente l'invitation. Il touche « J'accepte d'être alerté(e) ».
2. En cas d'alerte, il reçoit un SMS avec un lien. La page affiche l'état de l'alerte, la carte avec la position, un bouton pour appeler la personne et le bouton **« Je confirme, je m'en occupe »**.
3. Dès qu'il ouvre la page, l'état passe à « Vu » chez la personne en alerte. Après avoir touché le bouton, il passe à « Confirmé ».
4. Quand la personne clôture son alerte, la page l'indique.

## 6. Scénario de démonstration (5 minutes)

1. Démarrer le serveur et ouvrir `http://ADRESSE_IP_DU_PC:5000/dev` sur le PC.
2. Sur le téléphone, faire la configuration avec votre numéro, puis ajouter comme proche un deuxième numéro à vous.
3. Sur le deuxième téléphone, ouvrir le lien reçu dans le SMS d'invitation et accepter (les deux téléphones doivent être sur le même Wi-Fi que le PC).
4. Sur le premier téléphone, ouvrir « Mes proches » : le statut est « Accepté ».
5. Toucher le grand bouton de l'accueil et laisser le compte à rebours aller au bout.
6. Sur le deuxième téléphone, ouvrir le lien du SMS d'alerte : la carte s'affiche. Sur le premier, le proche passe à « Vu ».
7. Toucher « Je confirme, je m'en occupe » : l'état passe à « Confirmé » avec une annonce vocale.
8. Clôturer l'alerte sur le premier téléphone : la page du proche affiche « Alerte clôturée ».

Attention : les SMS partent réellement depuis la carte SIM du téléphone et peuvent être facturés. Pour tester sans frais, utilisez deux émulateurs Android (ports 5554 et 5556).

## 7. Les 21 écrans et la navigation

Les numéros sont ceux du PDF des maquettes. Les noms de routes sont déclarés dans `AppShell.xaml.cs`.

| N° | Écran | Page | Route |
|---|---|---|---|
| 1 | Lancement | SplashPage | splash |
| 2 | Choix de la langue | LanguePage | langue |
| 3 | Ce que fait AlerteProche | PromessePage | promesse |
| 4 | Numéro de téléphone (étape 1/4) | TelephonePage | telephone |
| 5 | Code reçu par SMS (étape 2/4) | CodeSmsPage | code?mode=signup |
| 21 | Vos droits et consentements | ConsentementsPage | consentements |
| 6 | Message préenregistré | MessagePage | message |
| 7 | Autorisations (étape 3/4) | AutorisationsPage | autorisations |
| 8 | Ajout d'un proche (étape 4/4) | AjoutProchePage | ajoutproche |
| 13 | Testez votre alerte | TestAlertePage | test |
| 9 | Accueil | AccueilPage | //main/accueil |
| 12 | Compte à rebours | CompteAReboursPage | countdown |
| 14 | Alerte active | AlerteActivePage | alerteactive |
| 15 | Numéros d'urgence | NumerosUrgencePage | urgence |
| 17 | Je rentre | JeRentrePage | jerentre |
| 16 | Mes proches | MesProchesPage | //main/proches |
| 10 | Historique des alertes | HistoriquePage | //main/historique |
| 18 | Réglages et confort | ReglagesPage | //main/reglages |
| 19 | Protection et Famille | ProtectionFamillePage | protection |
| 20 | Récupération et confidentialité | RecuperationPage | recuperation |
| 11 | Code reçu par SMS (récupération) | CodeSmsPage | code?mode=recovery |

## 8. Mode démonstration (sans serveur)

Dans `Helpers/AppConfig.cs`, mettez `UseDemoBackend = true` :

- le code de vérification accepté est **123456** ;
- les proches invités passent en « Accepté » après 20 secondes ;
- pendant une alerte, les états évoluent seuls (Envoyé, Délivré, Vu, puis Confirmé en 3 à 14 secondes) ;
- aucune donnée ne quitte le téléphone ; le SMS contient un simple lien de carte.

## 9. Organisation du code (MVVM)

- `Pages/` : les écrans (XAML et code-behind minimal).
- `ViewModels/` : la logique de chaque écran (CommunityToolkit.Mvvm).
- `Services/` : `AlertService` (compte à rebours, envoi, escalade, suivi, clôture), `TripService` (Je rentre), `SmsDispatcher` (file d'attente des SMS), `BackendClient` (dialogue avec le serveur), dépôts (proches, historique, consentements), réglages, thème.
- `Platforms/Android/` : envoi direct des SMS, autorisations, service de premier plan.
- `Helpers/` : traductions FR/EN (`Strings.cs`), configuration (`AppConfig.cs`), outils XAML.

## 10. Sécurité et confidentialité

- Le serveur vérifie le numéro par un code à 6 chiffres valable 5 minutes. Au bout de 5 essais ratés, le code est bloqué.
- Après vérification, l'application reçoit un jeton de session, stocké sur le téléphone et envoyé à chaque requête. Le serveur ne garde que son empreinte.
- Le lien d'alerte est propre à chaque proche. Les consentements sont conservés avec leur date. L'utilisateur peut supprimer son historique et son compte : le serveur efface alors toutes ses données.
- Le manifeste Android autorise le trafic HTTP non chiffré pour faciliter les essais en réseau local. Pour un vrai déploiement, utilisez HTTPS et supprimez `android:usesCleartextTraffic` dans `Platforms/Android/AndroidManifest.xml`.

## 11. Dépannage

| Problème | Solution |
|---|---|
| Erreur `XA5300` (Android SDK introuvable) | Ajoutez le chemin : `dotnet build -t:Run -f net9.0-android -p:AndroidSdkDirectory=C:\Android\sdk` (adaptez le chemin), ou définissez la variable d'environnement `ANDROID_HOME`. Si le JDK est aussi introuvable, ajoutez `-p:JavaSdkDirectory="C:\Program Files\Microsoft\jdk-21..."`. |
| Le code de vérification n'arrive pas, « envoi impossible » | Le serveur n'est pas joignable : vérifiez `ApiBaseUrl`, le Wi-Fi commun, le pare-feu et l'URL `/health` depuis le téléphone. |
| Le code est refusé | Utilisez le dernier code affiché dans la fenêtre du serveur. Il expire après 5 minutes. |
| « Trop de demandes » | 5 codes maximum par numéro en 10 minutes, attendez ou redémarrez le serveur. |
| Le statut du proche reste « En attente » | Le proche doit ouvrir le lien de l'invitation et accepter ; rouvrez ensuite l'écran « Mes proches ». |
| Le lien du SMS ne s'ouvre pas chez le proche | Le téléphone du proche doit joindre le serveur : même Wi-Fi, ou serveur public (voir le README du back-end). |
| Les SMS ne partent pas | Vérifiez l'autorisation SMS et la présence d'une carte SIM. Les SMS non partis sont gardés et renvoyés automatiquement. |
| L'application plante au lancement | Vérifiez Android 8.0 ou plus et reconstruisez après `dotnet clean`. |

## 12. Ce qui n'est pas inclus

- Le Plan Famille, la relance quotidienne « Ça va ? » et le partage de position en continu hors alerte.
- Les accusés de réception SMS : l'état « Délivré » n'existe qu'en mode démonstration, avec le vrai serveur les états réels sont Envoyé, Vu et Confirmé.
- L'envoi du code de vérification par un vrai SMS : le serveur l'affiche dans sa console et sur sa page `/dev`.
- Les tests automatisés du front-end (le back-end en a 35).
- Une compilation complète MAUI n'a pas pu être exécutée dans l'environnement de rédaction : la syntaxe de tout le code a été vérifiée avec le compilateur C#, mais d'éventuelles erreurs d'assemblage peuvent apparaître à la première compilation.
