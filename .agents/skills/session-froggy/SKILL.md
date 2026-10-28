---
name: session-froggy
description: Garde-fous, statut des décisions, vérifications et endroits où consigner, pour toute session de travail sur Froggy (code Unity, réseau, assets, sons, design). À charger AU DÉBUT de chaque session qui touche au projet et AVANT de dire qu'un travail est fini. Aussi quand on dit « continue », « implémente », « fais la suite », « corrige », ou qu'on demande un état des lieux. Indique quel autre skill du projet charger selon le domaine touché.
---

# Session de travail sur Froggy

Ce skill rassemble ce qui a coûté cher à apprendre sur les prototypes précédents (Eku, Lovecraft, Glouton) : casser le
travail d'un autre, toucher à une vraie sauvegarde, présenter une proposition comme une décision, recompiler pendant
qu'un humain joue, dépenser des crédits pour rien, annoncer « fini » sans vérification.

## 1. Avant d'écrire

1. **Situer chaque mécanique** : écrite dans Notion (« Froggy – Présentation du projet »), choisie pour le prototype,
   proposition, ou à définir. Une contradiction avec Notion se **signale**, elle ne se tranche pas. Le concept est en
   cours d'écriture par l'équipe : en cas de doute, c'est une proposition.
2. **D'autres travaillent en parallèle** (des devs, d'autres sessions d'IA, Codex). `git status` au départ ; des
   modifications non commitées qui ne viennent pas de toi se construisent par-dessus, ne se défont jamais. Relire un
   fichier juste avant de l'éditer. Une IA qui livre dans un dossier à part (`proposals/`, par exemple) n'écrit pas
   dans les scripts du jeu sans accord (Glouton, 27 octobre 2026 : Codex livrait ses propositions à côté, intégrées
   ensuite une à une).
3. **Charger le skill du domaine** (tableau d'`AGENTS.md`), plusieurs si besoin.
4. **Penser original d'abord.** Sur Glouton (27 octobre 2026), le premier prototype était une copie trop fidèle de
   Hole.io et a été repensé. Une mécanique proposée dit ce qu'elle a de propre à Froggy (la grenouille, l'eau, les
   cookies, la météo), pas seulement à quel jeu elle ressemble.

## 2. Garde-fous

- **Git** : jamais de commit, de push, ni d'index modifié sans demande (ni `git add`, ni `git rm`, ni `git stash`).
  Un écart s'avoue et se répare (`git reset -q HEAD -- <chemins>`). Chaque push se redemande.
- **Unity en mode Play** : ne jamais écrire un `.cs` ni recompiler tant qu'une partie tourne. Vérifier seul, dans un
  appel à part, puis agir (skill `unity-mcp`, section 1). C'est l'erreur la plus répétée de Lovecraft (trois fois).
- **Sauvegardes et réglages réels intouchables** : les tests redirigent vers des fichiers de test et les effacent. Si
  le projet n'a pas encore de mode test, en proposer un avant le premier test qui écrirait sur le disque.
- **Outils payants** : chaîne entière et coût total annoncés, validation explicite de la planche, gratuit d'abord
  (skill `production-assets`, section 1). Les clés (`.env`) ne se lisent jamais.
- **Supprimer** : déplacer plutôt que supprimer un asset (le mode auto refuse `AssetDatabase.DeleteAssets`, et on
  garde une trace). Garder l'ancienne version commutable tant que l'équipe n'a pas tranché.
- **Un serveur MCP ajouté en cours de session** n'apparaît qu'à la session suivante : le dire, et passer par la ligne
  de commande en attendant (`Blender -b -P script.py`, client HTTP du MCP de Unity).
- **Une IA ne compose pas les arènes à la place de l'équipe.** Sur Eku (22 octobre 2026), après une journée de placement
  de décor par règles jugé « aléatoire » : « avec du code, et toi une IA qui n'y voit rien, le jeu ne ressemble pas à
  grand-chose ». L'IA fabrique les outils, les éléments et les préfabriqués ; la disposition d'une arène se décide dans
  l'éditeur par un humain, ou se suggère en mots et en images.

## 3. Avant de dire « c'est fait »

1. **Compiler sans erreur** (console Unity lue, pas supposée) et faire tourner les tests du domaine (`run_tests` en
   EditMode ; skill `unity-mcp`, section 4). Un comportement nouveau reçoit **sa vérification dans un test** ; une
   sonde jetable va dans le scratchpad.
2. **Montrer** : capture en jeu, planche, son à écouter. Ouvrir l'image (`open <fichier>`) plutôt que de donner un lien.
3. **Dire ce qui n'a pas été vu** : ce qui n'a tourné qu'en headless ou en simulation, et ce qui se juge en jeu par un
   humain (ressenti, rendu, mixage, équilibrage à plusieurs).
4. **Consigner** :
   - les décisions de design : dans Notion, seulement si l'équipe le demande (sinon, les proposer dans le compte rendu) ;
   - une règle technique ou un piège nouveau : dans le skill concerné et, si toute l'équipe doit le savoir, dans
     `AGENTS.md` ;
   - l'état et la suite : dans la mémoire de la session et dans le compte rendu.

Le compte rendu dit ce qui est fait, ce qui reste bloqué et pourquoi, ce qui n'a été vu qu'en headless, et les
questions qui attendent l'équipe. Pour un choix esthétique : plusieurs vraies propositions côte à côte, avec une
recommandation, puis l'équipe choisit.
