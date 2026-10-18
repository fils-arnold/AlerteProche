# AlerteProche (application mobile .NET MAUI)

Application d'alerte en un toucher vers des proches de confiance (SMS avec position, même sans internet).
Projet de l'Activité N°5, ENSPD. Auteur : NGOK FILS ARNOLD.

## Lancer le projet

Prérequis : SDK .NET avec la charge de travail MAUI, Android SDK, un émulateur ou un téléphone Android 8.0 ou plus.

```bash
dotnet workload install maui-android      # une seule fois
dotnet build -t:Run -f net9.0-android     # compile et lance sur l'émulateur / téléphone branché
```

Si ton SDK n'est pas .NET 9, change `TargetFrameworks` dans `AlerteProche.csproj` (`net8.0-android` ou `net10.0-android`).

## Mode simulation (démonstration)

`AppConfig.UseDemoBackend = true` (fichier `Helpers/AppConfig.cs`) :
- le code de vérification SMS accepté est **123456** ;
- les proches invités passent en « Accepté » après 20 s ;
- pendant une alerte, les statuts évoluent seuls : Envoyé, Délivré, Vu, puis Confirmé (en 3 à 14 s) ;
- aucune donnée ne quitte le téléphone, et l'envoi des SMS passe par le vrai envoi Android (à tester avec deux émulateurs, ports 5554 et 5556).

Pour un vrai serveur : `UseDemoBackend = false`, renseigner `ApiBaseUrl`, et implémenter les routes listées dans `Services/BackendClient.cs`.

## Les 21 écrans et la navigation

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
| 18 | Réglages & confort | ReglagesPage | //main/reglages |
| 19 | Protection & Famille | ProtectionFamillePage | protection |
| 20 | Récupération & confidentialité | RecuperationPage | recuperation |
| 11 | Code reçu par SMS (récupération) | CodeSmsPage | code?mode=recovery |

## Organisation du code (MVVM)

- `Pages/` : les écrans (XAML + code-behind minimal).
- `ViewModels/` : la logique de chaque écran (CommunityToolkit.Mvvm).
- `Services/` : `AlertService` (compte à rebours, envoi, escalade, clôture), `TripService` (Je rentre), `SmsDispatcher` (file d'attente), dépôts (proches, historique, consentements), réglages, thème.
- `Platforms/Android/` : envoi direct des SMS, autorisations, service de premier plan.
- `Helpers/` : traductions FR/EN (`Strings.cs`), configuration, outils XAML.

## Logique ajoutée par rapport aux maquettes

- Compte à rebours annulable, puis envoi du SMS avec position (lien carte), contact principal d'abord.
- Escalade vers les contacts secondaires après 60 s sans confirmation, puis proposition d'appeler les secours.
- File d'attente locale des SMS non envoyés, renvoyés automatiquement.
- Statuts par proche (Envoyé, Délivré, Vu, Confirmé) avec retour vibration et voix.
- Clôture avec PIN facultatif et code de contrainte (l'alerte continue en silence).
- « Je rentre » : minuterie, dernier avertissement, alerte automatique sans réponse en 60 s.
- Qualification des alertes (vraie, fausse, test) et suppression de l'historique.
- Taille du texte, contraste élevé, mode discret, traduction français et anglais.
- Consentements explicites conservés avec leur date, droits d'accès, de rectification et de suppression, suppression du compte.
- Service de premier plan Android pendant une alerte ou un trajet.

## Écarts avec les maquettes

- Écran « Message préenregistré » : ajout d'un champ « Votre prénom » (utilisé dans le SMS).
- Écran « Autorisations » : ajout d'un bouton « Continuer » (actif quand localisation et SMS sont accordés).
- Écran « Réglages » : ajout d'une section « Plus d'options » avec les liens vers les écrans 6, 13, 15, 19 et 20, et d'un réglage de la durée du compte à rebours.
- Écran « Alerte active » : le bouton pause coupe temporairement la voix et les vibrations.
- Si aucun proche n'a encore accepté, l'alerte part quand même vers les proches invités (une alerte perdue est pire qu'un SMS à un proche en attente).

## Pas encore fait (à brancher sur un serveur ou à compléter)

- Page de suivi en ligne pour les proches, vrais codes SMS d'inscription, statuts « Vu » et « Confirmé » réels, relance « Ça va ? » quotidienne, Plan Famille, partage de position en continu.
- Accusés de réception SMS côté Android (les états s'arrêtent à « Envoyé » sans serveur).
- Tests automatisés.

Ce projet n'a pas pu être compilé là où il a été écrit : à la première compilation, quelques erreurs de syntaxe sont possibles.
