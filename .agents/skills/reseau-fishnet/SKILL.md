---
name: reseau-fishnet
description: API et pièges de FishNet 4.7.4 tel qu'installé dans Froggy (version épinglée, prédiction Replicate/Reconcile, PredictionRigidbody, NetworkTickSmoother, simulateur de latence, ping, connexions, transport Tugboat, port, NetworkManager d'une scène, salon, SyncVar/SyncList, ServerRpc, authentification, chargement de scènes, objets globaux). À charger AVANT d'écrire ou de modifier un script qui utilise FishNet, un prefab réseau, un NetworkManager, ou de toucher à la version du paquet. Aussi quand « le port est déjà pris », « Server failed to start », « SpawnablePrefabs is null », que les boutons Server / Client ne répondent pas, qu'un joueur compte double, ou que le ping semble faux.
---

# FishNet dans Froggy

Les règles de jeu en ligne (autorité, ce qui se synchronise, départs et arrivées) sont dans le skill
`multijoueur-equilibrage` ; tester à plusieurs sur une machine, dans `test-multijoueur-local`. Ici : ce qui est
**vérifié dans le code de la version installée** (lu dans `Library/PackageCache/com.firstgeargames.fishnet@<hash>/`,
novembre 2026). Une API absente de ce skill se lit dans ce dossier, jamais de mémoire : les exemples en ligne visent
souvent une autre version.

## 1. La version

- **FishNet 4.7.4**, épinglé dans `Packages/manifest.json` sur le commit `a2e2c3e1` de la branche `main`, **pas sur le
  tag `4.7.4`** qui ne compile pas ([FishNet#1110](https://github.com/FirstGearGames/FishNet/issues/1110)). Repasser
  sur un tag quand FishNet en publie un sain. Changer de version se décide en équipe, par une pull request.
- Transport : **Tugboat** (LiteNetLib), port **7770**, adresse client `localhost`.
- `TimeManager` absent des scènes : valeurs par défaut, **30 ticks par seconde**, `PhysicsMode.Unity`.

## 2. Ce qu'une scène réseau doit contenir

Le plus simple : le prefab `Assets/Froggy/Prefabs/Reseau/TestReseau.prefab` (P2-07), qui a tout. À la main :

- Le prefab `NetworkManager` des démos FishNet, avec **Spawnable Prefabs = `Assets/DefaultPrefabObjects.asset`**.
  Sans ça : « SpawnablePrefabs is null on NetworkManager » et rien n'apparaît.
- Sur son `PlayerSpawner` : le prefab du joueur (son `NetworkObject`) et les points d'apparition, utilisés dans l'ordre.
- **Un EventSystem** (avec `InputSystemUIInputModule`) : le prefab FishNet n'en a pas, ses boutons ne répondent pas.
- Le `NetworkManager` passe en `DontDestroyOnLoad` (`_dontDestroyOnLoad = true`).
- **Régler un prefab posé dans une scène par script** : `PrefabUtility.RecordPrefabInstancePropertyModifications`, ou
  passer par `SerializedObject` puis `ApplyModifiedProperties`. Sinon la valeur disparaît au Play. Relire le `.prefab`
  ou le `.unity` (`propertyPath:`) pour vérifier que la référence est bien écrite.

## 3. Script réseau

- Un objet partagé porte un `NetworkObject` ; un script d'état partagé hérite de `NetworkBehaviour`.
- `if (!IsOwner) return;` avant de lire des entrées ; ce qui dépend du propriétaire dans `OnStartClient()` (appeler
  `base.OnStartClient()`), pas dans `Awake` / `Start`.
- Côté serveur, les démos testent `IsServerStarted` (pas `IsServerInitialized`).
- `NetworkTransform` en client autoritaire = simple mais trichable : réservé aux scènes de test.

## 4. Prédiction (pour P2-02)

- Attributs `[Replicate]` et `[Reconcile]` (`FishNet.Object.Prediction`), méthodes **`private`**, une seule paire par
  classe, signature exacte :
  `void Bouger(Entrees d, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)` et
  `void Recaler(Etat d, Channel channel = Channel.Unreliable)`.
- Les données implémentent `IReplicateData` / `IReconcileData` (`GetTick`, `SetTick`, `Dispose`) ; `GetTick` doit
  lire un **champ privé** (contrôle du générateur de code).
- Obligatoire : `public override void CreateReconcile()` qui appelle la méthode `[Reconcile]` ; les démos l'appellent
  dans `TimeManager_OnPostTick`.
- Ordre d'un tick : recalage, `OnTick`, simulation physique (seulement en `PhysicsMode.TimeManager`), `OnPostTick`,
  envoi de l'état. Avec un Rigidbody ou des déclencheurs : **`PhysicsMode.TimeManager`** (les deux démos).
- Dans un replicate, le pas de temps est `(float)TimeManager.TickDelta`, jamais `Time.deltaTime`.
- `PredictionRigidbody` : `Initialize(rb)`, `AddForce` / `Velocity` mis en attente, puis `Simulate()` ; dans les
  données de recalage, puis `Reconcile(...)`.
- Lissage de l'affichage : un `NetworkTickSmoother` (`FishNet.Component.Transforming.Beta`) sur l'enfant graphique ;
  les réglages de lissage du `NetworkObject` sont annoncés obsolètes par l'inspecteur.
- Démos de référence : `Demos/Prediction/CharacterController/` et `Demos/Prediction/Rigidbody/`.

## 5. Mesurer et simuler

- Ping : `TimeManager.RoundTripTime` (ms), moyenne de 5 mesures, une par seconde, **arrondie au tick** : ≈ 33 ms à
  30 ticks/s même sans latence. Le dire avant de lire un chiffre.
- Simulateur : `NetworkManager.TransportManager.LatencySimulator` : `SetEnabled`, `SetLatency(long ms)`,
  `SetPacketLoss(double 0-1)`, `SetOutOfOrder(double 0-1)`. Il **retarde les envois du processus** où il est réglé ;
  l'hôte fait passer les deux sens par lui (« doublé » dans l'infobulle). Pour un aller-retour visé, régler **la
  moitié dans chaque fenêtre**. Mesuré : 100 ms visés → 147-173 ms affichés ; 250 ms → 300 ms.
- Pertes et désordre : messages non fiables seulement ; un message fiable n'est pas perdu, il arrive 30 % plus tard.
- **Seulement en éditeur et en build de développement** (`#if DEVELOPMENT`) : sans effet dans un build normal.

## 6. Connexions

- Serveur : `ServerManager.Clients` (client de l'hôte compris), `ServerManager.Started`, événements
  `OnServerConnectionState`, `OnRemoteConnectionState`. Client : `ClientManager.Connection.ClientId` (−1 si invalide),
  `ClientManager.Clients` (si `ShareIds`, vrai par défaut), `OnClientConnectionState`.
- `LocalConnectionState` : Stopped 1, Stopping 2, Starting 4, Started 8.
- **Compter les joueurs = connexions authentifiées** (`IsAuthenticated`). Une tentative de client arrivée avant que
  l'hôte écoute reste quelques secondes dans `Clients` sans objet ni authentification (vu en novembre 2026 : 3
  connexions pour 2 joueurs).
- **Réessayer une connexion** : poser soi-même l'état « Starting » avant `StartConnection()`. Sinon l'attente lit
  l'ancien « Stopped », relance tout de suite, et deux connexions partent pour un seul client.

## 7. Port et arrêt

- `ServerManager.StartConnection()` **rend `true` même si le port est pris** : Tugboat écrit en rouge « Server failed
  to start… » puis passe Starting → Stopping → Stopped. Détecter l'échec par `OnServerConnectionState`.
- **Port gardé après l'arrêt du Play** (novembre 2026, éditeur, Windows) : FishNet affichait « Local server is
  stopped » mais UDP 7770 restait ouvert jusqu'à la fermeture de Unity, même après une recompilation ; le Play suivant
  échouait. LiteNetLib seul libère bien son port ; cause dans FishNet non trouvée. Ce qui l'a évité :
  - arrêter soi-même client et serveur dans `OnApplicationQuit` ;
  - l'hôte de test essaie 7770 à 7779, en testant d'abord le port avec un `UdpClient` (pas d'erreur rouge), et note le
    port choisi dans `%TEMP%\froggy-test-reseau-port.txt`, relu par les clients (`TestReseau.cs`).
- Voir qui tient un port : `Get-NetUDPEndpoint -LocalPort 7770` (PowerShell). Un processus mort peut y rester
  quelques secondes.
- `Tugboat.SetPort` et `SetClientAddress` sont lus au démarrage : les régler **avant** `StartConnection`. Sur un client
  qui tourne, `GetPort()` rend son port de sortie, pas celui de l'hôte.
  `ClientManager.StartConnection(string adresse, ushort port)` fait les deux d'un coup.

## 8. Salon, scènes, état partagé (P2-03)

Exemple complet : `Assets/Froggy/Scripts/Reseau/Salon/` (prefab `Prefabs/Reseau/Salon.prefab`).

- **Ce qui n'existe pas** dans FishNet 4.7.4 : découverte réseau, salon, matchmaking, relais. Le `NetManager`
  LiteNetLib de Tugboat est `internal` : la recherche d'un salon par code passe par un `UdpClient` .NET à part
  (`RechercheSalon`, port 7790).
- **État synchronisé** : champs **`readonly`** `SyncVar<T>` (`.Value`, `OnChange(prev, next, asServer)`) et
  `SyncList<T>` (`OnChange(op, index, ancien, nouveau, asServer)`). `[SyncVar]` / `[SyncObject]` sont obsolètes. Une
  struct à champs publics se sérialise seule.
- **Demande d'un client** : `[ServerRpc(RequireOwnership = false)]` avec `NetworkConnection appelant = null` en
  **dernier** paramètre : FishNet le remplit côté serveur.
- **Objet qui survit au changement de scène** : prefab apparu avec `_isGlobal` coché sur son `NetworkObject` (refusé
  sur un objet posé dans une scène). `ServerManager.Spawn(objet, proprietaire)`.
- **Authentification** : hériter de `FishNet.Authenticating.Authenticator` (`HostAuthenticator` et
  `PasswordAuthenticator` sont dans `Demos/`, pas dans le runtime). Posé sur le NetworkManager, FishNet le trouve
  seul. Enregistrer le message d'entrée avec `RegisterBroadcast<T>(h, false)` (pas encore authentifié), répondre avec
  `ServerManager.Broadcast(conn, msg, false)` **avant** `OnAuthenticationResult(conn, false)`, qui coupe.
  Reconnaître le client de l'hôte : un jeton connu de son seul processus (`conn.IsLocalClient` n'est pas sûr à ce
  moment-là).
- **Scènes** : `SceneManager.LoadGlobalScenes(new SceneLoadData(nom) { ReplaceScenes = ReplaceOption.None })` et
  `UnloadGlobalScenes(new SceneUnloadData(nom))` ; scènes **dans les Build Settings**. Un client authentifié plus tard
  **reçoit les scènes globales déjà chargées** : refuser l'entrée pendant une partie. Recharger la scène du
  NetworkManager en crée un second : charger la partie en additif. Faire apparaître le joueur quand **lui** a fini de
  charger : `SceneManager.OnClientPresenceChangeEnd` (`Scene`, `Connection`, `Added`).
- `NetworkConnection.CustomData` (`object`) : donnée locale au serveur par connexion, non synchronisée ; la remettre
  à `null` à l'entrée (les connexions sont réutilisées).
- Testé le 18/11/2026 à 2 fenêtres : créer, rejoindre, mauvais code, partie en cours, lancer, revenir, deux Play de
  suite.

## 9. Règles décidées par l'hôte (P2-04)

Exemple : `Assets/Froggy/Scripts/Reseau/Partie/PartieReseau.cs`.

- **Réutiliser les règles locales** : l'hôte fait tourner les MonoBehaviours du poste 3 tels quels ; chez les autres,
  un drapeau `Miroir` coupe toute décision (dans `Pas` lui-même, pas seulement dans `FixedUpdate`) et `Refleter(...)`
  montre l'état reçu.
- **Publier les différences** : l'hôte compare et n'écrit dans `SyncList` / `SyncVar` que ce qui a changé (LateUpdate).
  Défaut des types synchronisés : canal **fiable**, envoi toutes les **0,1 s**. Ne pas passer `SyncTypeSettings(0f)`
  pour « envoyer tout de suite » : une valeur nulle ressemble au réglage par défaut.
- **Événements** : `[ObserversRpc(ExcludeServer = true)]` (le client de l'hôte a déjà émis les siens). Un RPC part
  **tout de suite**, l'état synchronisé au plus 0,1 s après : un client qui réagit au RPC lit l'ancien état. Mettre
  dans le message ce qu'il faut (nouveau score, gagnant) et le garder jusqu'à ce que l'état synchronisé le rattrape.
- **Numéroter par le salon**, pas par la position ; chercher un joueur par son numéro (trous possibles : 1 et 3).
- **`Start()` qui remet à zéro** : un objet apparu puis réglé dans la même image est remis à zéro par son `Start()` à
  l'image suivante ; attendre une image (coroutine) avant de commencer.
- Vérifier « en même temps » sans dépendre du réseau : poser les deux joueurs sur le cookie **pendant le compte à
  rebours** (règles en pause), la décision se fait au premier pas de jeu.
