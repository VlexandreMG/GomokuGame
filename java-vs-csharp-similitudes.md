# Java vs C# - Similitudes et differences utiles pour ce projet

Ce document compare Java et C# avec un angle pratique pour comprendre ce projet.

## 1) Vision globale

Java et C# sont deux langages:
- orientes objet
- types statiques
- avec GC (garbage collector)
- utilises pour des applications enterprise et desktop

Si tu viens de Java, tu vas reconnaitre tres vite la structure generale du code C#.

## 2) Similitudes directes

## Classes, objets, encapsulation

- Java: class, private/public, getters/setters
- C#: class, private/public, proprietes

Exemple conceptuel:
- Java: getX() / setX(...)
- C#: public int X { get; set; }

Dans ce projet:
- Les modeles et objets metier suivent exactement ce style (PartieModel, ActionModel, GameStone).

## Heritage et polymorphisme

- Java: extends, override
- C#: : BaseClass, override

Dans ce projet:
- BaseComponent definit un cycle de vie commun, puis GameBoard/GamePoint le specialisent.

## Interfaces et abstractions

- Java: interface
- C#: interface

Meme logique d'architecture en couches possible dans les deux langages (service/repository/model).

## Exceptions

- Java: try/catch/finally
- C#: try/catch/finally

Dans ce projet:
- Les services data gerent les erreurs de base de donnees avec try/catch.

## Collections

- Java: List<T>, Map<K,V>, Set<T>
- C#: List<T>, Dictionary<K,V>, HashSet<T>

Dans ce projet:
- Le moteur utilise Dictionary, List, HashSet pour gerer pierres/lignes/protections.

## 3) Differences importantes

## Propriete C# vs getter/setter Java

C# rend les DTO/modeles plus compacts avec les proprietes.

- Java:
  private int x;
  public int getX() { return x; }
- C#:
  public int X { get; set; }

## using var (dispose automatique)

C# a using var pour fermer proprement les ressources (connexions SQL, commandes, etc.).
C'est proche de try-with-resources en Java.

- Java: try (Connection c = ...) { ... }
- C#: using var conn = ...;

## Delegues et evenements (event-driven)

C# WinForms utilise beaucoup les evenements:
- button.Click += Handler;
- KeyDown, MouseClick, etc.

C'est proche des listeners Java Swing, mais la syntaxe C# est plus concise.

## Nullable reference types

C# moderne force une discipline de nullite (string vs string?).
C'est plus explicite que Java classique.

## LINQ

C# propose LINQ (OrderBy, Select, Where, FirstOrDefault...) directement sur les collections.
C'est proche des Streams Java.

## 4) Correspondances Java -> C# utiles dans ce projet

- Package Java -> Namespace C#
- POJO -> classe modele avec proprietes
- Service Java -> classe service C#
- Repository Java -> repository C# (GenericRepository)
- Listener Swing -> event handler WinForms
- try-with-resources -> using var

## 5) Architecture du projet (style Java-like)

Le projet est deja organise avec une logique tres familiere pour un dev Java:

- model/: classes de tables SQL
- data/: acces technique BD et repository
- service/: logique metier de persistance
- core/: regles du jeu
- ui/: presentation et interactions utilisateur

Donc si tu connais Spring/Swing/JPA mentalement, tu as deja les bons reflexes ici.

## 6) Ce qui peut surprendre un dev Java

- Les proprietes C# (get; set;) partout
- Les evenements WinForms avec +=
- Les tuples C# (ex: (int Dx, int Dy))
- Les collection initializers et syntaxe plus compacte

Mais le raisonnement metier reste le meme.

## 7) Resume rapide

- Similitude forte: OOP, couches, collections, exceptions
- Difference utile: syntaxe C# plus concise (proprietes, events, LINQ, using)
- Pour ce projet, ton experience Java est un gros avantage

## 8) Mini cheat sheet de syntaxes C# vs Java

1. `sealed class` (C#) = `final class` (Java)
2. `IReadOnlyList<T>` (C#) ~= `List<T>` non modifiable (Java)
3. `string?` (C# nullable explicite) vs nullite implicite en Java
4. `using var` (C#) = `try-with-resources` (Java)
5. `out` parameters (C#) = objet resultat / wrapper custom (Java)
6. `event += Handler` (C# WinForms) = `addActionListener` (Java Swing)
7. `switch` expression C# (`=>`) = switch expression Java moderne (syntaxe differente)
8. `new Obj { A = 1 }` (initializer C#) = constructeur/builder/setters (Java)
9. `_` peut etre discard en C#, mais attention: ton projet utilise aussi `namespace _;`
