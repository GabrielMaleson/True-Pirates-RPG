# Changes Log

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
