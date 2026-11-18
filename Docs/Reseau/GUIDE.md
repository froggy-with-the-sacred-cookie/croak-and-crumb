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

## Tester à deux sur une machine (5 minutes)

1. Ouvrir `Assets/Froggy/Scenes/Reseau_JardinJaponais.unity`. Elle charge le jardin japonais toute seule, sans le
   modifier, et y coupe le joueur local (`PlayerTemplate`).
2. **Window ▸ Multiplayer ▸ Multiplayer Play Mode** : cocher **Player 2** et attendre qu'il passe « Active ». Une
   seconde fenêtre Unity s'ouvre : c'est le deuxième joueur.
3. **Play** dans la fenêtre principale (le Player 2 suit).
   - Fenêtre principale : bouton **Server**, puis **Client**. Elle devient l'hôte.
   - Fenêtre du Player 2 : bouton **Client**.
4. **Cliquer dans la fenêtre** du joueur qu'on veut bouger, puis ZQSD (ou le stick). Sans ce clic, la fenêtre n'a pas
   le focus et Unity ignore le clavier. Sa capsule est **verte**, celle de l'autre **rouge** : chacun voit l'autre
   bouger.

Si le Player 2 reste sur un écran vide, il a démarré sans scène : arrêter Play, vérifier que la scène réseau est
ouverte dans la fenêtre principale, relancer.

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
- **Build Windows** : le projet est en IL2CPP, il faut les outils C++ de Visual Studio (charge de travail
  « Développement Desktop en C++ »).
