# Le réseau : tester à deux et rendre un script compatible

Pour tous les postes : gameplay, caméra, effets, son, UI. Froggy se joue **uniquement en multijoueur**, avec
**FishNet**. Une règle à garder en tête : **l'hôte décide, les clients demandent.** Ce guide vient de la tâche P2-01
(#22) ; les règles d'autorité et d'équilibrage sont dans le skill `multijoueur-equilibrage`.

## La version installée

- **FishNet 4.7.4**, épinglé dans `Packages/manifest.json` sur le commit `a2e2c3e1` de la branche `main` de FishNet.
  **Pas sur le tag `4.7.4`** : il ne compile pas (bug FishNet
  [#1110](https://github.com/FirstGearGames/FishNet/issues/1110)). On repassera sur un tag quand FishNet en publiera un
  sain.
- **Multiplayer Play Mode 3.0.0**, livré avec Unity 6000.6.7f1, pour jouer à plusieurs sur une seule machine.
- Mettre FishNet à jour se décide en équipe, par une pull request, comme la version de Unity.

## Tester à 2, 3 ou 4 sur une machine (5 minutes)

Rien à installer : Multiplayer Play Mode et FishNet sont dans le projet. Chaque joueur en plus est **un Unity de
plus** : compter environ 3 Go de mémoire par fenêtre. Sur un portable à 16 Go, rester à **2** ; viser 4 sur un poste
à 32 Go (le 18/11/2026, les Players 3 et 4 ont planté faute de mémoire sur un poste à 16 Go).

1. **Ouvrir la scène** : menu **Froggy ▸ Réseau ▸ 1. Ouvrir la scène de test** (scène du poste Réseau,
   `Assets/Froggy/Postes/2_Reseau/Test_Reseau.unity`). Si Unity propose d'enregistrer, répondre « Save ».
2. **Choisir les joueurs** : menu **Froggy ▸ Réseau ▸ 2. Fenêtre Multiplayer Play Mode**. Cocher **Player 2** (test
   à 2), **2 et 3** (à 3) ou **2, 3 et 4** (à 4). Attendre que chaque ligne affiche « Active » : une fenêtre Unity
   s'ouvre par joueur (la première fois, une à deux minutes). Unity ne permet pas de cocher ces cases par script.
3. **Choisir la connexion** : menu **Froggy ▸ Réseau ▸ 3. Connexion ▸** *Parfait*, *Wifi moyen* ou *Mauvaise
   connexion*. La coche montre celle qui est choisie ; elle vaut pour toutes les fenêtres.
4. **Lancer** : bouton ▶ (Play) en haut de la fenêtre principale. Les autres fenêtres suivent. **Aucun bouton à
   cliquer** : la fenêtre principale devient l'hôte, les autres se connectent seules en quelques secondes.
5. **Jouer** : cliquer dans la fenêtre d'un joueur, puis ZQSD (ou le stick). Sans ce clic, la fenêtre n'a pas le
   focus et Unity ignore le clavier. Sa capsule est **verte**, celles des autres **rouges**. Le panneau en haut à
   droite donne le rôle (Hôte, Client n°), le nombre de joueurs, le ping et la connexion simulée.

Pour arrêter : ▶ à nouveau dans la fenêtre principale. Pour fermer les joueurs : décocher leurs cases.

| Connexion | Aller-retour | Pertes | Ping affiché (mesuré le 18/11) | À quoi ça sert |
|---|---|---|---|---|
| Parfait | 0 ms | 0 % | ≈ 33 ms | Vérifier qu'une fonction marche |
| Wifi moyen | 100 ms | 1 % | ≈ 150-170 ms | Ce que vivront la plupart des joueurs |
| Mauvaise connexion | 250 ms | 5 % (+ 2 % dans le désordre) | ≈ 300 ms | Chercher ce qui casse |

Le ping affiché dépasse la valeur simulée d'environ un tick (33 ms à 30 ticks par seconde) : c'est le temps de base de
FishNet. **F8** pendant le jeu passe à la connexion suivante dans la fenêtre où l'on est seulement ; **F9** cache le
panneau. La simulation ne marche qu'en éditeur et en build de développement.

| Ce qu'on voit | Quoi faire |
|---|---|
| Une fenêtre de joueur reste sur un fond uni sans jardin | Arrêter le Play, refaire l'étape 1, relancer |
| Le panneau dit « Connexion à l'hôte… » plus de 15 secondes | Vérifier que la fenêtre principale est bien en Play |
| Le panneau de l'hôte dit « Serveur impossible : ports … pris » | Fermer et rouvrir Unity |
| Une fenêtre de joueur se ferme toute seule | Mémoire pleine : décocher un joueur, fermer d'autres programmes |
| La capsule ne bouge pas au clavier | Cliquer d'abord dans la fenêtre de ce joueur |
| Une ligne de Multiplayer Play Mode reste sur « Activating » | Attendre ; au-delà de 3 minutes, décocher puis recocher |

L'hôte écoute sur le port 7770, ou le suivant s'il est pris (FishNet garde parfois l'ancien port ouvert, voir
« Pièges connus »). Il note le port choisi dans `%TEMP%\froggy-test-reseau-port.txt`, que les autres fenêtres lisent.
Le code : `Assets/Froggy/Scripts/Reseau/TestReseau.cs` (démarrage), `PanneauTestReseau.cs`, réglages
`Assets/Froggy/Reseau/ReglagesTestReseau.asset`, le tout dans le prefab `Assets/Froggy/Prefabs/Reseau/TestReseau.prefab`.

## Jouer par code (salon)

Le salon (P2-03) : un joueur crée une partie et reçoit un **code de 4 lettres** (par exemple `KRBF`, jamais de O ni
de I). Les autres tapent ce code pour le rejoindre, se disent prêts, puis l'hôte lance. Pour l'instant, le code se
trouve **sur le même réseau local** (même machine ou même wifi) ; jouer depuis deux maisons viendra avec le relais
(P2-05).

1. **Ouvrir le salon de test** : menu **Froggy ▸ Réseau ▸ 4. Ouvrir le salon de test (jouer par code)**
   (`Assets/Froggy/Postes/2_Reseau/Salon_Reseau.unity`). Si Unity propose d'enregistrer, répondre « Save ».
2. **Choisir les joueurs** et **la connexion** : étapes 2 et 3 de la section précédente.
3. **Lancer** : ▶ (Play). Ici, **rien ne démarre seul** : chaque fenêtre affiche le panneau « Salon » en haut à gauche.
4. **Fenêtre principale** : **Créer un salon**. Le code s'affiche en grand.
5. **Chaque autre fenêtre** : cliquer dans la fenêtre, taper le code dans « Code » (minuscules et tirets acceptés),
   **Rejoindre**. Tous les joueurs apparaissent dans chaque fenêtre, avec leur couleur.
6. **Couleur suivante** si on veut changer (deux joueurs n'ont jamais la même), puis **Prêt**.
7. **L'hôte : Lancer la partie** (grisé tant qu'on est seul ou qu'un joueur n'est pas prêt). Le jardin s'ouvre chez
   tout le monde, une capsule par joueur à sa couleur.
8. **L'hôte : Revenir au salon**. Tout le monde revient, « pas prêt », et peut relancer. À la fin d'une vraie partie
   (événement « partie finie »), le retour se fait seul après 5 secondes.

| Message | Ce qu'il veut dire |
|---|---|
| « Aucun salon KRBF trouvé sur ce réseau » | Mauvais code, salon pas encore créé, ou autre réseau (pas de relais pour l'instant) |
| « Code inconnu de cet hôte » | L'hôte trouvé a un autre code (rare) |
| « Salon plein (4 joueurs) » | Il y a déjà 4 joueurs |
| « Partie en cours » | On ne rejoint pas une partie commencée : attendre le retour au salon |
| « Déconnecté : l'hôte a fermé le salon… » | L'hôte est parti ; recréer ou rejoindre un autre salon |
| Windows demande d'autoriser Unity sur le réseau | Accepter (réseau privé) : sinon les autres machines ne trouvent pas le salon |

**F10** cache le panneau. Ce panneau est **provisoire** : les vrais écrans (titre, salon, fin de partie) sont faits par
le poste 6 (P6-03) et se branchent sur le même code.

### Brancher un écran sur le salon (poste 6)

Tout passe par `Salon.Local` (`Assets/Froggy/Scripts/Reseau/Salon/Salon.cs`), sans toucher au réseau. Le panneau
provisoire `PanneauSalon.cs` en est un exemple complet.

```csharp
using Froggy.Reseau;

Salon salon = Salon.Local;
salon.Change += Rafraichir;          // joueurs, prêt, couleur, phase, connexion : tout changement
salon.Erreur += AfficherMessage;     // texte prêt à afficher

salon.Creer();                       // devenir l'hôte ; salon.Code donne le code
salon.Rejoindre(texteTape);          // le code tel que tapé
salon.SeDirePret(true);
salon.CouleurSuivante();             // ou salon.ChoisirCouleur(indice)
salon.Lancer();                      // hôte ; possible si salon.PeutLancer
salon.RevenirAuSalon();              // hôte, pendant la partie
salon.Quitter();

// À lire pour afficher : salon.Connecte, salon.EstHote, salon.Occupe (recherche en cours), salon.Etat,
// salon.Phase (Salon / EnPartie), salon.Membres (Numero, Couleur, Pret, Hote), salon.Moi, salon.Couleur(indice).
```

Les règles (4 joueurs au plus, couleur unique, lancement quand tous les invités sont prêts) sont dans `EtatSalon.cs`
et testées dans `Assets/Froggy/Editor/Tests/SalonTests.cs`. Le prefab `Assets/Froggy/Prefabs/Reseau/Salon.prefab`
contient le NetworkManager, le salon, la recherche par code, le panneau, les 4 points d'apparition et la caméra ; la
scène de partie est un champ de `Salon` (`Vitrine_JardinJaponais` pour l'instant).

## Les règles de partie en réseau (P2-04)

**Seul l'hôte décide** : cookie ramassé (le plus proche ; à égalité, le plus petit numéro), cookie lâché, score,
temps, gagnant, porteur du cookie sacré. Il fait tourner les règles du poste 3 (`PartieCookies`, `DeroulementPartie`)
telles quelles. Chez les autres joueurs, ces composants sont en **Miroir** : ils ne décident rien et montrent ce que
l'hôte a décidé. Tout passe par le canal fiable : rien ne se perd, même avec « Mauvaise connexion ».

Comment le voir : le salon (section précédente), **Lancer la partie**. Le jardin s'ouvre avec 5 cookies provisoires,
compte à rebours, partie de 30 s (`ReglagesPartie_Essai`), score en haut à gauche, temps en haut, gagnant à la fin,
puis retour au salon. Le panneau du salon passe en bas à gauche pendant la partie.

**Pour un poste qui écoute** (son, effets, HUD) : rien ne change. `EvenementsPartie` part chez **chaque** joueur
(l'hôte renvoie les siens aux autres) ; au moment où l'événement arrive, `PorteurCookies.Nombre` et
`DeroulementPartie.Gagnant` sont déjà à jour.

**Pour un poste qui décide** (torgnole P4-10, cookie sacré P5-08) : décider **sur l'hôte seulement**, par
`PartieReseau.Actuelle` (`Assets/Froggy/Scripts/Reseau/Partie/PartieReseau.cs`) :

```csharp
var partie = PartieReseau.Actuelle;              // null hors partie
if (partie != null && partie.IsServerStarted)    // seulement sur l'hôte
{
    partie.FaireLacher(numeroJoueur, directionDuCoup);   // la torgnole fait tomber un cookie
    partie.PorteurCookieSacre = numeroJoueur;            // ReglesPartie.Personne : personne
}
```

Le prefab `Assets/Froggy/Prefabs/Reseau/PartieReseau.prefab` (règles du poste 3 + 5 repères **provisoires** dans le
jardin) apparaît quand tous les joueurs ont leur personnage et disparaît au retour au salon. Une vraie arène
apportera ses repères.

## Les 5 règles d'un script réseau

1. **Un objet partagé porte un `NetworkObject`.** Sans lui, chaque joueur a sa propre copie, invisible des autres. Un
   objet réseau créé en jeu doit être un prefab : FishNet l'ajoute tout seul à `Assets/DefaultPrefabObjects.asset`.
2. **Un script qui touche à l'état partagé hérite de `NetworkBehaviour`** (`using FishNet.Object;`) au lieu de
   `MonoBehaviour`.
3. **Seul le propriétaire lit les entrées** : `if (!IsOwner) return;` en tête de `Update`. Sinon chaque joueur pilote
   toutes les grenouilles.
4. **`OnStartClient()` plutôt que `Start()`** pour ce qui dépend du propriétaire (caméra qui suit, HUD, couleur) :
   dans `Awake` et `Start`, FishNet ne sait pas encore à qui est l'objet. Appeler `base.OnStartClient()` en premier.
5. **Ce qui compte pour la partie se décide sur l'hôte** : score, capture du cookie, dégâts, réapparition. Un client
   envoie ses entrées ou une demande, jamais un résultat (« j'ai pris le cookie »).

Exemple complet et court : `Assets/Froggy/Scripts/Reseau/CapsuleReseau.cs`.

```csharp
using FishNet.Object;

public class MonScript : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (IsOwner) { /* caméra, HUD : seulement pour son propre joueur */ }
    }

    private void Update()
    {
        if (!IsOwner) return;   // les autres joueurs ne lisent pas mes touches
        // lire les entrées, bouger son personnage
    }
}
```

## Ce qui n'a pas besoin du réseau

- **Les effets de ressenti** (secousse, gel d'image, vibration, particules, sons) : chaque joueur les joue chez lui,
  en réaction à un événement réseau. On ne les synchronise jamais image par image.
- **Le décor qui se reconstruit à l'identique** (même graine) : on n'envoie pas ce qui se recalcule. Le jardin de la
  scène de test est chargé par chaque joueur chez lui.
- **Les menus, réglages et options d'accessibilité** : locaux.

## À mettre dans toute scène réseau

Le plus simple : poser le prefab **`Assets/Froggy/Prefabs/Reseau/TestReseau.prefab`**, qui contient déjà tout ce qui
suit (plus le démarrage automatique, le panneau et une caméra au-dessus du jardin). À la main :

- Le prefab **NetworkManager** de FishNet (Project : *Packages ▸ FishNet: Networking Evolved ▸ Demos ▸ Prefabs*).
- Sur ce NetworkManager, **Spawnable Prefabs = `Assets/DefaultPrefabObjects.asset`**. Sans ça, rien ne marche
  (console : « SpawnablePrefabs is null on NetworkManager »).
- Sur son **PlayerSpawner** : le prefab du joueur et les points d'apparition (utilisés dans l'ordre).
- Un **EventSystem** (*GameObject ▸ UI ▸ Event System*) : le prefab FishNet n'en a pas, et sans lui les boutons
  Server / Client ne répondent pas.

## Ce que fait la scène de test, et ce qu'elle ne fait pas

Les capsules utilisent un `NetworkTransform` en mode **client autoritaire** (le réglage par défaut de FishNet) : chaque
joueur déplace sa capsule et les autres reçoivent sa position. C'est simple et suffit pour vérifier la connexion, mais
un client pourrait tricher. Froggy passera à l'autorité de l'hôte (entrées envoyées, position prédite puis recalée)
avec P2-02 (#23).

## Pièges connus

- **Écrire une API FishNet de mémoire** : relire le code du package installé, les exemples trouvés en ligne visent
  souvent une autre version.
- **Modifier un script pendant le mode Play** : Unity recompile et la partie casse.
- **Régler par script un prefab posé dans une scène** (outils d'éditeur, IA) : sans
  `PrefabUtility.RecordPrefabInstancePropertyModifications`, la modification disparaît au Play.
- **Joueurs qui n'apparaissent pas** : désactiver Error Pause dans la console et lire la première erreur.
- **Port qui reste pris après Stop** : dans l'éditeur, FishNet 4.7.4 a gardé le port du serveur ouvert jusqu'au
  redémarrage de Unity (vu le 18/11/2026, cause non trouvée dans FishNet). Arrêter le serveur soi-même dans
  `OnApplicationQuit` l'a évité ; `TestReseau` passe en plus au port suivant si besoin.
- **Arrivée pendant une partie** : FishNet envoie à tout nouveau joueur les scènes « globales » déjà chargées, donc il
  tomberait en pleine partie. Le salon refuse donc l'entrée tant que la partie dure (P2-06 décidera mieux).
- **Un message (RPC) arrive avant l'état copié** : FishNet envoie l'état synchronisé toutes les 0,1 s, un message
  tout de suite. Un client qui réagit au message lirait l'ancien score : `PartieReseau` met donc le nouveau score (ou
  le gagnant) dans le message. Vu le 18/11/2026 (« partie finie » reçue, gagnant encore vide).
- **Unity qui plante à plusieurs fenêtres** (« d3d12: Device removed ») : machine à bout de mémoire. Après une
  réouverture ou une recompilation, Unity lance 3 « AssetImportWorker » (≈ 4 Go) ; les fermer avant le Play libère de
  la place (Unity les relance seul si besoin).
- **Recharger la scène qui contient le NetworkManager** en fait apparaître un second. Le salon garde donc sa scène
  chargée et ajoute / retire la scène de partie par-dessus.
- **Mémoire** : chaque joueur de Multiplayer Play Mode est un Unity complet (≈ 3 Go). Quand la mémoire manque, Windows
  agrandit son fichier d'échange sur C: et les fenêtres de joueur se ferment sans message.
- **Build Windows** : le projet est en IL2CPP, il faut les outils C++ de Visual Studio (charge de travail
  « Développement Desktop en C++ »).
