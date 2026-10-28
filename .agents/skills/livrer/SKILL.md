---
name: livrer
description: Prendre une tâche du tableau GitHub Projects de Froggy, puis la livrer jusqu'à la fusion : branche, commits, push, pull request vers develop, relecture et suivi de la carte. À charger quand on prend une tâche, avant un commit, un push ou une pull request, pour relire la pull request d'un autre, et quand on dit « livre », « commite », « pousse » ou « ouvre la PR ».
---

# Livrer une tâche

Les règles sont dans `AGENTS.md` (branches, tâches, garde-fous) et les vérifications dans le skill `session-froggy`. Ce
skill dit l'ordre des gestes. Il ne donne aucun droit en plus : chaque commit et chaque push se demandent (garde-fou 1).

## 1. Prendre une tâche

1. Sur le [tableau](https://github.com/orgs/froggy-with-the-sacred-cookie/projects/1), prendre une carte « Prêt » de
   son domaine, priorité Haute d'abord. Une carte « En cours » ou « En revue » appartient à quelqu'un.
2. Lire l'issue en entier. Si le résultat attendu ou la façon de le vérifier manque, poser la question en commentaire
   et laisser la carte où elle est.
3. S'assigner l'issue (`gh issue edit <n> --add-assignee @me`), passer la carte « En cours », et écrire sa branche
   dans un commentaire de l'issue.

Une IA change le statut d'une carte avec `gh project item-edit` : les identifiants se lisent avec
`gh project field-list 1 --owner froggy-with-the-sacred-cookie --format json` et
`gh project item-list 1 --owner froggy-with-the-sacred-cookie --format json`. Un humain fait glisser la carte.

## 2. Travailler sur sa branche

```sh
git switch develop && git pull
git switch -c feature/<sujet>      # ou fix/<sujet>, art/<sujet>
```

- Une branche par tâche, partie de `develop` à jour.
- `git status` au départ (garde-fou 2), puis charger le skill du domaine.
- Prévenir l'équipe avant de toucher une scène ou un prefab partagé.

## 3. Avant chaque commit

1. Les vérifications de `session-froggy`, section 3 : compiler sans erreur, faire tourner les tests, montrer.
2. Ajouter seulement les fichiers de la tâche, puis relire `git diff --staged`. Ni `Library`, `Temp`, `Logs`, `Build`,
   `Captures`, ni secret.
3. Les nouveaux assets et scripts ont leur `.meta`. Les fichiers lourds passent par LFS : `git lfs status` les liste.
4. Le dépôt est public : un asset externe garde sa source et sa licence, et cette licence permet de le publier.
5. Message en français, « Sujet : ce qui change » (exemple : `Caméra : bascule première / troisième personne`).

## 4. Pousser et ouvrir la pull request

1. Récupérer `develop` : `git fetch origin`, puis `git merge origin/develop` si elle a bougé. Pas de rebase ni de
   force-push sur une branche déjà poussée : d'autres l'ont peut-être tirée.
2. Pousser, sur demande : `git push -u origin <branche>`.
3. Ouvrir la pull request vers `develop`, sur demande : `gh pr create --base develop --draft`. Elle suit le
   [modèle](../../../.github/pull_request_template.md) : `Closes #n` en première ligne si elle termine l'issue (l'issue
   se ferme à la fusion), `Partie de #n` sinon.
4. Elle reste en brouillon tant que les vérifications ne sont pas faites ou qu'une question attend l'équipe. Prête
   (`gh pr ready <n>`) : passer la carte « En revue ». Repassée en brouillon (`gh pr ready <n> --undo`) : « En cours ».

## 5. Relire et fusionner

- Une approbation d'un autre membre est obligatoire. Une nouvelle modification annule l'approbation, et les discussions
  doivent être résolues.
- Qui relit commence par « Décisions prises par l'IA », puis teste en jeu ce qui se juge à l'œil ou à l'oreille.
- Une IA ne fusionne pas. L'auteur fusionne une fois l'approbation reçue ; la branche s'efface toute seule sur GitHub.
- En local, ensuite : `git switch develop && git pull && git branch -d <branche>`.
- Laisser le relais dans l'issue : ce qui est fait, la pull request, les vérifications, la suite.

`develop` rejoint `main` aux jalons, par une pull request de `develop` vers `main`, avec les mêmes règles.
