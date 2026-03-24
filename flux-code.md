# Flux global du projet GomokuGame

Ce document explique le parcours complet de l'application, depuis le menu de configuration jusqu'a la fin de partie.

## 1) Point d'entree de l'application

- Fichier: Program.cs
- Methode cle: Main()

Flux:
1. L'application WinForms est initialisee.
2. La fenetre principale Form1 est ouverte.

## 2) Initialisation de la fenetre principale

- Fichier: ui/Form1.cs
- Methodes cles: constructeur Form1(), InitializeLifecycle(), Initialize()

Flux:
1. Les composants UI sont crees (plateau, panneau bas, boutons Terminer + Retour).
2. Les handlers d'evenements sont relies (clic plateau, clavier, boutons).
3. Les services data sont prepares:
   - PartieService
   - ActionService
4. La fenetre attend son affichage pour lancer le menu de depart.

## 3) Ouverture du menu de depart

- Fichier: ui/atoms/GameSetupMenu.cs
- Methode cle: TryGetConfiguration(...)

Deux chemins possibles:
1. Nouvelle partie:
   - Saisie nom joueur 1
   - Saisie nom joueur 2
   - Choix taille de grille
2. Charger partie:
   - Ouverture de la liste des parties en base
   - Selection d'une partie existante

Le resultat est renvoye a Form1 sous forme de GameSetupResult.

## 4) Demarrage d'une nouvelle partie

- Fichier: ui/Form1.cs
- Methode cle: StartConfiguredGame(...)

Flux:
1. Les metadonnees de partie sont configurees (joueurs, grille, id partie).
2. Une nouvelle partie est creee en base via PartieService.
3. L'historique local est vide.
4. RebuildStateFromHistory() reconstruit un etat initial propre.
5. L'application affiche le prompt d'action du joueur courant.


--------
LoadGame 
--------


## 5) Chargement d'une partie existante

- Fichier: ui/Form1.cs
- Methode cle: StartLoadedGame(partieId)

Flux:
1. La partie est relue en base via PartieService.
2. Les actions de la partie sont relues via ActionService.
3. L'historique local est rempli avec ces actions.
4. RebuildStateFromHistory() rejoue toutes les actions dans l'ordre.
5. Le plateau, les lignes, les scores et le tour courant sont restaurees.

--------
TurnDetector
--------

## 6) Tour de jeu: choix d'action

- Fichiers:
  - ui/Form1.cs (PromptCurrentTurnAction)
  - ui/atoms/TurnActionAlert.cs
  - core/events/TurnDetector.cs

Flux:
1. Une alerte demande au joueur courant:
   - Placer un point
   - Lancer une bombe
2. TurnDetector memorise l'action choisie.
3. Selon l'action:
   - mode point: clic sur grille
   - mode bombe: selection ligne canon puis puissance clavier (Ctrl + Numpad 1..9)

## 7) Action "placer un point"

- Fichiers:
  - ui/Form1.cs (Board_MouseClick)
  - core/GomokuEngine.cs (TryPlaceStone)

Flux:
1. Le clic est converti en coordonnees grille.
2. Le moteur valide la case (dans grille, non occupee).
3. La pierre est ajoutee.
4. Le moteur cherche les nouvelles lignes de 5.
5. Les lignes detectees sont ajoutees a l'affichage.
6. Les scores sont mis a jour (1 point par ligne unique).
7. L'action est enregistree en base (table actions).
8. L'action est ajoutee a l'historique local.
9. Passage au joueur suivant.

## 8) Action "lancer une bombe"

- Fichiers:
  - ui/Form1.cs (HandleBombRowSelection, Form1_KeyDown)
  - ui/organisms/GameBoard.cs (canons)
  - core/GomokuEngine.cs (TryLaunchBomb, TryApplyBombAtTarget)

Flux:
1. Le joueur choisit une ligne en cliquant un canon.
2. Le joueur envoie la puissance via Ctrl + Numpad (1..9).
3. Le moteur mappe la puissance vers une colonne (regle de trois + floor).
4. Le moteur applique l'impact:
   - si point protege par ligne gagnante: aucun effet
   - si case vide: aucun effet
   - si pierre adverse hors protection: suppression
   - si pierre du tireur: ignore
5. Le plateau est resynchronise.
6. Les lignes/scores sont recalcules si necessaire.
7. L'action bombe est enregistree en base et en historique local.
8. Passage au joueur suivant (meme en cas d'echec du tir).

## 9) Undo / Retour (Ctrl+Z ou bouton)

- Fichier: ui/Form1.cs
- Methodes cles: UndoLastRound(), RebuildStateFromHistory(...)

Regle metier implementee:
- Un retour annule 2 actions:
  - le dernier tour adverse
  - puis ton tour precedent

Flux:
1. Verification qu'il y a au moins 2 actions.
2. Suppression des 2 dernieres actions en base (si possible).
3. Suppression des 2 dernieres actions de l'historique local.
4. Reconstruction complete de l'etat depuis l'historique restant.
5. Le tour revient au bon joueur.

## 10) Rebuild de l'etat (coeur de coherence)

- Fichier: ui/Form1.cs
- Methode cle: RebuildStateFromHistory(...)

Principe:
1. Reset total de l'etat UI et metier local.
2. Recreation moteur + detecteur de tour + etat partie.
3. Replay de chaque action dans l'ordre:
   - POINT -> TryPlaceStone
   - BOMBE -> TryApplyBombAtTarget
4. Resynchronisation plateau, lignes, scores, tour courant.

Ce mecanisme garantit la coherence pour:
- chargement de partie
- undo
- reprise apres modifications

## 11) Fin de partie

- Fichiers:
  - ui/Form1.cs (EndGameButton_Click)
  - core/events/EtatPartie.cs
  - ui/atoms/GameResultAlert.cs

Flux:
1. Le bouton "Terminer la partie" passe l'etat a Finie.
2. Le dialogue resultat affiche:
   - score joueur 1
   - score joueur 2
   - vainqueur / egalite
3. L'utilisateur choisit:
   - Fermer
   - Refaire une partie

## 12) Rejouer

- Fichier: ui/Form1.cs

Flux:
1. Si "Refaire une partie" est choisi, le menu de setup est reouvert.
2. On repart sur le meme flux:
   - nouvelle partie
   - ou chargement

## 13) Couches et responsabilites

- core/
  - Regles de jeu (moteur, lignes, tours, etat partie)
- ui/
  - Affichage, interactions utilisateur, prompts
- data/
  - Acces SQL generique et connexion
- model/
  - Modeles de tables SQL
- service/
  - Logique metier de persistance (parties, actions, sauvegardes)

Cette separation permet de garder un code lisible, testable et proche d'une architecture en couches.
