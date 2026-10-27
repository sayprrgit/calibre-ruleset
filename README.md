# Calibre

Ruleset d'entraînement et d'analyse pour **osu!lazer**, orienté aim et tap. Calibre joue les beatmaps osu!standard
existantes, mesure finement les erreurs du joueur et, à terme, génère des exercices ciblés sur ses points faibles.

> Projet en cours. Ce README décrit l'état au **jalon 2** et sera réécrit au jalon 6.

## Feuille de route

| Jalon | Contenu | État |
|---|---|---|
| J1 | Ruleset minimal jouable : conversion, cercles, jugement, autoplay | ✅ |
| J2 | Sliders, spinners, score, HP, HUD | ✅ |
| J3 | Module Diagnostic : latence, stabilité par touche, précision d'aim par zone | ⏳ |
| J4 | Module Analyse : heatmap, timeline d'erreur, classement par pattern | ⏳ |
| J5 | Module Entraînement adaptatif : profil local et générateur déterministe | ⏳ |
| J6 | Finitions, captures, README final | ⏳ |

## Prérequis

- Windows 10/11
- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) (ou `winget install Microsoft.DotNet.SDK.10`)
- osu!lazer **2026.1005.0** ou plus récent (version d'API de ruleset `2026.818.0`)

## Compiler, tester, installer

```bash
dotnet build
dotnet test
```

`dotnet test` exécute la logique NUnit et les scènes visuelles en mode headless.

Pour parcourir les scènes visuelles, lancer `dotnet run --project osu.Game.Rulesets.Calibre.Tests`. Le navigateur
utilise un stockage dédié (`%APPDATA%\calibre-visual-tests`), séparé des données de lazer.

Pour jouer dans lazer :

```powershell
powershell -ExecutionPolicy Bypass -File scripts\install.ps1
```

Le script compile en Release et copie `osu.Game.Rulesets.Calibre.dll` dans **`%APPDATA%\osu\rulesets\`**.
Redémarrer ensuite lazer : Calibre apparaît dans la liste des modes de jeu.

## Choix techniques

- **Grandeurs d'osu!standard.** Fenêtres de jugement, rayon des cercles, préempt et cadrage 512x384 sont identiques :
  un UR mesuré dans Calibre est comparable à celui du jeu.
- **Zéro allocation en jeu.** Les drawables et les libellés de jugement sont poolés. Le jugement passe par des
  lambdas statiques avec état en paramètre, sans fermeture. Le suivi des sliders et la rotation des spinners,
  calculés à chaque frame, n'allouent rien non plus.
- **Quatre entrées distinctes.** K1, K2, M1 et M2 restent séparées, au lieu d'être fusionnées en « gauche / droite ».
  Elles sont enregistrées avec chaque frappe, pour mesurer la stabilité touche par touche.
- **Aucune dépendance au ruleset osu!.** Le convertisseur, les sliders et la politique de note lock sont réécrits
  d'après le source officiel (MIT), et tous les visuels sont dessinés en code.

### Sliders et spinners (J2)

- **Seuls les cercles et têtes de slider entrent dans l'UR.** Les ticks, répétitions, fins de slider et spinners
  n'ont pas de fenêtre de timing : ils sont jugés au suivi ou à la rotation, et ne faussent pas les statistiques de
  timing des jalons suivants.
- **Règles de lazer.** La zone de suivi passe à 2,4 rayons une fois acquise, la fin du slider est jugée sur le suivi
  36 ms avant la fin, et une fin manquée coûte de la précision sans casser le combo.
- **Spinners.** Le nombre de tours requis dépend de l'OD comme dans osu!. La rotation est comptée dans les deux sens
  et plafonnée à 477 tours/min. Le calcul est une classe pure, testée unitairement.
- **Lisibilité.** Les anneaux d'approche sont relayés au-dessus de tous les objets : un slider qui précède un cercle
  ne masque plus son anneau.

### Score, vie et HUD (J2)

- Le score et la vie s'appuient sur les processeurs de lazer. Le drain est calibré sur toute la beatmap, ticks
  compris, et l'écran de résultats ne liste que les jugements utilisés par Calibre.
- Le HUD de lazer fonctionne tel quel : score, précision, combo, jauge de vie et compteur des quatre touches.
- Calibre ajoute sous l'aire de jeu un panneau par touche : nombre de frappes réussies et écart moyen à l'instant
  parfait, mis à jour à chaque jugement.

## Limites connues

- Pas encore de *stacking* des objets superposés, ni de corps de slider qui se dessine progressivement.
- Le suivi d'un slider accepte toute touche maintenue, même enfoncée avant le slider.
- En replay, revenir en arrière pendant un spinner ne recalcule pas sa rotation.
- Ni bonus de spinner ni son de glissement sur les sliders.

## Contraintes découvertes

Ces contraintes ont été établies par analyse statique des binaires de lazer installés, avec
[REA](https://github.com/morluto/rea), puis vérifiées en chargeant la DLL contre ces mêmes binaires.

- **Nom de la DLL.** lazer ne charge que les fichiers `osu.Game.Rulesets.*.dll`. Sa version distribuée refuse aussi
  ceux dont le nom contient `plugin`, `loader`, `trainer`, `inject`, `mosu`… Cette vérification est absente du source
  public. C'est pourquoi le module d'entraînement s'appelle `Training` et non `Trainer`.
- **Replays non persistés.** lazer n'écrit de fichier `.osr` que pour les quatre rulesets officiels. Un replay Calibre
  vit en mémoire jusqu'à l'écran de résultats, et les analyses doivent s'exécuter à ce moment-là.
- **Version ciblée.** Le NuGet `ppy.osu.Game 2026.1026.0` suit une pré-release, et la dernière version stable de lazer
  (2026.1005.0) n'a pas de paquet NuGet. Toutes les références de la DLL ont été vérifiées comme présentes dans lazer
  2026.1005.0, et la routine de compatibilité de lazer passe.

## Licence et mentions

Calibre est distribué sous licence MIT (voir [LICENCE](LICENCE)). Une partie du code est adaptée
d'[osu!](https://github.com/ppy/osu) (© ppy Pty Ltd, licence MIT) ; les fichiers concernés l'indiquent en en-tête.

Calibre est un projet indépendant, **sans affiliation avec ppy Pty Ltd**. Il n'utilise ni la marque osu!, ni son logo,
ni les ressources graphiques d'`osu-resources`.
