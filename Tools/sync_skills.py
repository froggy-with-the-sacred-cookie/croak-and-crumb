"""Recopie les skills de .claude/skills/ (la source) vers .agents/skills/ (la copie lue par Codex).

Claude Code lit .claude/skills/, Codex lit .agents/skills/. On n'édite que la source, puis :
    python3 Tools/sync_skills.py           # recopie
    python3 Tools/sync_skills.py --verifier  # dit si la copie est en retard, sans rien écrire (code 1 si oui)
Une copie plutôt qu'un lien symbolique : les liens se cassent sous Windows.
"""
import filecmp, shutil, sys
from pathlib import Path

RACINE = Path(__file__).resolve().parent.parent
SOURCE = RACINE / ".claude" / "skills"
COPIE = RACINE / ".agents" / "skills"


def ecarts(a, b):
    """Chemins relatifs qui diffèrent entre deux dossiers (manquants, en trop ou modifiés)."""
    cmp = filecmp.dircmp(a, b)
    out = [f for f in cmp.left_only + cmp.right_only + cmp.diff_files + cmp.funny_files]
    for sous in cmp.common_dirs:
        out += [f"{sous}/{x}" for x in ecarts(a / sous, b / sous)]
    return out


if "--verifier" in sys.argv:
    reste = ecarts(SOURCE, COPIE) if COPIE.exists() else ["(copie absente)"]
    for r in reste:
        print("en retard :", r)
    sys.exit(1 if reste else 0)

if COPIE.exists():
    shutil.rmtree(COPIE)
shutil.copytree(SOURCE, COPIE)
print(f"{sum(1 for _ in COPIE.rglob('SKILL.md'))} skills recopiés vers {COPIE.relative_to(RACINE)}")
