---
name: test-multijoueur-local
description: Tester Froggy à 2, 3 ou 4 joueurs sur une seule machine avec Multiplayer Play Mode, et le faire piloter par une IA (rôle hôte ou client de chaque fenêtre, joindre une fenêtre de joueur par le Unity CLI, cases à cocher par un humain, scène vide des clones, mémoire et disque, vérification d'un test). À charger AVANT de lancer un test réseau dans l'éditeur, d'activer des Player 2-4, ou de vérifier une fonction réseau. Aussi quand une fenêtre de joueur « reste vide », « se ferme toute seule », « ne se connecte pas », ou que le disque C: se remplit pendant un test.
---

# Tester à plusieurs sur une machine

Mode d'emploi humain : `Docs/Reseau/GUIDE.md`, « Tester à 2, 3 ou 4 ». API FishNet : skill `reseau-fishnet`. Ici :
ce qu'une IA doit savoir pour lancer, observer et juger un test (appris sur P2-01 et P2-07, novembre 2026).

## 1. Avant de lancer : la machine

- **Chaque Player de Multiplayer Play Mode (MPPM) est un Unity complet** : ≈ 3 Go de mémoire chacun. Sur 16 Go, rester
  à **2 fenêtres**. À 4 fenêtres (18/11/2026), Windows a agrandi `C:\pagefile.sys` jusqu'à 25 Go : C: est tombé à
  0,3 Go et les Players 3 et 4 ont planté **sans message** (seulement `Unity.exe.<pid>.dmp` dans
  `%LOCALAPPDATA%\CrashDumps`). Plus tard un Player 2 s'est fermé seul avec 1,7 Go de RAM libre.
- Avant d'ajouter un joueur, et après chaque test : lire C: libre et la RAM libre, et s'arrêter sous ~3 Go.

```powershell
"C: " + [math]::Round((Get-PSDrive C).Free/1GB,1) + " Go ; RAM " + [math]::Round((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory/1MB,1) + " Go"
Get-CimInstance Win32_PageFileUsage | Select Name,AllocatedBaseSize,PeakUsage
```

- La place prise par le fichier d'échange revient quand on ferme les Unity (vu : 0,3 → 8,8 Go), sinon au redémarrage.
- Un critère « tester à 4 » se vérifie sur un poste à 32 Go : le dire plutôt que de forcer.

## 2. Ce qui se fait par script, et ce qui ne se fait pas

- **Rôle de la fenêtre** : `Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor` (M majuscule à `PlayMode` ; module
  moteur, déjà référencé). En build il vaut **faux** : « je suis un clone » = `Application.isEditor &&
  !CurrentPlayer.IsMainEditor`. Pas d'API publique pour le numéro du joueur (2, 3, 4) : utiliser l'id client FishNet,
  ou un tag par joueur (`CurrentPlayer.Tags`).
- **Cocher Player 2-4 : impossible par une API publique** (MPPM 3.0, le code est dans le moteur, l'activation est
  interne). Le demander à l'humain avec le chemin exact : *Froggy ▸ Réseau ▸ 2. Fenêtre Multiplayer Play Mode*, ou
  *Window ▸ Multiplayer ▸ Multiplayer Play Mode*. Une fois cochés, les joueurs se relancent seuls à la réouverture de
  Unity.
- Un scénario *Play Mode Scenarios* créé par le CLI n'a pas lancé ses clones : ne pas compter dessus.
- **Lancer** : un Play dans la fenêtre principale suffit, les clones actifs suivent.

## 3. Joindre chaque fenêtre par le Unity CLI

- `unity status --format json` liste les fenêtres : `Main Editor` et `Player 2…`, avec leur `vpId` (`mppm<hex>`).
- `unity command` n'a pas d'option d'instance : viser un clone par **son dossier**,
  `--project-path <projet>/Library/VP/<vpId>`. Même chose pour `script eval_file`, `editor status`, `capture`.
- Un clone absent de `unity status` alors qu'il était prêt une minute avant : il a planté (section 1).
- Journal d'un clone : `Library/VP/<vpId>/Logs/Editor.log` (y chercher `Local client is`, `[Test réseau]`).
- Les scripts de test de l'IA vont dans le **scratchpad**, pas dans `Temp/` du projet : Unity vide `Temp/` à chaque
  redémarrage.

## 4. Scène des clones

- MPPM recopie la scène de la fenêtre principale **par chemin**, **en mode édition seulement**, et **pas les
  modifications non enregistrées**. Une scène jamais enregistrée, ou un clone activé avant de l'avoir reçue, donne une
  fenêtre de joueur vide.
- Toujours enregistrer avant Play. `TestReseau` recharge la scène de test si un clone démarre sur une scène vide.
- Ce qui est chargé **pendant** le jeu (décor additif) n'est pas recopié : chaque fenêtre le charge elle-même.

## 5. Vérifier un test

Pour chaque fenêtre, lire l'état plutôt que de supposer (`script eval_file` sur chaque `--project-path`) :

- rôle (hôte, client), serveur et client démarrés ;
- **connexions authentifiées** côté hôte = nombre de fenêtres (le total brut peut compter une tentative avortée) ;
- objets joueurs visibles dans chaque fenêtre = nombre de fenêtres ;
- ping et réglages du simulateur de latence ;
- journal : aucune erreur rouge pendant le démarrage simultané.

Puis :

- **enchaîner au moins deux Play** sans redémarrer Unity (c'est là que le port restait pris) ;
- **captures de chaque fenêtre** : `capture game_view --source screen` montre l'interface IMGUI (panneau), pas
  `capture screenshot` (skill `unity-mcp`, section 3) ;
- dire ce qui reste à juger par un humain : déplacement au clavier (l'IA ne peut pas donner le focus à une fenêtre),
  ressenti de la latence.

## 6. Arrêter proprement

- Stop dans la fenêtre principale ; vérifier que le port est libéré (`Get-NetUDPEndpoint -LocalPort 7770`).
- Port encore pris : `unity close <projet>` puis `unity open <projet>` (vérifier avant qu'aucune scène n'a de
  modification non enregistrée : `close` quitte sans enregistrer).
- Remettre le préréglage de connexion sur **Parfait** avant de commiter (l'asset est partagé par toute l'équipe).

## 7. Jouer une partie à deux par script (Froggy, 18 novembre 2026, #23 et #128)

Une IA ne peut pas donner le focus à une fenêtre ni appuyer sur une touche : elle pilote tout par
`unity command eval --project-path <fenêtre>` (principale, ou `Library/VP/<vpId>` pour Player 2).

- **Player 2 décoché** (un humain l'a décoché pour la mémoire) : rouvrir le panneau par
  `EditorApplication.ExecuteMenuItem("Froggy/Réseau/2. Fenêtre Multiplayer Play Mode (cocher Player 2, 3, 4)")` et
  demander à l'humain de cocher. `Library/VP/SystemData.json` dit qui est actif (`"Active"`).
- **Salon** : hôte `Salon.Local.Creer()` puis lire `Salon.Local.Code` ; Player 2 `Salon.Local.Rejoindre(code)`, puis
  `SeDirePret(true)` ; hôte `Lancer()` quand `PeutLancer`. Après la PR #135 (Unity Relay), « Créer » passe en ligne
  par défaut : poser `EnLigne = false` pour un test sur une machine.
- **La partie de test dure 30 s** (`ReglagesPartie_Essai`) : la rallonger chez l'hôte dès qu'elle est « en jeu »,
  sans toucher à l'asset partagé : champ `<Reste>k__BackingField` de `ReglesPartie` par réflexion, à 600. Les autres
  recopient le temps restant (vérifié dans les deux fenêtres). Le gobelin n'arrive alors qu'à la dernière minute.
- **Téléporter une Froggy** : `Rigidbody.position` et `transform.position`, sur la fenêtre de son propriétaire.
  L'hôte voit la nouvelle place 0,1 à 0,2 s plus tard : une action décidée par l'hôte (torgnole, ramassage) lancée
  dans le même appel part de l'ancienne place (« torgnole dans le vide »). Placer, faire un appel vide, puis agir.
- **Simuler une touche** : appeler ce que la touche appelle (`PartieTorgnoles.DemandeFrappe(JoueurLocal())`,
  `PartieObjets.DemandeUtiliser(Local)` chez un client ; `Frapper` / `Utiliser` chez l'hôte).
  `DeplacementWow.Imposer` **verrouille le clavier** de cette Froggy jusqu'à la fin du Play : si un humain joue
  aussi, rendre la main en remettant le champ privé `imposees` à faux (réflexion). Prévenir l'humain avant.
- **Donner un objet précis** (le tirage est au hasard) : chez l'hôte, `ReglesObjets.emplacements`
  (`SortedDictionary<int, int>`, numéro du joueur vers rang de la fiche) et retirer le joueur de `enRecharge`.
- **Mesurer chez un joueur** (vitesse d'un recul, onde reçue) : un enregistreur branché sur
  `EditorApplication.update`, son résultat rangé par `AppDomain.CurrentDomain.SetData`, relu dans un appel suivant.
  Chaque appel du CLI fige l'éditeur un instant : une mesure de fluidité par script est faussée, la fluidité se juge
  en jouant.
- **Tests EditMode dans l'éditeur ouvert** : `unity command test run --mode editor --format json` (sans second
  éditeur, contrairement à `unity test`).
- Mémoire : avec un jeu lourd ouvert (4 Go), il restait 2,1 à 2,4 Go ; une partie à deux est passée, mais on reste
  sous le seuil de la section 1. Le dire à l'humain avant.
