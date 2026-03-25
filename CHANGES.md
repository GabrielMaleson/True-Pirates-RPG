# Changes Log

---

## 2026-03-24 (session 6)

### Fix: Three console errors on play
**Files:** `Assets/Scripts/Dialogue Scripts/ImageScript.cs`, `Assets/Shaders/BattleTransition.shader`

**`[YarnCommand("wait")] duplicate registration`** — Yarn Spinner already registers `wait` as a built-in command. `DialogueManager` had a manual `[YarnCommand("wait")]` coroutine that conflicted with it, printing the error on every scene load. **Removed** the duplicate; Yarn's native `wait` handles `<<wait seconds>>` correctly.

**`Material doesn't have texture property '_MainTex'`** — `RawImage` always tries to set `_MainTex` on any custom material assigned to it. `BattleTransition.shader` only declared `_GradientTex`. **Added** `_MainTex` as a declared (unused) property to the shader's Properties block so Unity stops logging the error.

**`MissingComponentException on "Objective Canva"`** — Inspector fix only: add a `Canvas Group` component to the "Objective Canva" GameObject. `PartyMenuManager.GetObjectiveCanvasGroup()` calls `GetComponent<CanvasGroup>()` on it; without the component Unity throws every time objectives are toggled.

---

## 2026-03-24 (session 5)

### Feature: SFX system — all player interactions now have sound effects
**Files:** `Assets/Scripts/SFXManager.cs` (new), `Assets/Scripts/Battle/CombatUIManager.cs`, `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`, `Assets/Scripts/Menu Scripts/ItemDetails.cs`, `Assets/Scripts/Interaction/ChestScript.cs`, `Assets/Scripts/Interaction/DoorScript.cs`, `Assets/Scripts/CraftingSimples.cs`

**Created** `SFXManager` — DontDestroyOnLoad singleton with two AudioSources (one-shot SFX + looping ambiance). Public AudioClip fields for all 10 SFX files in `Assets/Sounds/SFX/`.

**Wired into:**
- `CombatUIManager` — UIForward on attacks/items/defend/wait/target-select; UIBackward on undo/cancel/cancel-targeting/defend-unavailable; SuccessAcquired on victory; DefeatFail on defeat
- `PartyMenuManager` — UIForward on OpenMenu; UIBackward on CloseMenu
- `ItemDetails` — UIForward on equip/use success; UIBackward on drop/close
- `ChestScript` — ChestDoorOpen + PieceCraftFound2 on chest open
- `DoorScript` — ChestDoorOpen on door open
- `CraftingSimples` — PieceCraftFound on craft success; UIBackward on craft failure

**BoatAmbiance / BoatAmbianceInside** — included in SFXManager with `PlayLoop(clip)` / `StopLoop()` methods; call from scene-specific scripts or Yarn commands as needed.

**Inspector:** Create a GameObject named `SFXManager` in any persistent scene, attach the `SFXManager` component, and assign all 10 AudioClips from `Assets/Sounds/SFX/` to the matching fields.

---

## 2026-03-24 (session 4)

### Feature: CrashingWaves added as 8th battle transition type
**Files:** `Assets/Scripts/Battle/BattleTransitionManager.cs`, `Assets/Scripts/Battle/BattleTransitionConfig.cs`

User initially requested replacing Gooey with CrashingWaves, then revised to keep both.

**Added** `CrashingWaves = 7` to `BattleTransitionType` enum. **Added** procedural fallback in `GradientValue()` — two sinusoidal wave fronts (fromTop, fromBottom) collide at the horizontal center. **Added** `public Texture2D crashingWaves` field to `BattleTransitionConfig` and its corresponding `case` in `GetTexture()`. Gooey (procedural Voronoi) was retained at index 5; CrashingWaves is index 7.

**Inspector:** Assign `crashingWaves` texture in BattleTransitionConfig asset; leave `gooey` empty to keep procedural fallback.

---

## 2026-03-24 (session 3)

### Fix: Shop buttons unresponsive after returning from combat
**File:** `Assets/Scripts/EncounterS/PreviousScene.cs`

Unity disables duplicate EventSystems automatically. When the Combat scene (with its own EventSystem) loaded additively, the exploration scene's EventSystem was disabled as a duplicate. After the Combat scene unloaded its EventSystem was destroyed, but the exploration one remained disabled — so all UI button clicks stopped working.

**Added** `FixEventSystems()` called from `LoadScene()` alongside the existing `FixAudioListeners()`. It finds all EventSystems after restoration; if none are enabled, it enables the first one found (or creates a new one if none exist).

---

### Fix: All party/enemy characters spawning on the same spot
**File:** `Assets/Scripts/Battle/CombatSystem.cs`

`GetDefaultPartySpawnPos` and `GetDefaultEnemySpawnPos` had identical X and Y viewport coordinates for every index, stacking all characters on the same world position.

**Changed** to stagger characters vertically (Y = 0.40 / 0.30 / 0.20) with a slight X zigzag, giving a classic JRPG stack formation on each side.

---

### Feature: Game over on defeat — loads main menu instead of returning to exploration
**File:** `Assets/Scripts/Battle/CombatSystem.cs`

On defeat, the previous code returned the player to the exploration scene (same as victory), with no consequence.

**Added** `public string mainMenuSceneName = "Menu"` field. `ReturnToMapAfterDelay` now checks `currentState`: DEFEAT → `SceneManager.LoadScene(mainMenuSceneName)` (single mode, clears everything); VICTORY → original exploration restore flow. **Set `mainMenuSceneName` in the inspector to match your actual main menu scene name.**

---

### Fix: Simon's battle music not playing
**File:** `Assets/Scripts/Battle/CombatSystem.cs`

`MusicManager.PlayClip` has an early-out guard (`if clip == current clip && isPlaying → skip`) that silently skipped playing if the exploration music happened to be the same clip. Also, if exploration music was still playing when combat started, there was no guarantee it would be replaced.

**Added** `MusicManager.Instance?.StopMusic()` call before `PlayClip` in `InitializeCombatWithData`. This always clears the current clip so `PlayClip` always fires.

**Inspector:** Also assign `battleMusic` AudioClip on Simon's EncounterFile — the code is correct but the field must be set.

---

### Fix: Combat background not fitting screen on rat fight
**Files:** `Assets/Scripts/Battle/FitToCamera.cs`, `Assets/Scripts/Battle/CombatSystem.cs`

`FitToCamera.Awake()` assigned `Camera.main`, which is null when the exploration camera has been deactivated by `PreviousScene.UnloadScene()`. `Fit()` then silently did nothing, leaving the background unscaled.

**Changed** `Fit()` to accept an optional `Camera cam` parameter (`Fit(Camera cam = null)`); uses `cam ?? targetCamera`. `CombatSystem` now calls `Fit(GetCombatCamera())`, passing the resolved combat camera explicitly. **Also assign `targetCamera` on FitToCamera in the inspector to the combat camera as a permanent backup.**

---

## 2026-03-24 (session 2)

### Fix: Combat camera lookup returning exploration camera → wrong spawn positions + black screen
**Files:** `Assets/Scripts/Battle/CombatSystem.cs`, `Assets/Scripts/Dialogue Scripts/DynamicMovementDialogue.cs`

`GetCombatCamera()` was falling back to `Camera.main` when no camera was found in the Combat scene. `Camera.main` is the exploration camera, which is at a different world position depending on where the player is. Viewport-based spawn positions were therefore relative to the exploration camera's current location — correct for the first fight (camera near origin) but wrong for the rat fight (camera has moved). The fallback also caused a null-ref crash when the exploration camera was already deactivated by `PreviousScene`, leaving the screen black.

**Added** `public Camera combatCamera` inspector field to `CombatSystem`. `GetCombatCamera()` checks this first; the scene-based search is a secondary fallback with a clear warning. **Assign the Combat scene's camera to this field in the Unity inspector.**

**Also added** the missing reverse transition call in `InitializeCombatWithData()` — `BattleTransitionManager.Instance.PlayReverseTransition(type)` now fires after spawning, revealing the combat scene from black.

**Also converted** `DynamicCutsceneScript.StartRatEncounter()` from `static void` to `static IEnumerator`. The coroutine now blocks Yarn dialogue until the transition callback fires (`WaitUntil`), preventing the dialogue-complete event from triggering player-control re-enabling during the 0.7 s transition window. Logic from `StartRatFightTrue()` was inlined and the method removed.

---

## 2026-03-24

### Fix: Combat character visuals spawning in wrong scene (additive loading)
**File:** `Assets/Scripts/Battle/CombatSystem.cs`

`Instantiate()` places objects in the active scene, which stays as the exploration scene during additive loading. Party and enemy walk-sprite prefabs were appearing in the exploration scene hierarchy instead of the Combat scene, causing wrong camera rendering and incorrect positions.

**Added** `SceneManager.MoveGameObjectToScene(obj, gameObject.scene)` after each `Instantiate()` call for both party and enemy visuals.

---

### Feature: Camera-viewport-based spawn positions for combat characters
**File:** `Assets/Scripts/Battle/CombatSystem.cs`

Manually positioned spawn-point transforms inside a UI Canvas were being read as world coordinates scaled by the Canvas Scaler, placing characters in a tiny cluster near the camera origin.

**Added** `GetDefaultPartySpawnPos()` and `GetDefaultEnemySpawnPos()` using `Camera.ViewportToWorldPoint()` to calculate spawn positions at runtime (party left side, enemies right side, all at 20% from screen bottom). Spawn-point inspector fields are kept but bypassed — code always uses viewport positions.

---

### Feature: Combat background sprite per encounter
**Files:** `Assets/Scripts/EncounterS/EncounterFile.cs`, `Assets/Scripts/Battle/CombatSystem.cs`

Replaced `battleBackgroundName` (unused string) with `battleBackground` (Sprite) on `EncounterFile`. `CombatSystem` now has a `backgroundRenderer` (SpriteRenderer) field; `InitializeCombatWithData()` assigns the sprite from the encounter file. Added warnings when either field is unassigned.

**Inspector:** Assign `backgroundRenderer` on the CombatSystem object; assign `battleBackground` sprite on each EncounterFile.

---

### Refactor: Shared encounter setup via `EncounterStarter.BuildEncounterData()`
**Files:** `Assets/Scripts/EncounterS/EncounterStarter.cs`, `Assets/Scripts/Dialogue Scripts/DynamicMovementDialogue.cs`, `Assets/Scripts/Dialogue Scripts/SpecialCutscene.cs`

Both cutscene scripts duplicated the EncounterData setup logic. Extracted into a `public static BuildEncounterData(EncounterFile, SistemaInventario)` method on `EncounterStarter`. All three encounter starters now share identical data-setup logic; only `EncounterStarter.StartEncounter()` sets `encounterStarterObject`.

**Also fixed in SpecialCutsceneScript:** was using `FindFirstObjectByType<SistemaInventario>()` instead of `SistemaInventario.Instance`, and was missing `CalculateRewards()` call.

---

### Fix: Party menu not updating when Simon/Joodie join mid-game
**File:** `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

`CreatePartyMemberDisplays()` was only called at `Start()` and `OpenMenu()`. When `<<joodieadd>>` or `<<addsimon>>` fired during a cutscene, the menu never refreshed its button/display list.

**Added** `inventory.onPartyUpdated += CreatePartyMemberDisplays` subscription in `Start()`.

---

### Fix: Missing yarn node `sidequest2_complete` causing softlock
**File:** `Assets/Scripts/Dialogue Scripts/prologue_parte2.yarn`

An NPCTalker in the Main Scene referenced a dialogue node `sidequest2_complete` that did not exist in any yarn file. The DialogueRunner would start but never fire `onDialogueComplete`, leaving the player permanently locked.

**Added** stub node `sidequest2_complete` with `<<darken>>` / `<<brighten>>` to unblock; content to be filled in.

---

### Fix: CraftingSimples merge conflict — compass progress now correctly added
**File:** `Assets/Scripts/CraftingSimples.cs`

Merge conflict between `refactor-fixes` and `main` left duplicate/broken setup code in `CraftItem()`. Resolved by keeping the `SistemaInventario.Instance` reference pattern from the refactor branch. `inv.AddProgress("compass")` now always runs after a successful craft.

---

### Fix: FightProgress re-triggering on Rigidbody2D.WakeUp() — Timon soldier softlock
**File:** `Assets/Scripts/Dialogue Scripts/FightProgress.cs`

When `DisablePlayerControl()` / `EnablePlayerControl()` call `Rigidbody2D.Sleep()` / `WakeUp()`, Unity re-evaluates all overlapping triggers. If the player was inside a `FightProgress` zone when a dialogue ended, `OnTriggerEnter2D` would fire again, attempting to start `cantprogressyet` while movement was already re-enabled — causing a softlock when approaching the Timon soldier left→right.

**Added** `if (doingthing) return` guard in `OnTriggerEnter2D` and `OnTriggerExit2D` to reset the guard when the player actually leaves. Works together with the `IsDialogueRunning` guard in `StartDialogue()`.

---

### Feature: Objectives panel toggle in party menu (alpha-based, never deactivated)
**File:** `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

Added `ToggleObjectives()` (flips `ObjectiveManager`'s `CanvasGroup` alpha 0↔1), `CloseObjectivesPanel()` (sets alpha to 0), and `public GameObject objectivesButton` inspector field. `OpenMenu()` closes the objectives panel before opening party menu. `CloseMenu()` re-shows both `MenuOpener` and `objectivesButton`. The objectives panel is never `SetActive(false)` — visibility is alpha-only to preserve its DontDestroyOnLoad state.

**Wire in inspector:** assign the objectives HUD button to `objectivesButton`; wire the objectives button's OnClick to `PartyMenuManager.ToggleObjectives()`.

---

## 2026-03-23

### Fix: Cutscene managers destroyed after combat — root cause of multiple bugs
**Files:** `Assets/Scripts/Dialogue Scripts/DynamicMovementDialogue.cs`, `Assets/Scripts/Dialogue Scripts/SpecialCutscene.cs`

Both `DynamicCutsceneScript.StartRatFightTrue()` and `SpecialCutsceneScript.StartEncounterCoroutine()` were setting `encounterData.encounterStarterObject = gameObject`. `PreviousScene.LoadScene()` destroys whatever is in `encounterStarterObject` after combat — intended only for disposable trigger-zone objects (`EncounterStarter`), not manager scripts.

**Removed** `encounterData.encounterStarterObject = gameObject` from both methods.

**Bugs fixed by this change:**
- Janitor (Zelador) not spawning after rat fight — `DynamicCutsceneScript.instance` was set to null on destroy, breaking `<<activate>>` and `<<deactivate>>` Yarn commands; child NPCs were also physically destroyed
- Party members (truejoodie world sprite) disappearing after first battle — child of DynamicCutsceneScript, destroyed with it
- Chest reverting to closed state after combat — potentially a child of the same object
- `"Ignore"` tag not helping — objects were being destroyed entirely, not just deactivated

---

### Fix: Commandant 2 softlock when dialogue already running
**File:** `Assets/Scripts/Dialogue Scripts/ImageScript.cs` (`DialogueManager`)

`NPCTalker` calls `MovimentacaoExploracao.StopForDialogue()` before calling `DialogueManager.StartDialogue()`. If `StartDialogue` returned early because `dialogueRunner.IsDialogueRunning` was already true, the player was left unable to move with no dialogue running.

**Added** `EnablePlayerControl()` to the early-return branch in `StartDialogue()`.

---

### Docs: CLAUDE.md and CHANGES.md created
**Files:** `CLAUDE.md`, `CHANGES.md`

Initial project documentation covering architecture, scene flow, PreviousScene tag system, key scripts, Yarn commands, progress tags, and known pitfalls.
