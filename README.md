# Croak & Crumb — Froggy et la recherche du cookie sacré

Un jeu **multijoueur** de type *Capture The Flag*, de 2 à 4 joueurs. Chacun incarne Froggy, une grenouille humanoïde :
lacs et nénuphars pour des sauts précis, terrain étroit, météo qui change la partie (la pluie accélère les grenouilles),
caméra à la première ou à la troisième personne.

La première version se joue dans un seul environnement, le **jardin japonais**, en troisième personne ; les autres
environnements viennent ensuite. Son périmètre est dans la page Notion
[« V1 »](https://app.notion.com/p/4077e94430ba80d4b2fbed4ba5d9f6ee).

> Le concept est en cours d'écriture. La référence du design est la page Notion
> [« Froggy – Présentation du projet »](https://app.notion.com/p/4077e94430ba80caac00da616ba0e73c).

## Prérequis

- **Unity 6000.6.7f1**, exactement (via Unity Hub), avec le module de build de ta plateforme. Une autre version
  réécrit les réglages du projet à l'ouverture et crée des conflits : changer de version se décide en équipe, par une PR.
- **Git LFS** : les images, modèles et sons sont stockés en LFS (`.gitattributes`).
- Réseau : **FishNet** (pas encore installé).

## Démarrer

```sh
git lfs install
git clone https://github.com/froggy-with-the-sacred-cookie/croak-and-crumb.git
cd croak-and-crumb
git switch develop
```

Puis ouvrir le dossier dans Unity Hub (*Add project from disk*) avec la version ci-dessus.

## Travailler à plusieurs

| Branche | Rôle |
|---|---|
| `main` | versions stables, ne reçoit que des fusions de `develop` |
| `develop` | intégration, branche par défaut |
| `feature/<sujet>`, `fix/<sujet>`, `art/<sujet>` | ton travail, partie de `develop` |

`main` et `develop` sont protégées : on passe toujours par une **pull request** vers `develop`, avec **une relecture
approuvée** d'un autre membre et les discussions résolues. Pas de force-push. La branche s'efface après la fusion.

```sh
git switch develop && git pull
git switch -c feature/saut-nenuphar
# … travail, commits …
git push -u origin feature/saut-nenuphar   # puis ouvrir la pull request vers develop
```

Messages de commit en français, sous la forme « Sujet : ce qui change » (exemple :
`Caméra : bascule première / troisième personne`).

Unity et git : fermer une scène avant de tirer les changements d'un autre sur la même scène, et prévenir l'équipe avant
de modifier une scène ou un préfabriqué partagé (les conflits de scène se fusionnent mal).

## Travailler avec une IA

Le projet est prêt pour Claude Code et Codex :

- [`AGENTS.md`](AGENTS.md) : les règles communes à toutes les IA (lues par Codex, chargées par [`CLAUDE.md`](CLAUDE.md)).
- `.claude/skills/` : 18 skills (savoir-faire repris de prototypes précédents : mouvement, caméra, multijoueur, assets
  avec Gemini / Meshy / Blender, son, tech art…). Copie pour Codex dans `.agents/skills/` : après une modification,
  `python3 Tools/sync_skills.py`.
- [`Docs/IA/HERITAGE.md`](Docs/IA/HERITAGE.md) : d'où vient chaque skill.
- `Tools/` : scripts d'appoint (Meshy, Blender, client du MCP de Unity, outils d'art, veille du tableau).

## Organisation

Les tâches sont suivies dans le [tableau GitHub Projects](https://github.com/orgs/froggy-with-the-sacred-cookie/projects/1).
Domaines : développement (arènes, météo, caméra, personnages, multijoueur, capacités, objets, score, équilibrage,
menus), animation, sound design, graphisme (HUD), modélisation 3D, réseau, gestion de projet.

Prendre une tâche et la livrer jusqu'à la pull request : skill [`livrer`](.claude/skills/livrer/SKILL.md). Une issue
se crée par le formulaire **Tâche** ou **Bug** (*New issue*), une pull request suit le
[modèle](.github/pull_request_template.md). Une idée de design va dans Notion.
