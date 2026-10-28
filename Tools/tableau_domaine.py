"""Recopie le Domaine choisi dans le formulaire d'une issue sur sa carte du tableau GitHub Projects.

Le formulaire Tâche ou Bug écrit le domaine dans le texte de l'issue, que le tableau ne lit pas. Ce script remplit la
case Domaine des cartes où elle est vide ; une case déjà remplie, à la main ou par lui, n'est jamais modifiée.
Il passe par `gh` (droit `project`) et tourne en veille sur la machine de la gestion de projet :
    python3 Tools/tableau_domaine.py          # remplit les cases vides
    python3 Tools/tableau_domaine.py --essai  # dit ce qu'il ferait, sans rien écrire
"""
import json, re, subprocess, sys

ORGA, NUMERO, CHAMP = "froggy-with-the-sacred-cookie", 1, "Domaine"

LIRE = """query($orga: String!, $numero: Int!, $champ: String!, $apres: String) {
  organization(login: $orga) { projectV2(number: $numero) {
    id
    field(name: $champ) { ... on ProjectV2SingleSelectField { id options { id name } } }
    items(first: 100, after: $apres) {
      pageInfo { hasNextPage endCursor }
      nodes {
        id
        fieldValueByName(name: $champ) { ... on ProjectV2ItemFieldSingleSelectValue { name } }
        content { ... on Issue { number body } }
      }
    }
  } }
}"""

ECRIRE = """mutation($projet: ID!, $carte: ID!, $champ: ID!, $option: String!) {
  updateProjectV2ItemFieldValue(input: {projectId: $projet, itemId: $carte, fieldId: $champ,
                                        value: {singleSelectOptionId: $option}}) { projectV2Item { id } }
}"""


def graphql(requete, **variables):
    """Appelle l'API GraphQL de GitHub par gh et renvoie son champ data."""
    args = ["gh", "api", "graphql", "-f", f"query={requete}"]
    for nom, valeur in variables.items():
        if valeur is not None:
            args += ["-F" if isinstance(valeur, int) else "-f", f"{nom}={valeur}"]
    r = subprocess.run(args, capture_output=True, text=True, encoding="utf-8")
    if r.returncode:
        sys.exit(f"gh a échoué : {r.stderr.strip()}")
    return json.loads(r.stdout)["data"]


def domaine_du_texte(texte):
    """Le domaine écrit par le formulaire (la ligne qui suit « ### Domaine »), ou None."""
    m = re.search(r"^###\s*Domaine\s*$\s*^(.+?)\s*$", texte or "", re.M)
    return m.group(1).strip() if m else None


def main():
    essai, apres, cartes = "--essai" in sys.argv, None, []
    while True:
        p = graphql(LIRE, orga=ORGA, numero=NUMERO, champ=CHAMP, apres=apres)["organization"]["projectV2"]
        cartes += p["items"]["nodes"]
        if not p["items"]["pageInfo"]["hasNextPage"]:
            break
        apres = p["items"]["pageInfo"]["endCursor"]
    if not p["field"]:
        sys.exit(f"Champ {CHAMP} introuvable sur le tableau {NUMERO}")
    options = {o["name"]: o["id"] for o in p["field"]["options"]}
    for carte in cartes:
        issue = carte["content"] or {}
        if "number" not in issue or carte["fieldValueByName"]:
            continue
        domaine = domaine_du_texte(issue["body"])
        if domaine not in options:  # pas de formulaire, « Je ne sais pas » ou « _No response_ »
            continue
        print(f"#{issue['number']} : Domaine {domaine}" + (" (essai, rien écrit)" if essai else ""))
        if not essai:
            graphql(ECRIRE, projet=p["id"], carte=carte["id"], champ=p["field"]["id"], option=options[domaine])


if __name__ == "__main__":
    main()
