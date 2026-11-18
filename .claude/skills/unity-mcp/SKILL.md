---
name: unity-mcp
description: Piloter et tester l'éditeur Unity de Froggy par une IA (MCP for Unity, execute_code, captures, tests, build) sans casser la partie d'un humain ni l'éditeur. À charger AVANT tout appel mcp__UnityMCP__* (refresh_unity, execute_code, run_tests, manage_camera, manage_build), avant d'écrire un script .cs pendant que l'éditeur est ouvert, avant une capture d'écran du jeu, un build en ligne de commande, ou quand Unity « plante », « ne répond plus », que les captures sortent vides, blanches ou cyan, ou que les outils du MCP ont disparu.
---

# Piloter Unity par une IA

Leçons de Lovecraft et Glouton (Unity 6, septembre-octobre 2026). Le skill global `unity-mcp-skill` donne les outils
du MCP ; celui-ci dit comment ne pas casser.

## 0. Brancher le MCP

- Le serveur MCP for Unity (CoplayDev) tourne en HTTP sur `http://127.0.0.1:8080/mcp`, lancé depuis Unity
  (*Window > MCP for Unity*). Le bouton « Configure » ne l'enregistre que pour un projet : pour un autre,
  `claude mcp add --transport http UnityMCP http://127.0.0.1:8080/mcp`, puis **redémarrer la session**.
- Codex : `[mcp_servers.UnityMCP]` avec `url = "http://127.0.0.1:8080/mcp"` dans `.codex/config.toml`.
- **Quand la session perd les outils** (après un redémarrage de Unity) alors que le serveur répond encore (HTTP 406 sur
  `/mcp`) : l'appeler en JSON-RPC (`initialize`, puis `tools/call` avec l'en-tête `Mcp-Session-Id` et
  `Accept: application/json, text/event-stream`). Lovecraft avait un petit client Python pour ça (`Outils/unity.py`).
- **Sans aucun MCP, la ligne de commande `unity` suffit** (Froggy, 28 octobre 2026 : rien sur le port 8080, le paquet
  `com.unity.pipeline` est dans le projet) : `~/.unity/bin/unity status`, `unity command eval --project-path <projet>
  '<C#>'` (C# récent, `return` donne le résultat), `unity recompile --project-path <projet>`. Après l'entrée en mode
  Play, la connexion tombe le temps du rechargement (« No Pipeline instance found ») : `unity status --until-ready`
  avant l'appel suivant. Les règles des sections 1 et 3 valent pareil (un appel pour vérifier, un pour agir).

## 1. Ne jamais recompiler pendant une partie

Unity recompile en jeu : la partie perd son état (erreurs en boucle) ou s'arrête net. Lovecraft l'a vécu trois fois.

- Vérifier `EditorApplication.isPlayingOrWillChangePlaymode` (ou la ressource `mcpforunity://editor/state`) **seul,
  dans un appel à part**, lire la réponse, **puis** agir. Jamais la vérification et l'écriture dans le même lot
  d'appels parallèles, ni dans la même commande Bash enchaînée par `;` (2026-10-05 et 2026-10-06 : la vérification
  disait « en jeu », l'écriture est passée quand même).
- Refaire la vérification **juste avant chaque** `refresh_unity`, `run_tests` ou écriture de `.cs`, pas une fois en
  début de tâche.
- Méthode qui marche : préparer les fichiers hors de `Assets/` (scratchpad), vérifier seul, copier d'un coup,
  revérifier seul, puis `refresh_unity` tout de suite.
- Quelqu'un dit « je teste », « j'ai lancé », ou envoie une capture du jeu : supposer qu'il joue et ne plus toucher aux
  scripts avant qu'il ait arrêté.
- « Script Changes While Playing » réglé après coup n'a **pas** empêché la recompilation en jeu (2026-10-06).
- Si une partie s'ouvre sans que personne l'ait lancée, le signaler tout de suite (2026-10-08 : lire le
  `ShortcutManager` par le MCP a coïncidé avec un lancement ; ne plus l'interroger).

## 2. execute_code et réglages

- `execute_code` compile en **C# 6** (CodeDom) : pas de déconstruction de tuple, écrire `UnityEngine.Object`.
- Juste après une recompilation, une référence directe à un type de l'éditeur échoue (« Could not resolve type ») :
  passer par la réflexion (`System.Type.GetType("<Ns>.<Type>, Assembly-CSharp-Editor")`).
- **Modifier un ScriptableObject** : par `SerializedObject`, puis `AssetDatabase.SaveAssetIfDirty`, et relire le
  `.asset` sur le disque. Une affectation directe suivie de `SaveAssets` peut rester en mémoire et se perdre.
- **Un menu qui ouvre une fenêtre** (`EditorUtility.DisplayDialog`) bloque Unity si on le lance par le MCP : appeler
  directement sa méthode publique.
- **Bâtir dans un appel, photographier dans le suivant** : un maillage créé puis rendu dans le même appel sort blanc.
- **Un appel qui échoue sans message, puis `no_unity_session`** : Unity est peut-être tombé. Ne JAMAIS relancer le même
  appel (2026-10-01 : deux plantages de suite). Lire la fin de `Logs/Editor.log` du projet et
  `~/Library/Logs/DiagnosticReports/Unity-*.ips`.
- **Domain reload désactivé en mode Play** (si le projet le fait) : tout cache statique se vide par
  `[RuntimeInitializeOnLoadMethod]`, et un champ d'état d'un MonoBehaviour prend `[System.NonSerialized]`.
- Un champ GameObject vide est un « faux null » dans l'éditeur : `x != null ? x : null` avant de le ranger dans un
  `object`.

## 3. Captures en jeu

Sans focus, Unity n'avance pas les images en mode Play.

1. Premier appel : `EditorApplication.isPlaying = true`.
2. Deuxième appel : `EditorApplication.isPaused = true`. **Jamais de `Step()` dans le même appel** : « PlayerLoop
   called recursively », rien n'avance, les captures sortent vides.
3. Appels suivants : `EditorApplication.Step()` (une vraie image chacun ; ~60 après une recompilation fraîche), puis la
   capture. Plus de ~200 `Step()` dans un appel dépasse le délai du MCP (réponse vide) mais va au bout : relire l'état
   dans un second appel.
4. Arrêt : `isPlaying = false`, puis attendre `isPlayingOrWillChangePlaymode == false` dans un appel suivant avant de
   remettre un drapeau de test.

Pièges :
- **Interface absente des captures** : rendre la caméra et le panneau UI Toolkit dans deux RenderTexture
  (`panelSettings.targetTexture`, `clearColor` transparent), puis superposer. Prendre le `panelSettings` de l'objet en
  scène, pas un `Resources.Load` (copie : la capture sort vide).
- `Screen.width/height` lus par `execute_code` sont ceux d'une autre fenêtre : fixer la vue Game par réflexion
  (`GameView.SetCustomResolution`) et rendre à la taille voulue.
- **Objets cyan** : compilation asynchrone des shaders ; `EditorSettings.asyncShaderCompilation = false` le temps des
  captures, puis le remettre.
- Les effets dessinés par `Graphics.DrawMesh` n'apparaissent pas si l'on rend la caméra hors d'une image : poser
  `cam.targetTexture`, un ou deux `Step()`, puis lire.
- `manage_camera` n'écrase pas un fichier existant (il ajoute `-1`) : toujours un nom neuf, sinon on regarde l'ancien.
- Une capture prise après l'arrêt du mode Play montre l'aperçu de l'éditeur : vérifier `isPlaying`.
- Les captures vont dans un dossier ignoré par git (`Captures/`) ; sortir en JPG, le disque se remplit vite.
- Comparer : composer les captures côte à côte et regarder ; un écart de pixels chiffré ne suffit pas.
- **Poser un clip humanoïde sans jouer** (Froggy, 18 novembre 2026, gestes de Froggy P) : en mode édition,
  `AnimationClip.SampleAnimation` et un `PlayableGraph` évalué laissent le modèle au repos (os et formes immobiles).
  Seul `AnimationMode.StartAnimationMode()` puis `BeginSampling` / `SampleAnimationClip` / `EndSampling` pose la pose,
  formes comprises ; `StopAnimationMode()` dans un `finally`. Et un `cam.Render()` dans le même appel montre encore
  l'ancien skinning : cuire la pose (`smr.BakeMesh(m, false)`, l'objet du FBX porte une échelle de 100) dans un
  maillage fixe et photographier celui-là. Une capture qui montre le repos ne prouve rien : mesurer d'abord un os.
- **Simuler des appuis par le MCP ne marche pas** (Froggy, 28 octobre 2026, MCP officiel de Unity, `Unity_RunCommand`) :
  `InputSystem.QueueStateEvent` sur un clavier ou une manette virtuelle, puis `Step()` ou `InputSystem.Update()`, ne
  change pas l'état de l'appareil (éditeur sans focus), même en passant `backgroundBehavior` à `IgnoreFocus`. Et ce
  MCP refuse `System.Reflection`. Pour mesurer un contrôleur sans humain : un test avec `InputTestFixture`, ou un
  point d'entrée de test public dans le contrôleur.

## 4. Tests

- `run_tests` en EditMode. Les suites qui lisent une scène l'ouvrent en `[OneTimeSetUp]` (le Test Runner part d'une
  scène vide).
- **`run_tests` enregistre la scène ouverte si elle est modifiée** : si une modification attend l'accord de l'équipe,
  ne pas lancer les tests avant, ou prévenir.
- **Sans MCP, pas de `unity test` sur un projet ouvert** : il lance un second éditeur (option `--timeout` : « Kill the
  Unity process »), comme `-batchmode`. Froggy, 28 octobre 2026 : on a exécuté les méthodes `[Test]` / `[TestCase]` par
  réflexion dans `unity command eval` (une `AssertionException` = un échec). Les tests dans `Assets/Froggy/Editor/`
  voient NUnit sans asmdef.

## 5. Build en ligne de commande

- **Jamais de `-batchmode` sur le projet pendant que l'éditeur l'a ouvert.** Glouton : `rsync` vers une copie
  (`.buildcopy/`, qui garde sa `Library`) et build là. Une autre session peut construire dans la même copie : pour un
  build de test avec des retouches de débogage, une copie privée dans le scratchpad.
- Le binaire peut prendre des options de test (Glouton : `-autoplay -shots <dossier>`, `-sim N`, `-players n`) : l'IA
  joue à la place d'un humain et laisse captures et journal. Utile pour vérifier sans souris (skill
  `multijoueur-equilibrage`).
- Premier lancement d'un nouveau build sur macOS : la fenêtre du pare-feu peut bloquer les tests en réseau local.
  Relancer avant d'accuser le code.

## 6. Pièges Unity 6 rencontrés

- `GetInstanceID` est obsolète en erreur depuis Unity 6.6.
- USS : `-unity-slice-scale` exige une unité (`0.5px`).
- La profondeur de champ d'URP est fausse en caméra orthographique.
- Un build peut révéler qu'URP retire le post-traitement (`m_StripUnusedPostProcessingVariants`) : contrôler un build,
  pas seulement l'éditeur.
- Les modèles de kits sans couleurs de sommets peuvent faire perdre les leurs aux maillages générés dessinés après (le
  shader lit `COLOR`) : un postprocesseur d'import qui leur donne du blanc l'a réglé.
- **Dans l'éditeur, `GetComponent<T>()` rend un « faux null »** quand le composant manque : `x.GetComponent<T>() ??
  x.AddComponent<T>()` n'ajoute rien. Tester avec `!= null` (`VitrineJardin.Composant<T>`, 28 octobre 2026).
- **Travailler dans un éditeur qu'un autre utilise** (Froggy, 28 octobre 2026 : la session des personnages avait
  `Test_Personnages` ouverte) : un outil qui ouvre sa scène retient la scène d'avant, refuse si elle a des modifications
  non enregistrées, et la rouvre à la fin (`VitrineJardin.OuvrirVitrine` / `Revenir`). Une sonde instancie dans une scène
  d'aperçu (`EditorSceneManager.NewPreviewScene`), pas dans la scène ouverte.
- **`unity recompile` sans le focus peut durer plus de 6 minutes** (macOS ralentit l'éditeur en arrière-plan) : le lancer
  en arrière-plan avec `--timeout 600`, puis vérifier les shaders et les types par `unity command eval`
  (`ShaderUtil.ShaderHasError`, `Type.GetType`). La sortie de `recompile` peut être vide même quand tout va bien.
- **Une touche se nomme par sa place sur un clavier QWERTY américain, et un Mac ISO la déplace** (Froggy, 29 octobre
  2026) : sur le Mac AZERTY de Florent, `Key.Backquote` est la touche « < » à côté de Maj, pas « @ » au-dessus de Tab,
  et le menu de debug ne s'ouvrait pas. Avant de choisir une touche, lire `Keyboard.current[k].displayName` dans
  Unity ; pour une touche « au-dessus de Tab », écouter aussi le caractère (`onTextInput` : « ² », « @ »).
- **Particules** : `Particle.velocity` ignore les modules de vitesse et de bruit ; une sonde lit `totalVelocity`.
- **Traînée des particules** (`limitVelocityOverLifetime.drag`, Froggy, 29 octobre 2026, voltige de sakura) : par
  défaut Unity la multiplie par la taille de la particule (`multiplyDragByParticleSize`) ; à 0,1 m elle ne freinait
  presque rien (2,3 m/s de dérive au vent). Et elle freine aussi le module de vitesse : une chute réglée là tombait à
  −0,02 m/s. Pour une vitesse limite, une force (gravité faible, vent) divisée par la traînée, mesurée par une sonde.
- **Le Mono de Unity calcule les `float` en double**, puis les arrondit en les rangeant (Froggy, 29 octobre 2026,
  analyse des trajets du Bâtisseur) : un plus court chemin en `float` voyait des cases « améliorées » sans l'être et
  les remettait dans la file ; plus de 15 minutes sur 6 000 cases, 2 ms en .NET. Un algorithme qui compare des sommes
  (Dijkstra, accumulations) travaille en `double` et exige un écart minimal pour améliorer.
- **`unity command eval` coupe la réponse à 30 s, mais pas le calcul** : l'éditeur reste occupé jusqu'au bout, sans
  moyen de l'arrêter, et ne répond plus. Chronométrer d'abord sur une petite entrée, un appel par étape ; pour déboguer
  un calcul pur, le recopier dans un petit projet `dotnet` du scratchpad (hors de Unity), et `sample <pid>` montre si
  l'éditeur tourne dans du code géré.
- **Photographier une fenêtre de l'éditeur** (Froggy, 29 octobre 2026, Bâtisseur d'arène) : `w.ShowTab()` (rangée en
  onglet, c'est sinon l'onglet voisin qu'on photographie), `RepaintImmediately` deux fois, puis la méthode interne
  `GrabPixels(RenderTexture, Rect)` du `HostView` (champ `m_Parent` de l'`EditorWindow`, par réflexion), le rectangle en
  **pixels** (taille × `EditorGUIUtility.pixelsPerPoint`). L'image sort **à l'envers** : retourner les lignes avant
  d'enregistrer. Marche aussi pour la vue Scène, avec les poignées et les panneaux dessinés par les outils.
