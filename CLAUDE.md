# True Pirates RPG — Project Guide for Claude

## Project Summary
A 2D RPG built in Unity (2D, C#). Side-scrolling exploration with turn-based combat loaded additively. Portuguese-language story about Benito, Simon, and Joodie aboard a pirate airship (Highwaker). Built with Yarn Spinner for dialogue.

---

## Architecture Overview

### Scene Structure
- **Main map scene** — exploration, NPCs, chests, triggers
- **Combat scene** (`Combat`) — loaded additively over the map, unloaded after battle
- **DontDestroyOnLoad** — `SistemaInventario` (inventory/party singleton, tagged `"Inventory"`), `EncounterData` (combat data), `DialogueManager` (Yarn UI + commands, tagged `"Inventory"`)

### Combat Transition Flow
1. `EncounterStarter.StartEncounter()` or `DynamicCutsceneScript.StartRatFightTrue()` or `SpecialCutsceneScript.StartEncounterCoroutine()` triggers combat
2. Creates `PreviousScene` → `UnloadScene()` deactivates all non-Inventory/non-Ignore root objects
3. `EncounterData` (DontDestroyOnLoad) carries party + enemy data into the Combat scene
4. Combat ends → `CombatSystem.ReturnToMapAfterDelay()` → `PreviousScene.LoadScene()` reactivates stored objects, unloads Combat scene

### PreviousScene Tag System
- **`"Inventory"`** — DontDestroyOnLoad, never stored/deactivated (e.g. SistemaInventario, DialogueManager)
- **`"Ignore"`** — not stored, not deactivated — object maintains current active/inactive state through combat (must be a **root** GameObject to work correctly)
- **`KeepDeactivated` component** — added by `<<deactivate charname>>` Yarn command. PreviousScene skips reactivating root objects with this component. Children deactivated via `SetActive(false)` also stay deactivated automatically when parent is reactivated (Unity preserves child active-state).

### CRITICAL RULE — `encounterStarterObject`
`PreviousScene.LoadScene()` calls `Destroy(encounterData.encounterStarterObject)`. This is meant ONLY for `EncounterStarter` trigger-zone objects.
**Never set `encounterData.encounterStarterObject = gameObject` in cutscene manager scripts** (`DynamicCutsceneScript`, `SpecialCutsceneScript`). Doing so destroys those managers after combat, nuking their static instances, child NPCs, and Yarn command handlers.

---

## Key Scripts

### Exploration / Scene Management
| File | Class | Role |
|---|---|---|
| `EncounterS/PreviousScene.cs` | `PreviousScene` | Stores/restores scene on combat enter/exit |
| `EncounterS/EncounterStarter.cs` | `EncounterStarter` | Collision trigger → starts a random/fixed encounter |
| `EncounterS/EncounterData.cs` | `EncounterData` | DontDestroyOnLoad data carrier for one combat |
| `EncounterS/KeepDeactivated.cs` | `KeepDeactivated` | Marker component: this object should NOT be reactivated by PreviousScene |
| `SistemaInventario.cs` | `SistemaInventario` | Singleton — inventory, party members (`List<PartyMemberState>`), gold, game progress tags |
| `MovimentacaoExploracao.cs` | `MovimentacaoExploracao` | Player movement; `StopForDialogue()` / static stop control |

### Dialogue / Cutscene
| File | Class | Role |
|---|---|---|
| `Dialogue Scripts/ImageScript.cs` | `DialogueManager` | Singleton, DontDestroyOnLoad. Yarn UI, all static Yarn commands (`<<darken>>`, `<<brighten>>`, `<<sprite>>`, `<<progress>>`, etc.), `StartDialogue()` entry point |
| `Dialogue Scripts/DynamicMovementDialogue.cs` | `DynamicCutsceneScript` | Manages named NPC GameObjects. Handles `<<activate name>>`, `<<deactivate name>>`, `<<move name point>>`, `<<startratfight>>`, `<<joodieadd>>`. Uses `static instance` — must stay alive for entire scene. |
| `Dialogue Scripts/SpecialCutscene.cs` | `SpecialCutsceneScript` | Manages Simon/Timon cutscene. Handles `<<simonenter>>`, `<<guysleave>>`, `<<startencounter>>`, etc. Must NOT be destroyed mid-scene. |
| `Dialogue Scripts/LoadScriptFirst.cs` | `ProgressCheck` | Fires `OnEnable` (when PreviousScene reactivates it). Checks progress conditions, starts a dialogue if met. Used to trigger post-combat story beats. |
| `Interaction/NPCTalker.cs` | `NPCTalker` | Player walks up + Space → starts a Yarn dialogue. `hasstarted` prevents re-triggering. `compassquest` flag blocks interaction until `HasProgress("compass")`. |

### Combat
| File | Class | Role |
|---|---|---|
| `Battle/CombatSystem.cs` | `CombatSystem` | Turn-based logic, spawns character visuals from `partyMemberVisualPrefab`. Filters out HP=0 members in `InitializeCombat()`. |
| `Battle/CombatUIManager.cs` | `CombatUIManager` | UI cards for party/enemies, action menu, targeting |
| `PartyData.cs` | `PartyMemberState` | Runtime character state (HP, AP, EXP, equipment). Non-serialized `transform` field for combat visual. **Same objects referenced by SistemaInventario AND EncounterData** — HP changes in combat persist. |

### Persistence
| File | Class | Role |
|---|---|---|
| `Interaction/ChestScript.cs` | `ChestScript` | Checks `GetGameProgress().Contains(chestID)` in `Start()` to restore open state. Adds chestID to progress on open. |
| `Dialogue Scripts/SaveLoadManager.cs` | `SaveLoadManager` | Saves/loads `gameProgress`, inventory, party to persistent storage |

---

## Yarn Commands Reference

### DialogueManager (static, always available)
`<<darken>>`, `<<brighten>>`, `<<sprite tag position>>`, `<<removesprite position>>`, `<<progress id>>`, `<<removeprogress id>>`, `<<wait seconds>>`, `<<playsound name>>`, `<<sceneload sceneName>>`, `<<add_objective text>>`

### DynamicCutsceneScript (requires live instance in scene)
`<<activate characterName>>` — activates NPC, removes KeepDeactivated
`<<deactivate characterName>>` — deactivates NPC, adds KeepDeactivated
`<<move characterName pointName>>` — moves NPC to waypoint
`<<startratfight>>` — starts the rat encounter
`<<joodieadd>>` — adds Joodie to party

### SpecialCutsceneScript (requires live instance in scene)
`<<simonenter>>`, `<<timonenter>>`, `<<guysleave>>`, `<<dooropen>>`, `<<doorclose>>`, `<<surprise>>`, `<<runaway>>`, `<<startencounter>>`, `<<addsimon>>`

### NPCTalker (static)
`<<additem itemId [quantity]>>` — gives item from the NPCTalker's `giveableItems` list

---

## Story / Progress Tags
Key progress strings used throughout the game:
- `piece2`, `piece3` — navigation device fragments
- `ratkilled` — added during `sidequest3_ratcomplete` dialogue
- `mutanttime`, `mutantbosstime` — trigger FightProgress monster spawns
- `startencounter_matriz` — triggers the boss mutant fight
- `dispositivo_completo` — all 3 pieces combined
- `compass` — NPCTalker `compassquest` gate

---

## Common Pitfalls
1. **Cutscene managers must not be `encounterStarterObject`** — only real trigger-zone objects (from `EncounterStarter`) should be destroyed post-combat.
2. **`KeepDeactivated` on root vs child** — works correctly either way: root objects are checked by PreviousScene directly; child objects keep their own `SetActive(false)` state when parent is reactivated.
3. **`"Ignore"` tag only works on root GameObjects** — children of non-Ignore roots get deactivated with their parent.
4. **`GetPartyMembersForCombat()` returns the same `PartyMemberState` references** — HP changes in combat modify the live objects in `SistemaInventario.partyMembers`.
5. **`DialogueManager.StartDialogue()` must call `EnablePlayerControl()` on early return** — otherwise any caller that stopped movement before calling it will softlock the player.
