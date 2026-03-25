# Notions C# importantes a connaitre pour ce projet

Ce guide liste les notions C# les plus importantes pour comprendre et faire evoluer ce Gomoku.

## 1) Programmation orientee objet (POO)

A maitriser:
- classes
- encapsulation (private/public)
- constructeurs
- heritage (BaseComponent)
- override

Pourquoi ici:
- Toute l'UI est basee sur des classes specialisees.
- Le moteur de jeu et les services sont des objets metier clairs.

## 2) Proprietes C#

A maitriser:
- auto-properties: public int X { get; set; }
- propriete en lecture seule: public int GridSize { get; }

Pourquoi ici:
- Les modeles, le moteur, et les DTO utilisent massivement les proprietes.

## 3) Generiques

A maitriser:
- List<T>
- Dictionary<TKey, TValue>
- HashSet<T>
- IReadOnlyList<T>

Pourquoi ici:
- Le moteur manipule les pierres/lignes avec ces structures.
- Le repository generic manipule n'importe quel modele.

## 4) LINQ

A maitriser:
- Where
- Select
- OrderBy / ThenBy
- FirstOrDefault
- ToList

Pourquoi ici:
- Utilise dans les services/repository pour trier et projeter des donnees.

## 5) Nullable reference types

A maitriser:
- string vs string?
- GameStone vs GameStone?
- patterns de verification null

Pourquoi ici:
- Le projet est en nullable enable, donc les contrats null/non-null sont importants.

## 6) Gestion des erreurs (exceptions)

A maitriser:
- try/catch
- fallback propre
- logs utiles

Pourquoi ici:
- Les services DB ne doivent pas casser le gameplay si la base est indisponible.

## 7) IDisposable et using

A maitriser:
- using var pour connexions SQL, commandes, readers

Pourquoi ici:
- Evite les fuites de ressources en data layer.

## 8) Evenements WinForms

A maitriser:
- abonnement: control.Event += Handler
- handlers souris/clavier/boutons
- cycle de vie Form (Shown, KeyDown, Click)

Pourquoi ici:
- Le coeur de l'interaction jeu (pose, bombe, undo) depend des evenements UI.

## 9) GDI+ (dessin) pour WinForms

A maitriser:
- OnPaint
- Graphics
- Pen / Brush
- conversion coordonnees grille -> pixels

Pourquoi ici:
- Le plateau, les points, les canons et les lignes sont dessines manuellement.

## 10) Architecture en couches

A maitriser:
- separation des responsabilites
- UI vs Core vs Service vs Data vs Model

Pourquoi ici:
- C'est la structure du projet actuel.
- C'est ce qui rend le code lisible et evolutif.

## 11) Etat et reconstruction deterministe

A maitriser:
- source de verite (historique des actions)
- replay des actions pour reconstruire le plateau

Pourquoi ici:
- Utilise pour:
  - chargement de partie
  - undo (Ctrl+Z)
  - coherence score/tour/lignes

## 12) Base de donnees PostgreSQL avec Npgsql

A maitriser:
- NpgsqlConnection
- NpgsqlCommand
- NpgsqlParameter
- mapping modele <-> table

Pourquoi ici:
- Persistance des parties et actions.

## 13) Tuples et structures compactes

A maitriser:
- tuples nommes: (int Dx, int Dy)

Pourquoi ici:
- Utilises pour les directions de scan dans le moteur.

## 14) Logs applicatifs

A maitriser:
- logs horodates
- logs d'action metier

Pourquoi ici:
- Le projet utilise TerminalLogger pour suivre clairement le flux de jeu.

## 15) Priorite d'apprentissage conseillee

1. Evenements WinForms + OnPaint
2. Collections + LINQ
3. Nullable + exceptions
4. Architecture couches (service/data/model/core/ui)
5. Npgsql et repository generic

Avec ces notions, tu peux comprendre et maintenir tout le projet sans blocage.

## 16) Syntaxes C# qui different de Java (detaillees)

Cette section repond a la question "quelles syntaxes C# dois-je absolument reconnaitre".

### 16.1 `sealed`

Syntaxe:
- `public sealed class GomokuEngine`

Signification:
- La classe ne peut pas etre heritee.

Equivalent Java:
- `final class GomokuEngine`

Pourquoi dans ce projet:
- Pour verrouiller certaines classes techniques et eviter des heritages non voulus.

### 16.2 `_` (underscore) en C#

Le symbole `_` peut avoir plusieurs usages:

1. Discard (valeur ignoree)
- Exemple: `out _`
- Equivalent Java: pas d'equivalent direct aussi propre.

2. Identifiant "normal" (si tu le nommes ainsi)
- Ton projet a `namespace _;` dans Program.cs.
- Consequence: eviter de sur-utiliser `_` comme variable locale, sinon confusion possible.

3. Pattern matching discard
- Exemple: `key switch { _ => null }`

### 16.3 `IReadOnlyList<T>`

Syntaxe:
- `public IReadOnlyList<GameStone> Stones => _stones;`

Signification:
- Le consommateur peut lire la liste, mais pas la modifier via cette reference.

Equivalent Java:
- Exposer `List<T>` non modifiable via `Collections.unmodifiableList(...)`.

Interet:
- Encapsulation plus sure des donnees internes.

### 16.4 Propriete expression-bodied (`=>`)

Syntaxe:
- `public bool IsInProgress => Status == EtatPartieStatus.EnCours;`

Equivalent Java:
- Methode courte `boolean isInProgress() { return ...; }`

Interet:
- Ecriture concise pour les acces simples.

### 16.5 Nullable reference types (`?`)

Syntaxe:
- `string?`, `GameStone?`, `IWin32Window?`

Signification:
- Le `?` indique explicitement qu'une reference peut etre `null`.

Equivalent Java:
- Java autorise null partout (sans annotation standard imposee par le langage).

Interet:
- Le compilateur aide a prevenir les NullReferenceException.

### 16.6 Operateurs `??` et `??=`

Syntaxe:
- `value ?? defaultValue`
- `value ??= newValue`

Equivalent Java:
- `value != null ? value : defaultValue`

Interet:
- Gestion concise des valeurs null.

### 16.7 `var` (inference de type locale)

Syntaxe:
- `var actions = _actionService.TryGetByPartieId(partieId);`

Equivalent Java:
- `var` existe aussi depuis Java 10, mais uniquement local.

Interet:
- Code plus lisible quand le type est evident.

### 16.8 `using var` (gestion de ressources)

Syntaxe:
- `using var conn = new NpgsqlConnection(...);`

Equivalent Java:
- `try-with-resources`

Interet:
- Dispose automatique sans bloc imbrique volumineux.

### 16.9 `out` parameters

Syntaxe:
- `TryPlaceStone(..., out GameStone? placedStone, out IReadOnlyList<WinningLine> lines)`

Equivalent Java:
- Pas natif; souvent objet resultat, tuple custom, ou wrapper mutable.

Interet:
- Retourne plusieurs valeurs sans classe supplementaire.

### 16.10 Tuples nommes

Syntaxe:
- `(int Dx, int Dy)`
- `Dictionary<(int Dx, int Dy), HashSet<Point>>`

Equivalent Java:
- En general, classe Pair/record custom.

Interet:
- Pratique pour des petits couples de donnees techniques.

### 16.11 `switch` expression

Syntaxe:
- `return key switch { Keys.NumPad1 => 1, _ => null };`

Equivalent Java:
- `switch` expression existe en Java moderne, mais syntaxe differente.

Interet:
- Plus compact, lisible et sans `break` repetitif.

### 16.12 Initializers d'objets

Syntaxe:
- `new ActionModel { PartieId = id, X = x, ... }`

Equivalent Java:
- Constructeur long, builder, ou setters successifs.

Interet:
- Ecriture tres concise des DTO/modeles.

### 16.13 Evenements (`+=`)

Syntaxe:
- `_undoButton.Click += UndoButton_Click;`

Equivalent Java:
- `addActionListener(...)`

Interet:
- Modele event-driven naturel en WinForms.

### 16.14 Methodes statiques utilitaires

Syntaxe:
- `public static class TurnActionAlert`

Equivalent Java:
- Classe utilitaire avec methode `static`.

Interet:
- Regrouper des helpers sans etat.

### 16.15 XML documentation comments

Syntaxe:
- `/// <summary> ... </summary>`

Equivalent Java:
- Javadoc `/** ... */`

Interet:
- Intellisense riche directement dans l'IDE.

## 17) Pieges frequents pour un dev Java

1. Melanger `_` discard et namespace `_` du projet.
2. Exposer `List<T>` au lieu de `IReadOnlyList<T>` quand on veut proteger l'etat interne.
3. Abuser de `var` quand le type n'est pas evident.
4. Oublier la logique nullable (`?`) et les checks associes.
5. Utiliser des classes utilitaires avec etat cache alors qu'elles sont pensees stateless.

## 18) Lecture guidee dans ce projet

Pour voir ces syntaxes en contexte:

1. `sealed`, tuples, `out`, `IReadOnlyList`: core/GomokuEngine.cs
2. `IReadOnlyList`, LINQ, object initializer: service/ActionService.cs
3. `using var`, reflection generique: data/GenericRepository.cs
4. `event +=`, nullable (`?`), `switch` expression: ui/Form1.cs
5. classes utilitaires statiques: ui/atoms/TurnActionAlert.cs
