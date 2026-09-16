# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

"Blood Court / Iron March" — a Unity roguelike deckbuilder built on a chess board. Players draft a clan
(Blood Court or Iron March), place pieces on an 8x8 board, fight scripted enemy encounters using chess-like
movement plus a card/spell layer, and progress through a branching node map between battles (à la
Slay the Spire). Unity version: **6000.2.1f1** (see `ProjectSettings/ProjectVersion.txt`).

There is no README beyond a placeholder and no CLAUDE.md prior to this one.

## Working with this repo

This is a Unity project, not a CLI-buildable app — there are no build/lint/test scripts. Unity regenerates
`*.csproj`/`*.sln`/`Library/`/`obj/`/`Temp/` on open (all gitignored), so don't hand-edit or commit them.
All gameplay scripts compile into the default `Assembly-CSharp` assembly — there are **no `.asmdef` files**,
so every script in `Assets/Scripts` shares one compilation unit.

- **No automated test suite exists.** `com.unity.test-framework.performance` is present as a package
  dependency but nothing under `Assets` uses it. Verify changes by opening the project in the Unity Editor
  and using Play Mode.
- To check compile errors without opening the Editor UI, use Unity's batch mode:
  `Unity.exe -batchmode -nographics -projectPath . -logFile - -quit`
- Scenes (`Assets/Scenes/`): `StartScreen.unity` → `ClanSelectScene.unity` → `MapScene.unity` →
  `SampleScene.unity` (the battle scene) → `ShopScene.unity`; `UI_Battle.unity` holds battle UI. Scene
  transitions go through `GameManager.SceneController` (singleton, `DontDestroyOnLoad`), which loads by
  scene name and hands off data via the static `SceneArgs.Payload` bucket.
- `Assets/Scripts/EncounterSetup/README_MapSetup.cs` is an in-editor doc-comment (not a real script) that
  explains how the node-map scene is wired up in the Unity Editor — read it before touching `MapGenerator`,
  `MapNodeVisual`, or `RunMapController`.

## Architecture

### Global run state vs. per-battle state

- **`GameSession`** (`Assets/Scripts/EncounterSetup/GameSession.cs`) is a `DontDestroyOnLoad` singleton
  (`GameSession.I`) holding everything that persists *across scenes for the whole run*: selected clan,
  army (`List<PieceDefinition>`), the current run's card deck, spell pool, map position/movement-type
  currency, and queued piece upgrades. `PieceDefinition` ScriptableObjects are **cloned at runtime**
  (`CreateRuntimePiece`) so upgrades/stat changes never mutate the shared asset; `GameSession` keeps a
  runtime-clone → original-template map for resolving UI back to the canonical definition.
- **`TurnManager`** (`Assets/Scripts/Chess/Setup/TurnManager/`) is a scene-local singleton owning one
  battle's state: AP (action points for moves/attacks), Mana (spell phase currency), turn phase, command
  history, and clan/ability wiring. It's a large `partial class` split by concern — when editing turn flow,
  check all of these before assuming a piece is complete:
  - `TurnManager.cs` — fields, AP/mana core
  - `TurnManager.TurnFlow.cs` — phase transitions (`Preparation → SpellPhase → PlayerTurn → EnemyTurn → Cleanup`)
  - `TurnManager.PlayerActions.cs`, `TurnManager.Combat.cs` — player move/attack entry points
  - `TurnManager.EnemyAI.cs`, `TurnManager.EnemyIntents.cs` — enemy turn resolution + telegraphed intent highlighting
  - `TurnManager.ClanAbilities.cs`, `TurnManager.Helpers.cs`, `TurnManager.RestartRound.cs`, `TurnManager.VictoryLives.cs`

### Command pattern for undo/redo

All player-initiated board actions implement `IGameCommand` (`Execute()`/`Undo()`, `Assets/Scripts/Commands/`)
and run through `CommandHistory` (`TurnManager._history`), which maintains undo/redo stacks and fires
`GameEvents.OnCommandExecuted/Undone/Redone`. `MoveCommand`, `AttackCommand`, `PlayCardPlaceCommand`, and
enemy/boss equivalents (`EnemyMoveCommand`, `EnemyAttackCommand`, `BossPatrolCommand`, `BossRayAttackCommand`)
all follow this shape. When adding a new player action, implement `IGameCommand` and route it through
`TurnManager.ExecuteCommand`/`_history.Execute` rather than mutating board state directly, so undo/redo and
the `OnCommand*` events keep working.

### Event bus

`GameEvents` (`Assets/Scripts/Events/GameEvents.cs`) is a static class of `System.Action` delegates —
the de facto pub/sub backbone connecting board logic, UI, sound, and abilities without direct references
(turn flow, AP/mana changes, piece spawn/move/capture, status applied/removed, card lifecycle, spell flow,
encounter won/lost). Prefer wiring new UI/VFX/SFX reactions through `GameEvents` rather than adding new
direct calls into `TurnManager`/`Piece`.

### Piece data model: 3 layers

1. **`PieceDefinition`** (ScriptableObject, `Assets/Scripts/PieceDefinitions/`) — designer-authored base
   stats/prefab reference. Cloned per-run by `GameSession` so runtime edits don't touch the asset.
2. **`Piece`** (abstract `MonoBehaviour`, `Assets/Scripts/Chess/Piece.cs`) — the board-facing piece: team,
   coordinate, movement rules (`GetLegalMoves`), and legacy serialized stat fields kept for old prefabs.
   Concrete pieces (`Pawn`, `Knight`, `Bishop`, `Rook`, `Queen` under `PlayerPieces/`; `EnemyPawn`,
   `EnemyKnight`, etc. under `Enemies/`) implement movement per chess rules (occasionally modified — pieces
   can gain keyword abilities like Piercing/Sweeping Strike that change legal-move/attack resolution).
3. **`PieceRuntime`** (`MonoBehaviour`, `Assets/Scripts/Chess/Clans/PieceRuntime.cs`) — sits alongside
   `Piece`, owns live HP/Attack/Movement, applied `PieceUpgradeSO`s (slot-limited), and innate/keyword
   `PieceAbilitySO` lists. It dispatches lifecycle hooks (`OnSpawn`, `OnBeginPlayerTurn`, `OnPieceMoved`,
   `OnAttackPreCalc`/`OnAttackResolved`, `OnPieceCaptured`, etc.) to every ability so clan/upgrade behavior
   stays data-driven — new passive effects are usually a new `PieceAbilitySO` subclass overriding hooks, not
   a change to `PieceRuntime` itself.

Clan-level (not per-piece) behavior — auras, queen abilities — goes through `AbilitySO`
(`Assets/Scripts/Chess/Clans/`) and `ClanRuntime`, built once per battle in `TurnManager.BuildClanRuntime`
from the `ClanDefinition` asset selected in `GameSession`.

### Card & spell system

- `CardDefinitionSO` (abstract ScriptableObject, `Assets/Scripts/Chess/CardSystem/`) has two concrete
  subclasses: `UnitCardDefinitionSO` (summons a `PieceDefinition` onto the board) and
  `SpellCardDefinitionSO` (resolves one or more `SpellEffectSO`s).
- `SpellEffectSO` is the per-effect strategy object (`Resolve(SpellContext)` / `Undo(SpellContext)`) — each
  concrete spell (`DamageTargetPieceEffectSO`, `DrawCardsEffectSO`, `ApplyBleedScalingEffectSO`, etc., under
  `CardDefinitionS/Spells/{BloodCourtSpells,IronDom_Spells}/`) is a small ScriptableObject asset, composed
  onto a `SpellCardDefinitionSO`. Casting goes through `CastSpellCardCommand` (another `IGameCommand`), so
  spell resolution participates in the same undo/redo history as board moves.
- `DeckManager` owns the runtime deck/hand/discard/exhaust piles and mana spending; `HandPanel`/`CardView`
  are the UI layer, driven by `GameEvents` card lifecycle events (`OnCardDrawn`, `OnCardAddedToHand`, etc.).

### Status effects

`StatusController` (per-piece `MonoBehaviour`) stores stack counts keyed by `StatusId` enum and fires
`GameEvents.OnStatusApplied/OnStatusRemoved`. `StatusDefinition`/`StatusDatabase` (ScriptableObjects) hold
designer data (icons, tick behavior); `StatusTickSystem` applies end-of-turn ticking (e.g. Bleed) across the
board. Clan-specific status glue lives next to the clan code (e.g. `BleedSystem`, `FortifyStatusUtility`,
`RetaliateStatusUtility`, `IronMarch_FortifyEndTurn`).

### Enemies

`EnemyBehaviorFactory` + `IEnemyBehavior` implement a strategy pattern for enemy AI per piece type
(`EnemyChaseClosest`, `EnemyGreedyCapture`, `EnemyBishopForwardBehavior`, etc.). `TurnManager.EnemyAI.cs`
drives resolution each enemy turn; `TurnManager.EnemyIntents.cs` precomputes and highlights next-turn threat
tiles before the player acts. `BossEnemy` + `BossPatrolCommand`/`BossRayAttackCommand` implement scripted
boss patterns outside the normal per-piece behavior strategy. Encounter composition (which enemies spawn,
in what wave, difficulty tier) is authored via `EncounterDefinition`/`EncounterWave`/`EncounterCatalog`
ScriptableObjects and executed by `EncounterRunner`/`EncounterSetup`.

### Map / run progression

`MapGenerator` procedurally builds a branching node graph (`MapNode`, weighted by `MapNodeType`: Encounter/
Shop/RandomEvent) each run; `MapState` persists the layout and progress to `PlayerPrefs` so returning to
`MapScene` resumes where the player left off (cleared by `GameSession.StartNewRun`). `RunMapController`
handles node clicks → lock current row → unlock next row → scene transition via `SceneController`. Map
*movement type* (Rook/Bishop/Knight/Queen) is a separate currency tracked on `GameSession`
(`rookMapMoveCount`, etc.) that gates which nodes a player can path to — granted as run rewards, not the
same thing as in-battle piece movement.

### Namespaces

Gameplay code is split across three main namespaces: `Chess` (board/turn/piece/clan/status — most of the
game), `Card` (card/spell system), and `GameManager` (scene loading, menus). `GameSession`,
`CurrencyManager`, and a few top-level MonoBehaviours sit in the global namespace.
