# Froggy : règles pour les IA du projet

Ce fichier est la source commune pour toutes les IA de l'équipe (Claude Code le charge par `CLAUDE.md`, Codex le lit
directement). Tout se passe en français : les échanges, les documents, les messages de commit.

## Le jeu

Froggy et la recherche du cookie sacré : un jeu **exclusivement multijoueur** de type *Capture The Flag*, de 2 à 4
joueurs, où chacun incarne Froggy, une grenouille humanoïde. Lacs et nénuphars qui demandent des sauts précis, terrain
étroit, météo qui change la partie (la pluie accélère les grenouilles), caméra qui passe de la première à la troisième
personne en cours de partie.

**La V1 se joue dans un seul environnement : le jardin japonais**, sur une arène faite pour 4 joueurs et jouable aussi
à 3 et à 2, en troisième personne. Les autres environnements et la bascule en première personne viennent dans les
versions suivantes. Le périmètre exact est la page Notion « V1 »
(https://app.notion.com/p/4077e94430ba80d4b2fbed4ba5d9f6ee).

**La référence du design est la page Notion « Froggy – Présentation du projet »**
(https://app.notion.com/p/4077e94430ba80caac00da616ba0e73c). Le concept est en cours d'écriture : tout ce qui n'y est
pas écrit est une **proposition**, jamais une décision. Une contradiction avec Notion se signale, elle ne se tranche pas.

## La technique

- Unity **6000.6.7f1** (tout le monde exactement la même ; en changer se décide en équipe, par une PR), URP, Input
  System (`Assets/InputSystem_Actions.inputactions`).
- Réseau : **FishNet 4.7.4** (épinglé sur un commit, pas sur le tag : voir `Docs/Reseau/GUIDE.md`) et Multiplayer
  Play Mode pour tester à plusieurs. Vérifier le code de la version installée avant d'écrire du code réseau : ne
  jamais écrire une API FishNet de mémoire. Test hôte-client et règles d'un script réseau : `Docs/Reseau/GUIDE.md`.
- Gestion de projet : le [tableau GitHub Projects](https://github.com/orgs/froggy-with-the-sacred-cookie/projects/1),
  voir « Les tâches ».
- Dépôt : https://github.com/froggy-with-the-sacred-cookie/croak-and-crumb
- Habiller une arène : menu **Froggy ▸ Bâtisseur d'arène**, rangé en cinq étapes (① Terrain, ② Jeu, ③ Équilibre,
  ④ Décor, ⑤ Tester) avec la jauge « Prête pour un test », et **F5** pour jouer au point visé ; mode d'emploi
  `Docs/Outils/BATISSEUR.md` (commencer par « Premiers pas »). Toute nouvelle fonction de construction d'arène y
  va, pas dans une fenêtre de plus.
- Socle commun (`Assets/Froggy/Scripts/Socle/`) : `Modificateurs` (vitesse et saut de Froggy : chaque poste pose le
  sien, ils se multiplient) et `EvenementsPartie` (cookie ramassé, lâché, objet utilisé, cookie sacré : un poste émet,
  les autres écoutent). Passer par eux plutôt que d'appeler le code d'un autre poste.
- Scènes de test : une par poste dans `Assets/Froggy/Postes/`, presque vide, qui charge le jardin par-dessus
  (`DecorDeTest`). À deux sur le même poste : menu **Froggy ▸ Scènes de test ▸ Nouvelle scène perso** (ignorée par
  Git). Ce qui se partage vit dans un préfabriqué, pas dans une scène.
- Où ranger un asset : ce qui est à nous vit sous `Assets/Froggy/`, un pack externe sous `Assets/ThirdParty/<pack>/`,
  rien d'autre à la racine d'`Assets/`. La base de la Froggy (`PlayerTemplate`) est dans `Assets/Froggy/Joueur/`
  (son `LISEZMOI`). `Personnages/<Modèle>/` ne reçoit que des Froggy : tout FBX y est importé en humanoïde (le gobelin
  a son dossier `Gobelin/`). Les réglages et essais d'un poste vont dans `Postes/<n>_<poste>/`. Un fichier déplacé part
  avec son `.meta`, et les chemins `"Assets/…"` écrits dans le code se cherchent et se corrigent dans le même commit.
- Cookies (`Assets/Froggy/Scripts/Regles/`) : `ReglesCookies` porte les règles en C# pur (sans scène ni réseau, c'est
  ce que l'hôte fera tourner) ; `PartieCookies` les fait vivre dans une scène à partir des repères « Apparition de
  cookies » du Bâtisseur ; `PorteurCookies` donne le score d'une Froggy. Les valeurs sont dans `ReglagesCookies`.
  Faire lâcher un cookie à un joueur (un coup, un malus) : `PartieCookies.FaireLacher(porteur, direction)`.
- Déroulement d'une partie (même dossier) : `ReglesPartie` (compte à rebours, durée, gagnant, prolongation, en C# pur)
  et `DeroulementPartie` dans la scène. Le porteur du cookie sacré se donne par `PorteurCookieSacre` ; le début et la
  fin partent en événements du socle (`PartieCommencee`, `PartieFinie`). En fin de partie, « Rejouer » (bouton ou R,
  provisoire en attendant le salon) appelle `DeroulementPartie.Rejouer()`.
- Objets (`Assets/Froggy/Scripts/Objets/`) : `ReglesObjets` porte les règles en C# pur (boîtes, tirage, recharge) ;
  `PartieObjets` les fait vivre dans une scène à partir des repères « Boîte à objets » du Bâtisseur ; `PorteurObjet`
  donne l'emplacement d'objet d'une Froggy. Un objet = une `FicheObjet`, listée dans `ReglagesObjets`. L'effet d'un
  objet écoute `PorteurObjet.Utilise`, ou l'événement `ObjetUtilise` du socle. Premier effet : `EffetSuperCroassement`
  (élan, puis onde qui repousse les joueurs au sol ; son événement `Onde` sert au camouflage pour révéler les
  camouflés), réglé par `Postes/4_ObjetsCapacites/ReglagesSuperCroassement.asset`.
- Réapparition (même dossier) : `ReglesReapparition` (chute, délai, point le plus loin des autres, en C# pur) et
  `ApparitionJoueurs` dans la scène, qui pose les Froggy sur les repères « Apparition d'un joueur » du Bâtisseur.
  Avant de frapper un joueur, lire `PorteurCookies.Intouchable` (tombé ou tout juste réapparu) ; `FaireLacher` le
  refuse déjà. Le retour part en événement du socle (`JoueurReapparu`).
- Torgnole (même dossier) : `ReglesTorgnole` (cône, portée, 5 s de recharge, en C# pur) et `PartieTorgnoles` dans la
  scène (touche V ou bouton ouest). Un bot ou l'hôte frappe par `PartieTorgnoles.Frapper(porteur)` ; le coup part en
  événement du socle (`CoupDonne`). Réglages : `Postes/4_ObjetsCapacites/ReglagesTorgnole.asset`.
- Météo et événements (`Assets/Froggy/Scripts/Meteo/`) : `ReglesMeteo` (tirage pondéré, annonce, durée, en C# pur),
  `GestionnaireMeteo` dans la scène, une fiche `ReglagesMeteo` par arène (événements et poids) et une
  `DefinitionEvenement` par événement (durée, vitesse, saut, posés par les `Modificateurs`). Le rendu et le son d'un
  événement écoutent `Annonce`, `Debut` et `Fin` du gestionnaire ; il démarre et s'arrête avec la partie.
- Cookie sacré et gobelin (dossier `Regles/`) : `ReglesCookieSacre` (le gobelin arrive dans la dernière minute,
  capture, lâcher, ramassage, en C# pur) et `PartieCookieSacre` dans la scène, qui fait fuir le gobelin (`FuiteGobelin`,
  préfabriqué `Assets/Froggy/Gobelin/Gobelin_Kappa`, menu **Froggy ▸ Gobelin**) depuis le repère « Départ du gobelin »
  et tient `PorteurCookieSacre` à jour. Une torgnole (`CoupDonne`) ou un cookie lâché le font tomber ; un autre coup
  passe par `PartieCookieSacre.FaireLacher(porteur, direction)`. Réglages :
  `Postes/5_MeteoEvenements/ReglagesCookieSacre.asset`. En réseau, `CookieSacreReseau` envoie l'état de l'hôte ; chez
  un joueur qui n'est pas l'hôte (« Miroir »), il ne décide rien et le montre (`Refleter`).
- HUD de partie (`Assets/Froggy/Scripts/Interface/`) : `ContenuHud` écrit les textes (scores, minuteur, objet,
  torgnole, cookie sacré, météo, fin, en C# pur) et `HudPartie` les dessine. Il s'installe seul en jeu, ne lit que ce
  que la scène a, et éteint les affichages provisoires des postes (menu **Froggy ▸ HUD de partie** pour les
  retrouver). Un poste qui ajoute un affichage provisoire lui donne une propriété `AffichageProvisoire` et le branche
  dans `HudPartie.PrendreLaPlace` ; une nouvelle information de jeu s'ajoute au HUD, pas dans un `OnGUI` de plus.
- Nage (`Assets/Froggy/Scripts/Deplacement/`) : une `ZoneEau` (boîte déclencheur dont le dessus est la surface) sur
  chaque plan d'eau assez profond pour nager ; dedans, `DeplacementWow` passe la main à `NageFroggy` (flotte, nage à
  60 % de la course, plonge avec C ou le bouton est, ressort en sautant). Réglages : `ReglagesNage`. L'éclaboussure et
  le son écoutent `NageFroggy.Entree` et `Sortie`. Bassin d'essai : `Postes/1_MouvementCamera/BassinEssai.prefab`.
- Bots (`Assets/Froggy/Scripts/Bots/`, hors réseau) : `ReglesBot` (les décisions, en C# pur), `CerveauBot` sur la
  Froggy (il « appuie » sur les mêmes commandes qu'un humain : `DeplacementWow.Imposer`, `PartieTorgnoles.Frapper`,
  `PartieObjets.Utiliser`) et `BotsPartie` dans la scène (réglage « nombre de bots » : il copie la Froggy du joueur,
  construit la NavMesh au lancement et coupe les mannequins d'essai). Un repère « Apparition d'un joueur » par joueur,
  bots compris. Réglages : `Postes/3_ReglesBots/ReglagesBot.asset`. Un nouvel objet marche chez les bots sans rien
  changer : ils utilisent celui qu'ils portent quand un rival est proche. Un bot garde un `PlayerInput` **éteint**
  (exigé par `FroggyApparence`) : le joueur de cette machine se reconnaît à un `PlayerInput` **allumé**.
- Menus (`Assets/Froggy/Scripts/Interface/`) : la scène `Assets/Froggy/Scenes/Menu.unity`, première du build, porte le
  salon (P2-03) ; `MenuPrincipal` s'y installe seul et montre un écran à la fois (accueil de Julien, Paramètres,
  Rejoindre un salon, Salle d'attente), à la souris comme à la manette. La partie se charge par-dessus et le menu se
  cache jusqu'au retour. Les écrans ne passent que par `Salon.Local` ; leurs textes sont dans `ContenuSalle` (C# pur).
  Les réglages du joueur (volumes, secousses, flashs, vibrations, de 0 à 1 ; champ de vision en degrés ; touches) :
  `ReglagesJoueur.Actuels`, à lire au moment où l'effet part ; les tests écrivent dans un fichier de test, jamais dans
  le vrai. Paramètres (`PanneauReglages`) s'ouvre aussi depuis la pause.
- Commandes (P6-05, `Scripts/Interface/Commandes.cs`) : **toute touche du jeu est une action de la carte « Player »
  d'`InputSystem_Actions`**, avec une liaison clavier et une manette ; le joueur la change dans Paramètres ▸ Commandes.
  Un poste prend sa copie par `Commandes.Creer("NomAction")` (rendue par `Commandes.Liberer`), ou appelle
  `Commandes.Suivre(playerInput.actions)` ; jamais de `new InputAction` avec une touche écrite en dur. Une nouvelle
  commande s'ajoute aussi à `Commandes.Lignes`. Une touche affichée se lit sur l'action : son icône par
  `IconesTouches.Pour(action, index)` (manette branchée, toutes les touches du clavier du pack), sinon son nom
  (`GetBindingDisplayString`). Icônes **provisoires, à faire valider** : pack « Input Prompts » 1.5a de Kenney (CC0)
  dans `Interface/Resources/Touches`, importées en sprites par `ImportIconesTouches` ; `IconesTouches.Actives = false`
  remet le texte.
- Partie complète : `Assets/Froggy/Scenes/Partie_JardinJaponais.unity`, lancée par « Solo » (avec 3 bots). Comme une
  scène de test, elle charge le jardin par-dessus (`DecorDeTest`) et assemble tous les systèmes avec leurs vrais
  réglages (3 min, météo du jardin). **Un nouveau système de jeu s'y ajoute aussi**, pas seulement dans la scène de test
  de son poste, sans ses outils `Essai…`. Ses repères sont provisoires : à placer au Bâtisseur. Le salon charge encore la
  vitrine tant que les objets, la torgnole, la météo et le cookie sacré ne sont pas en réseau. `PartieJardinTests`
  vérifie que chaque système y est une fois, et **échoue si un système d'une scène de test de poste y manque**. En
  partie, Échap ou Start ouvre la pause (`MenuPause` : reprendre, rejouer, retour au menu ; le solo se fige).
- Froggy en ligne (`Assets/Froggy/Scripts/Reseau/FroggyReseau.cs`, P2-02, première version sans prédiction) : le salon
  fait apparaître `Prefabs/Reseau/FroggyReseau` pour chaque joueur, un objet réseau invisible (NetworkTransform, client
  autoritaire) plus une copie de la Froggy du jardin (`PlayerTemplate`), faite sur chaque machine comme les bots.
  Chez son propriétaire, elle garde contrôleur, caméra et touches ; chez les autres, elle suit la position reçue, corps
  cinématique, animée par `FroggyApparence.VitesseExterne` / `AuSolExterne`. Elle porte les cookies sous le numéro du
  joueur dans le salon. Revenir aux capsules de test : `prefabJoueur` du `Salon.prefab` sur `CapsuleReseau`.
- Partie en ligne (`Prefabs/Reseau/PartieReseau`, scripts dans `Scripts/Reseau/Partie/`, #128) : **l'hôte décide,
  les autres montrent** (leurs composants passent en « Miroir »). `PartieReseau` (cookies, temps, gagnant),
  `MeteoReseau`, `CookieSacreReseau`, `TorgnoleReseau` (la touche demande le coup à l'hôte ; le recul part chez le
  propriétaire de la Froggy touchée), `ObjetsReseau` (boîtes, objet tenu, « objet utilisé » envoyé à tous, numéros
  dans l'ordre du salon), `GelReseau` (l'hôte décide qui est gelé) et `ReapparitionReseau` (l'hôte décide la chute
  et la réapparition ; la Froggy d'un autre joueur est posée chez son propriétaire ; un repère « Apparition d'un
  joueur » par joueur dans le préfabriqué). Un effet qui ne pousse que les corps non
  cinématiques (super croassement) joue partout et pousse chaque Froggy chez son propriétaire. Une nouvelle mécanique
  de partie s'ajoute à ce préfabriqué avec son script réseau ; la tester à deux fenêtres (skill
  `test-multijoueur-local`, section 7).
- **Pas de sprint : un dash** (#134, décision de Florent du 18/11/2026). Action « Dash » de l'Input System (Maj gauche,
  R1). Au sol seulement, 4 m en 0,2 s, recharge 3 s (`Postes/1_MouvementCamera/Resources/ReglagesDash.asset`). Le
  geste est dans `DeplacementWow.Dasher` (sauter ou quitter le sol le coupe à la vitesse de course : les portées de
  saut ne changent pas) ; `ReglesDash` (C# pur) tient recharges et assommements ; `PartieDash` s'installe seul en jeu,
  lit la touche du joueur local, bouscule la Froggy percutée et assomme les deux si elles dashaient toutes les deux.
  Un bot ou l'hôte dashe par `PartieDash.Actuel.Dasher(froggy)`. Le choc n'est pas encore en réseau (aucun script dans `Scripts/Reseau/Partie/`).

## Les branches

- `main` : versions stables. Ne reçoit que des fusions de `develop`.
- `develop` : intégration, branche par défaut.
- Toutes deux protégées : pull request obligatoire, 1 relecture approuvée (une nouvelle modification annule
  l'approbation), discussions résolues, ni force-push ni suppression, admins compris.
- On travaille sur une branche partie de `develop` : `feature/<sujet>`, `fix/<sujet>`, `art/<sujet>`, puis une pull
  request vers `develop`. La branche s'efface à la fusion.
- Une IA ne commite jamais sur `main` ni sur `develop`, et n'ouvre une pull request que sur demande.

## Les tâches

- Chaque tâche est une issue de ce dépôt, sur le tableau, avec un **Domaine** et une **Priorité** (Haute, Moyenne,
  Basse).
- Colonnes : Backlog (à préciser), Prêt (claire, peut être prise), En cours, En revue (pull request prête à relire),
  Terminé.
- Une carte « En cours » ou « En revue » appartient à quelqu'un : ne pas la reprendre sans son accord.
- Une issue se crée par le formulaire **Tâche** ou **Bug** de GitHub. Une IA qui la crée en ligne de commande reprend
  les titres du formulaire en `###`, dans le même ordre : Domaine, Résultat attendu, C'est fini quand…, Références,
  Hors sujet (pour un bug : Domaine, Ce qui se passe, Ce qui devrait se passer, Pour le reproduire, Capture ou vidéo).
- La case Domaine d'une carte se remplit toute seule depuis le formulaire, quand elle est vide : une veille de la
  gestion de projet lance `Tools/tableau_domaine.py`. Une case remplie n'est jamais réécrite ; un bug en « Je ne sais
  pas » se règle à la main, au tri.
- Une tâche floue reste en Backlog, sa question en commentaire.
- Prendre une tâche, commiter, pousser, ouvrir et relire une pull request : skill `livrer`.

## Garde-fous (pour toute IA, toute session)

Chacun a coûté cher sur un projet précédent (Eku, Lovecraft, Glouton). Le détail est dans le skill `session-froggy`.

1. **Jamais de commit ni de push sans demande explicite.** Un push par demande : un accord donné une fois ne vaut pas
   pour la suite. Messages de commit en français, « Sujet : ce qui change ».
2. **D'autres personnes et d'autres IA travaillent en parallèle.** `git status` au départ ; des modifications qui ne
   viennent pas de toi se construisent par-dessus, elles ne se défont jamais. Relire un fichier juste avant de l'éditer.
3. **Ne jamais écrire un script ni recompiler pendant que Unity est en mode Play.** Vérifier l'état de l'éditeur **seul,
   dans un appel à part**, lire la réponse, puis agir (skill `unity-mcp`).
4. **Les vraies sauvegardes et réglages des joueurs sont intouchables** : les tests et captures passent par des
   fichiers de test.
5. **Outils payants (Meshy, Higgsfield, API d'images…)** : jamais sans annoncer la chaîne entière et son coût total, ce
   qui pourrait échouer, et vérifier que les références suffisent. Le gratuit d'abord. Un asset ne part en génération
   payante qu'après validation explicite de sa planche (skill `production-assets`).
6. **Les clés ne se lisent jamais** (`.env`, jetons) : les serveurs MCP les voient déjà.
7. **Garder l'ancienne version** quand on remplace un rendu, un modèle ou une ambiance, avec un moyen de basculer :
   l'équipe compare avant de choisir.
8. **Montrer le résultat** (capture, planche, son à écouter) avant de dire que c'est bon. Ce qui se juge à l'œil ou à
   l'oreille (ressenti, rendu, mixage) se juge en jeu par un humain : le dire dans le compte rendu.
9. **Ne pas inventer** : un fait, un chiffre ou une API qu'on n'a pas vérifiés se marquent comme hypothèses.

## Les skills du projet

Dans `.claude/skills/` (copie pour Codex dans `.agents/skills/`, voir plus bas). Charger celui du domaine **avant**
d'écrire ; plusieurs si besoin.

| Tu touches à… | Skill |
|---|---|
| Début de session, « continue », état des lieux, avant de dire « fini » | `session-froggy` |
| Prendre une tâche du tableau, commit, push, pull request, relecture | `livrer` |
| Piloter ou tester Unity par une IA (MCP), captures, tests, build | `unity-mcp` |
| Réseau, autorité, synchronisation, bots, simulation d'équilibrage, anti-boule-de-neige | `multijoueur-equilibrage` |
| Code FishNet (API de la version installée, prédiction, latence, port, NetworkManager) | `reseau-fishnet` |
| Tester à 2, 3 ou 4 sur une machine (Multiplayer Play Mode), mémoire, fenêtres de joueur | `test-multijoueur-local` |
| Contrôleur de Froggy, saut, nage, vitesse, tolérances | `ressenti-mouvement` |
| Caméra 1re / 3e personne, cadrage, secousse | `camera` |
| Coups, capacités, malus, gel d'image, vibration, lisibilité et équité | `impact-feedback` |
| Effet visuel, particule, animation de décor | `rendu-vivant` |
| Effet qui secoue, flashe ou vibre ; info à l'écran ; menus ; commandes | `accessibilite` |
| Commander, générer ou intégrer un asset ; prompt Gemini ou GPT ; outil payant | `production-assets` |
| Meshy, modèle 3D, personnage riggé, Blender, import dans Unity | `route-3d-meshy` |
| Objet modélisé par script Blender, reconnaissable de loin (cookies, ramassables) | `modele-3d-blender` |
| Direction artistique d'une arène, concept art, palette, juger des tirages | `direction-artistique` |
| Images animées 2D par GPT / Codex (HUD, icônes, flipbooks d'effets) | `animation-gpt` |
| Brancher un son, une musique, le mixage | `son-integration` |
| Composer une musique, fabriquer un effet sonore | `composition-audio` |
| Faune, eau, pluie, végétation, lumière : tout ce qui doit paraître vrai (recherche d'abord) | `recherche-reference` |
| Météo, eau, vent, shaders, rendu stylisé, performances du rendu | `tech-art` |

## Pour les skills

- On modifie **`.claude/skills/`**, puis on lance `python3 Tools/sync_skills.py` pour recopier vers `.agents/skills/`
  (Codex). Ne jamais éditer la copie.
- Un skill garde les leçons **avec leur raison** (ce qui s'est passé, quand). Une leçon nouvelle s'y ajoute ; une règle
  qui ne tient plus se corrige ou s'efface.
- D'où vient chaque skill, et ce qui n'a pas été repris : `Docs/IA/HERITAGE.md`.
