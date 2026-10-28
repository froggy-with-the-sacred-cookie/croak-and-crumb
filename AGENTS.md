# Froggy : règles pour les IA du projet

Ce fichier est la source commune pour toutes les IA de l'équipe (Claude Code le charge par `CLAUDE.md`, Codex le lit
directement). Tout se passe en français : les échanges, les documents, les messages de commit.

## Le jeu

Froggy et la recherche du cookie sacré : un jeu **exclusivement multijoueur** de type *Capture The Flag*, de 2 à 4
joueurs, où chacun incarne Froggy, une grenouille humanoïde. Arènes variées (jungle, marécage, désert, oasis), lacs et
nénuphars qui demandent des sauts précis, terrain étroit, météo qui change la partie (la pluie accélère les grenouilles),
caméra qui passe de la première à la troisième personne en cours de partie.

**La référence du design est la page Notion « Froggy – Présentation du projet »**
(https://app.notion.com/p/4077e94430ba80caac00da616ba0e73c). Le concept est en cours d'écriture : tout ce qui n'y est
pas écrit est une **proposition**, jamais une décision. Une contradiction avec Notion se signale, elle ne se tranche pas.

## La technique

- Unity **6000.6.7f1** (tout le monde exactement la même ; en changer se décide en équipe, par une PR), URP, Input
  System (`Assets/InputSystem_Actions.inputactions`).
- Réseau : **FishNet** (choisi dans Notion, pas encore installé). Vérifier la documentation de la version installée
  avant d'écrire du code réseau : ne jamais écrire une API FishNet de mémoire.
- Gestion de projet : le [tableau GitHub Projects](https://github.com/orgs/froggy-with-the-sacred-cookie/projects/1),
  voir « Les tâches ».
- Dépôt : https://github.com/froggy-with-the-sacred-cookie/croak-and-crumb

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
