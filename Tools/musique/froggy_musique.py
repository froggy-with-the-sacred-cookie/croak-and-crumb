"""Musique de Froggy (P6-08, #69), tirée du thème A « Jardin malicieux » choisi le 19/11/2026.

Rythme signature : appel (deux croches), envol (noire qui saute vers l'aigu), réception (retombée).
Ré pentatonique (ré mi fa# la si) pour la mélodie : aucune note ne frotte avec les accords du morceau.

Sorties (partitions JSON pour render_music de Sound Forge) :
  mus_jardin_v01   boucle de partie, 32 mesures à 112 BPM, A A' B A'' ; stems ambience, melody, rythme, tension
  mus_menu_v01     thème du menu, 24 mesures à 100 BPM, A A' B ; stems ambience, melody
  mus_victoire_v01 jingle (le saut réussi), non bouclé
  mus_defaite_v01  jingle (le saut raté), non bouclé

Usage : python3 froggy_musique.py <dossier de sortie>   (écrit <nom>.partition.json et vérifie l'harmonie)
"""
import json
import sys

PC = {'C': 0, 'D': 2, 'E': 4, 'F': 5, 'G': 7, 'A': 9, 'B': 11}


def midi(nom):
    lettre, reste = nom[0], nom[1:]
    alt = 0
    while reste and reste[0] in '#b':
        alt += 1 if reste[0] == '#' else -1
        reste = reste[1:]
    return 12 * (int(reste) + 1) + PC[lettre] + alt


def pc(nom):
    return midi(nom if nom[-1].isdigit() else nom + '4') % 12


def mesure(n):
    return 4 * (n - 1)


def suite(seq, debut, vel=0.7):
    notes, t = [], debut
    for n, d in seq:
        if n is not None:
            notes.append({'at': round(t, 3), 'len': d, 'note': n, 'vel': vel})
        t += d
    return notes, t


ACCORDS = {'D': ['D', 'F#', 'A'], 'Bm': ['B', 'D', 'F#'], 'G': ['G', 'B', 'D'], 'Asus4': ['A', 'D', 'E'],
           'Em7': ['E', 'G', 'B', 'D']}
PERMISES = {'D': ['E', 'B'], 'Bm': ['A', 'E'], 'G': ['A', 'E', 'F#'], 'Asus4': ['B', 'F#'], 'Em7': ['F#', 'A']}


def verifier(nom, notes, accords):
    """Chaque note d'au moins un temps : son de l'accord ou tension permise. Et aucune note hors de la pentatonique
    dans les voix mélodiques."""
    alertes = []
    for note in notes:
        accord = None
        for (t, a) in accords:
            if t <= note['at'] + 1e-6:
                accord = a
        if accord is None:
            continue
        if note['len'] >= 1:
            ok = {pc(x) for x in ACCORDS[accord] + PERMISES[accord]}
            if pc(note['note']) not in ok:
                alertes.append(f"{nom}: {note['note']} à {note['at']} (len {note['len']}) sur {accord}")
    return alertes


def plan_vers_accords(plan):
    return [(mesure(m) + t, a) for (m, t, a) in plan]


# ------------------------------------------------------------------ le thème (8 mesures)
PHRASE = [
    [('A4', .5), ('D5', .5), ('A5', 1), ('F#5', .5), ('E5', .5), ('D5', 1)],
    [('B4', 1.5), ('A4', .5), (None, 2)],
    [('B4', .5), ('D5', .5), ('B5', 1), ('A5', .5), ('F#5', .5), ('E5', 1)],
    [('F#5', 1.5), ('D5', .5), (None, 2)],
    [('D5', .5), ('E5', .5), ('B5', .5), ('A5', .5), ('D5', .5), ('E5', .5), ('B5', .5), ('A5', .5)],
    [('D5', .5), ('E5', .5), ('D6', 1), ('B5', .5), ('A5', .5), ('F#5', .5), ('E5', .5)],
    [('D5', 1), ('B4', .5), ('A4', .5), ('B4', 1), ('D5', 1)],
]
FIN_OUVERTE = [('B4', .5), ('D5', .5), ('E5', 3)]
FIN_FERMEE = [('A4', .5), ('B4', .5), ('E5', 1), ('D5', 2)]
PLAN_A = [(1, 0, 'D'), (2, 0, 'D'), (3, 0, 'Bm'), (4, 0, 'G'), (5, 0, 'G'), (6, 0, 'Asus4'),
          (7, 0, 'Bm'), (7, 2, 'G'), (8, 0, 'Asus4')]
PLAN_A_FERME = PLAN_A[:-1] + [(8, 0, 'Asus4'), (8, 2, 'D')]
PLAN_B = [(1, 0, 'Bm'), (2, 0, 'G'), (3, 0, 'D'), (4, 0, 'Asus4'), (5, 0, 'Bm'), (6, 0, 'G'), (7, 0, 'Em7'),
          (8, 0, 'Asus4')]

# B : la marimba appelle (le saut), la flûte répond en notes longues ; des silences pour laisser passer le jeu
APPELS_B = {1: [('B4', .5), ('D5', .5), ('F#5', 1)], 3: [('A4', .5), ('D5', .5), ('A5', 1)],
            5: [('D5', .5), ('F#5', .5), ('B5', 1)], 7: [('E5', .5), ('F#5', .5), ('B5', 1)]}
REPONSES_B = [
    [(None, 2), ('B5', 2)],
    [('A5', 1.5), ('F#5', .5), ('D5', 2)],
    [(None, 2), ('F#5', 2)],
    [('E5', 1.5), ('D5', .5), ('E5', 2)],
    [(None, 2), ('D6', 2)],
    [('B5', 1), ('A5', 1), ('B5', .5), ('A5', .5), ('F#5', 1)],
    [(None, 2), ('D6', 1), ('B5', 1)],
    [('A5', 1.5), ('B5', .5), ('E5', 2)],
]

HARPE = {
    'D': ['D3', 'A3', 'D4', 'E4', 'F#4', 'E4', 'D4', 'A3'],
    'Bm': ['B2', 'F#3', 'B3', 'D4', 'F#4', 'D4', 'B3', 'F#3'],
    'G': ['G2', 'D3', 'G3', 'A3', 'B3', 'A3', 'G3', 'D3'],
    'Asus4': ['A2', 'E3', 'A3', 'B3', 'D4', 'B3', 'A3', 'E3'],
    'Em7': ['E2', 'B2', 'E3', 'G3', 'B3', 'G3', 'E3', 'B2'],
}
BASSE = {'D': ('D2', 'A2'), 'Bm': ('B1', 'F#2'), 'G': ('G1', 'D2'), 'Asus4': ('A1', 'E2'), 'Em7': ('E2', 'B1')}
NAPPE = {'D': ['F#3', 'A3', 'D4'], 'Bm': ['F#3', 'B3', 'D4'], 'G': ['G3', 'B3', 'D4'], 'Asus4': ['A3', 'D4', 'E4'],
         'Em7': ['G3', 'B3', 'D4']}
OSTINATO = {'D': ['D4', 'A4', 'F#4', 'A4'], 'Bm': ['D4', 'B4', 'F#4', 'B4'], 'G': ['D4', 'B4', 'G4', 'B4'],
            'Asus4': ['E4', 'A4', 'D4', 'A4'], 'Em7': ['E4', 'B4', 'G4', 'B4']}


def octave_plus(n, k=1):
    i = len(n) - 1
    return n[:i] + str(int(n[i]) + k)


def motifs_accompagnement(vel_harpe=.38):
    p = {}
    for a, ns in HARPE.items():
        p['harpe_' + a] = [{'at': i * .5, 'len': .5, 'note': n, 'vel': vel_harpe} for i, n in enumerate(ns)]
        p['harpe_' + a + '_d'] = p['harpe_' + a][:4]
        # B : plus calme, en noires
        p['harpe_' + a + '_q'] = [{'at': i, 'len': 1, 'note': n, 'vel': vel_harpe} for i, n in enumerate(ns[:4])]
        r, q = BASSE[a]
        p['basse_' + a] = [{'at': 0, 'len': 1, 'note': r, 'vel': .7}, {'at': 1.5, 'len': .5, 'note': q, 'vel': .5},
                           {'at': 2, 'len': 1, 'note': octave_plus(r), 'vel': .6}, {'at': 3, 'len': .5, 'note': q, 'vel': .45}]
        p['basse_' + a + '_d'] = p['basse_' + a][:2]
        p['basse_' + a + '_q'] = [{'at': 0, 'len': 1, 'note': r, 'vel': .6}, {'at': 2, 'len': 1, 'note': q, 'vel': .45}]
        o = OSTINATO[a]
        p['osti_' + a] = [{'at': i * .5, 'len': .25, 'note': o[i % 4], 'vel': .5 if i % 2 == 0 else .38} for i in range(8)]
        p['osti_' + a + '_d'] = p['osti_' + a][:4]
    p['bois'] = [{'at': 0, 'len': .5, 'note': 'E2', 'vel': .5}, {'at': 1.5, 'len': .5, 'note': 'F2', 'vel': .32},
                 {'at': 2, 'len': .5, 'note': 'F2', 'vel': .4}, {'at': 3, 'len': .5, 'note': 'E2', 'vel': .35},
                 {'at': 3.5, 'len': .5, 'note': 'F2', 'vel': .28}]
    p['bois_calme'] = [{'at': 0, 'len': .5, 'note': 'E2', 'vel': .42}, {'at': 3, 'len': .5, 'note': 'F2', 'vel': .26},
                       {'at': 3.5, 'len': .5, 'note': 'F2', 'vel': .3}]
    p['taiko'] = [{'at': 0, 'len': 1, 'note': 'C2', 'vel': .38}, {'at': 2.5, 'len': 1, 'note': 'C2', 'vel': .25}]
    # tension : pulsation de taiko à la croche, timbale sur le temps fort, roulement de bois toutes les 2 mesures
    p['tension_pulse'] = ([{'at': i * .5, 'len': .5, 'note': 'C2', 'vel': .42 if i % 2 == 0 else .26} for i in range(8)]
                          + [{'at': 0, 'len': 1, 'note': 'D2', 'vel': .4}])
    p['tension_roul'] = [{'at': 4 + 2 + i * .25, 'len': .25, 'note': 'F2', 'vel': .25 + .05 * i} for i in range(8)]
    return p


def jouer_plan(accords, fin_t, suffixe_par_mesure):
    """Pour chaque accord : motifs harpe/basse/ostinato (complet, demi-mesure, ou « _q » en section calme)."""
    harpe, basse, osti = [], [], []
    for (t0, a), suivant in zip(accords, accords[1:] + [(fin_t, None)]):
        d = suivant[0] - t0
        suf = '_d' if d <= 2 else suffixe_par_mesure(t0)
        harpe.append({'pattern': 'harpe_' + a + suf, 'at': t0})
        basse.append({'pattern': 'basse_' + a + suf, 'at': t0})
        osti.append({'pattern': 'osti_' + a + ('_d' if d <= 2 else ''), 'at': t0})
        if d > 4:  # un accord tenu plus d'une mesure
            for k in range(1, int(d // 4)):
                harpe.append({'pattern': 'harpe_' + a + suf, 'at': t0 + 4 * k})
                basse.append({'pattern': 'basse_' + a + suf, 'at': t0 + 4 * k})
                osti.append({'pattern': 'osti_' + a, 'at': t0 + 4 * k})
    return harpe, basse, osti


def decaler(plan, n):
    return [(m + n, t, a) for (m, t, a) in plan]


# ================================================================== JARDIN (boucle de partie)
def jardin():
    bars, bpm = 32, 112
    # A (1-8, ouverte) · A' (9-16, fermée) · B (17-24) · A'' (25-32, ouverte -> revient au début)
    plan = decaler(PLAN_A, 0) + decaler(PLAN_A_FERME, 8) + decaler(PLAN_B, 16) + decaler(PLAN_A, 24)
    accords = plan_vers_accords(plan)

    flute = []
    for debut, fin, vel in ((1, FIN_OUVERTE, .72), (9, FIN_FERMEE, .78), (25, FIN_OUVERTE, .82)):
        t = mesure(debut)
        for b in PHRASE + [fin]:
            ns, t = suite(b, t, vel)
            flute += ns
    t = mesure(17)
    for b in REPONSES_B:
        ns, t = suite(b, t, .66)
        flute += ns

    marimba = []
    for base in (0, 8, 24):
        marimba += suite([('F#4', .5), ('A4', .5), ('D5', 1)], mesure(base + 2) + 2, .55)[0]
        marimba += suite([('D4', .5), ('G4', .5), ('B4', 1)], mesure(base + 4) + 2, .55)[0]
    for base in (8, 24):  # le sommet doublé
        marimba += suite([('D5', .5), ('E5', .5), ('D6', 1)], mesure(base + 6), .5)[0]
    for m, cellule in APPELS_B.items():
        marimba += suite(cellule, mesure(16 + m), .6)[0]

    p = motifs_accompagnement()
    calme = lambda t0: '_q' if mesure(17) <= t0 < mesure(25) else ''
    harpe, basse, osti = jouer_plan(accords, mesure(bars + 1), calme)
    bois = ([{'pattern': 'bois', 'at': 0, 'times': 16, 'every': 4}]
            + [{'pattern': 'bois_calme', 'at': mesure(17), 'times': 8, 'every': 4}]
            + [{'pattern': 'bois', 'at': mesure(25), 'times': 8, 'every': 4}])
    taiko = [{'pattern': 'taiko', 'at': mesure(9), 'times': 8, 'every': 4},
             {'pattern': 'taiko', 'at': mesure(25), 'times': 8, 'every': 4}]
    tension = [{'pattern': 'tension_pulse', 'at': 0, 'times': bars, 'every': 4},
               {'pattern': 'tension_roul', 'at': 0, 'times': bars // 2, 'every': 8}]

    music = {
        'name': 'Froggy — jardin japonais (partie)', 'bpm': bpm, 'bars': bars, 'looping': True, 'peak_db': -1,
        'reverb': {'room': .55, 'damping': .45, 'mix': .22}, 'patterns': p,
        'tracks': [
            {'name': 'flute', 'instrument': 'flute', 'stem': 'melody', 'notes': flute, 'reverb': .3, 'humanize': .2},
            {'name': 'marimba', 'instrument': 'marimba', 'stem': 'melody', 'notes': marimba, 'reverb': .25, 'pan': .25},
            {'name': 'harpe', 'instrument': 'harpe', 'stem': 'ambience', 'play': harpe, 'reverb': .35, 'pan': -.2,
             'humanize': .25, 'volume_db': -9},
            {'name': 'pizz', 'instrument': 'violoncelles_pizz', 'stem': 'ambience', 'play': basse, 'reverb': .15,
             'volume_db': -5},
            {'name': 'bois', 'instrument': 'percussions', 'stem': 'rythme', 'play': bois + taiko, 'reverb': .15,
             'humanize': .2, 'volume_db': 9},
            {'name': 'cordes_tension', 'instrument': 'violons_court', 'stem': 'tension', 'play': osti, 'reverb': .2,
             'pan': .15, 'volume_db': -7, 'humanize': .15},
            {'name': 'taiko_tension', 'instrument': 'percussions', 'stem': 'tension', 'play': tension, 'reverb': .15,
             'volume_db': 7, 'humanize': .15},
        ],
    }
    alertes = verifier('jardin flûte', flute, accords) + verifier('jardin marimba', marimba, accords)
    return 'mus_jardin_v01', music, alertes


# ================================================================== MENU (thème principal)
def menu():
    bars, bpm = 24, 100
    plan = decaler(PLAN_A, 0) + decaler(PLAN_A_FERME, 8) + decaler(PLAN_B, 16)
    accords = plan_vers_accords(plan)

    flute = []
    for debut, fin, vel in ((1, FIN_OUVERTE, .66), (9, FIN_FERMEE, .72)):
        t = mesure(debut)
        for b in PHRASE + [fin]:
            ns, t = suite(b, t, vel)
            flute += ns
    t = mesure(17)
    for b in REPONSES_B:
        ns, t = suite(b, t, .6)
        flute += ns
    # glockenspiel : les appels de B à l'octave, et le sommet du 2e passage
    glock = []
    for m, cellule in APPELS_B.items():
        glock += suite([(octave_plus(n) if n else None, d) for n, d in cellule], mesure(16 + m), .4)[0]
    glock += suite([('D6', 1)], mesure(14) + 1, .4)[0]

    p = motifs_accompagnement(vel_harpe=.34)
    # le menu respire : harpe en noires partout sauf au 2e passage
    calme = lambda t0: '' if mesure(9) <= t0 < mesure(17) else '_q'
    harpe, basse, _ = jouer_plan(accords, mesure(bars + 1), calme)
    nappe = []
    for (t0, a), suivant in zip(accords, accords[1:] + [(mesure(bars + 1), None)]):
        for n in NAPPE[a]:
            nappe.append({'at': t0, 'len': suivant[0] - t0, 'note': n, 'vel': .3})

    music = {
        'name': 'Froggy — thème du menu', 'bpm': bpm, 'bars': bars, 'looping': True, 'peak_db': -3,
        'reverb': {'room': .65, 'damping': .5, 'mix': .26}, 'patterns': p,
        'tracks': [
            {'name': 'flute', 'instrument': 'flute', 'stem': 'melody', 'notes': flute, 'reverb': .35, 'humanize': .2},
            {'name': 'glockenspiel', 'instrument': 'glockenspiel', 'stem': 'melody', 'notes': glock, 'reverb': .4,
             'pan': .3, 'volume_db': -4},
            {'name': 'harpe', 'instrument': 'harpe', 'stem': 'ambience', 'play': harpe, 'reverb': .4, 'pan': -.2,
             'humanize': .25, 'volume_db': -7},
            {'name': 'pizz', 'instrument': 'violoncelles_pizz', 'stem': 'ambience', 'play': basse, 'reverb': .2,
             'volume_db': -7},
            {'name': 'nappe', 'instrument': 'violons', 'stem': 'ambience', 'notes': nappe, 'reverb': .45,
             'volume_db': -12},
        ],
    }
    alertes = verifier('menu flûte', flute, accords) + verifier('menu glock', glock, accords)
    return 'mus_menu_v01', music, alertes


# ================================================================== JINGLES
def victoire():
    """Le saut réussi : deux bonds qui montent, puis l'accord de ré qui résout."""
    melodie = suite([('A4', .5), ('D5', .5), ('A5', 1), ('D5', .5), ('F#5', .5), ('D6', 1)], 0, .85)[0]
    melodie += [{'at': 4, 'len': 3, 'note': 'D6', 'vel': .9}, {'at': 4, 'len': 3, 'note': 'A5', 'vel': .75}]
    marimba = suite([('A4', .5), ('D5', .5), ('A5', 1), ('D5', .5), ('F#5', .5), ('D6', 1)], 0, .6)[0]
    glock = [{'at': 4, 'len': 2, 'note': 'D6', 'vel': .6}, {'at': 4.25, 'len': 2, 'note': 'F#6', 'vel': .5},
             {'at': 4.5, 'len': 2, 'note': 'A6', 'vel': .5}, {'at': 4.75, 'len': 2, 'note': 'D7', 'vel': .55}]
    harpe = [{'at': 2 + i * .125, 'len': 2, 'note': n, 'vel': .45}
             for i, n in enumerate(['D3', 'F#3', 'A3', 'D4', 'E4', 'F#4', 'A4', 'B4', 'D5', 'E5', 'F#5', 'A5', 'D6', 'E6', 'F#6', 'A6'])]
    harpe += [{'at': 4 + i * .06, 'len': 4, 'note': n, 'vel': .55} for i, n in enumerate(['D2', 'A2', 'D3', 'F#3', 'A3', 'D4'])]
    pizz = [{'at': 0, 'len': 1, 'note': 'D2', 'vel': .7}, {'at': 2, 'len': 1, 'note': 'A2', 'vel': .7},
            {'at': 4, 'len': 1, 'note': 'D2', 'vel': .9}]
    perc = [{'at': 0, 'len': .5, 'note': 'F2', 'vel': .5}, {'at': .5, 'len': .5, 'note': 'F2', 'vel': .5},
            {'at': 1, 'len': 1, 'note': 'E2', 'vel': .6}, {'at': 2, 'len': .5, 'note': 'F2', 'vel': .55},
            {'at': 2.5, 'len': .5, 'note': 'F2', 'vel': .55}, {'at': 3, 'len': 1, 'note': 'E2', 'vel': .65},
            {'at': 4, 'len': 2, 'note': 'C2', 'vel': .7}, {'at': 4, 'len': 2, 'note': 'D2', 'vel': .6},
            {'at': 4, 'len': 4, 'note': 'A2', 'vel': .45}]
    music = {
        'name': 'Froggy — victoire', 'bpm': 112, 'bars': 2, 'looping': False, 'peak_db': -1,
        'reverb': {'room': .55, 'damping': .45, 'mix': .22},
        'tracks': [
            {'name': 'flute', 'instrument': 'flute', 'stem': 'melody', 'notes': melodie, 'reverb': .3, 'humanize': .1},
            {'name': 'marimba', 'instrument': 'marimba', 'stem': 'melody', 'notes': marimba, 'reverb': .25, 'pan': .25},
            {'name': 'glock', 'instrument': 'glockenspiel', 'stem': 'melody', 'notes': glock, 'reverb': .4, 'pan': .3,
             'volume_db': -3},
            {'name': 'harpe', 'instrument': 'harpe', 'stem': 'ambience', 'notes': harpe, 'reverb': .4, 'pan': -.2,
             'volume_db': -6, 'humanize': .05},
            {'name': 'pizz', 'instrument': 'violoncelles_pizz', 'stem': 'ambience', 'notes': pizz, 'reverb': .2,
             'volume_db': -3},
            {'name': 'perc', 'instrument': 'percussions', 'stem': 'rythme', 'notes': perc, 'reverb': .2, 'volume_db': 7,
             'humanize': .05},
        ],
    }
    return 'mus_victoire_v01', music, []


def defaite():
    """Le saut raté : l'appel, puis un bond qui retombe au lieu de monter, et un si mineur doux (pas une punition)."""
    flute = suite([('D5', .5), ('E5', .5), ('A4', 1.5), (None, .5), ('B4', .5), ('A4', .5), ('F#4', 2)], 0, .7)[0]
    marimba = suite([(None, 2), ('D4', .5), ('E4', .5), ('A3', 1)], 0, .5)[0]
    pizz = [{'at': 4, 'len': 1, 'note': 'B2', 'vel': .6}, {'at': 5, 'len': 1, 'note': 'A2', 'vel': .55},
            {'at': 6, 'len': 1, 'note': 'F#2', 'vel': .55}, {'at': 8, 'len': 1, 'note': 'B1', 'vel': .7}]
    harpe = [{'at': 8 + i * .1, 'len': 4, 'note': n, 'vel': .45} for i, n in enumerate(['B2', 'F#3', 'B3', 'D4', 'E4', 'F#4'])]
    perc = [{'at': 0, 'len': .5, 'note': 'F2', 'vel': .45}, {'at': .5, 'len': .5, 'note': 'F2', 'vel': .45},
            {'at': 1.5, 'len': 1, 'note': 'E2', 'vel': .5}, {'at': 8, 'len': 1, 'note': 'E2', 'vel': .55}]
    music = {
        'name': 'Froggy — défaite', 'bpm': 100, 'bars': 3, 'looping': False, 'peak_db': -3,
        'reverb': {'room': .6, 'damping': .5, 'mix': .25},
        'tracks': [
            {'name': 'flute', 'instrument': 'flute', 'stem': 'melody', 'notes': flute, 'reverb': .35, 'humanize': .1},
            {'name': 'marimba', 'instrument': 'marimba', 'stem': 'melody', 'notes': marimba, 'reverb': .3, 'pan': .25},
            {'name': 'harpe', 'instrument': 'harpe', 'stem': 'ambience', 'notes': harpe, 'reverb': .4, 'pan': -.2,
             'volume_db': -4},
            {'name': 'pizz', 'instrument': 'violoncelles_pizz', 'stem': 'ambience', 'notes': pizz, 'reverb': .2,
             'volume_db': -2},
            {'name': 'perc', 'instrument': 'percussions', 'stem': 'rythme', 'notes': perc, 'reverb': .2, 'volume_db': 6},
        ],
    }
    accords = [(0, 'D'), (4, 'Bm')]
    return 'mus_defaite_v01', music, verifier('défaite flûte', flute, accords)


if __name__ == '__main__':
    sortie = sys.argv[1] if len(sys.argv) > 1 else '.'
    toutes = []
    for f in (jardin, menu, victoire, defaite):
        nom, music, alertes = f()
        toutes += alertes
        with open(f'{sortie}/{nom}.partition.json', 'w') as fh:
            json.dump(music, fh, ensure_ascii=False, separators=(',', ':'))
        notes = sum(len(t.get('notes', [])) for t in music['tracks'])
        print(f"{nom}: {music['bars']} mesures à {music['bpm']} BPM = {music['bars'] * music.get('beats_per_bar', 4) * 60 / music['bpm']:.2f} s, "
              f"{len(music['tracks'])} pistes, stems {sorted({t['stem'] for t in music['tracks']})}, {notes} notes écrites")
    print('\n'.join(toutes) if toutes else 'Harmonie : aucune note longue hors accord.')
