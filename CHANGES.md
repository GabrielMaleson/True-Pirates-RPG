# Changes Log

---

## 2026-04-01

### Fix: Shader de transição de batalha removido no build
**Files:** `ProjectSettings/GraphicsSettings.asset`

`Shader.Find("Custom/BattleTransition")` retornava null em builds porque o shader não estava referenciado por nenhum material de cena e não estava na lista Always Included Shaders. Resultado: sem efeito de wipe na transição (fallback imediato, sem tela preta). Adicionado o shader à lista `m_AlwaysIncludedShaders` em GraphicsSettings.asset.

---

## 2026-03-29

### Fix: ConfigMenu fecha sozinho na primeira abertura
**Files:** `Menu Scripts/ConfigMenu.cs`

`Start()` chamava `gameObject.SetActive(false)` — em Unity, `Start()` só executa na **primeira ativação**. Se o painel começar inativo na cena (como deveria), a primeira vez que settings era aberto, `Start()` disparava e fechava o painel imediatamente. Movido para `OnEnable()` com flag `hasInitialized`. O painel deve começar **inativo** no Inspector.

---

### Fix: Save, áudio, mira de item em batalha e UI de objetivos
**Files:** `SaveLoadManager.cs`, `EncounterStarter.cs`, `CombatSystem.cs`, `PartyMenuManager.cs`, `SFXManager.cs`, `MusicManager.cs`, `CombatUIManager.cs`, `Objective Manager.cs`

**Save — `SaveLoadManager.Instance` era null → saves silenciosos:**
Adicionado `GetOrCreate()` (semelhante ao `BattleSaveManager`) — garante que o singleton sempre exista. Todos os callers (`PartyMenuManager.ConfirmLeave`, `PartyMenuManager.SaveGame`, `CombatSystem.ConfirmLeave`) agora usam `GetOrCreate()` em vez de `Instance?.`. Adicionado auto-save antes de batalha em `EncounterStarter.StartEncounter()` e `StartEncounterFromCutscene()`.

**Áudio — `AudioSource` sem `outputAudioMixerGroup` → slider não tinha efeito:**
`SFXManager` e `MusicManager` não roteavam suas `AudioSource`s pelo AudioMixer. Adicionados campos `sfxMixerGroup`, `loopMixerGroup` (SFXManager) e `musicMixerGroup` (MusicManager). Atribuir no Inspector para que os sliders de volume do `ConfigMenu` tenham efeito.

**Mira de item em batalha — UI congelava se `efeitos` estivesse vazio:**
Adicionado fallback: se o item não tiver `ItemEffectData` configurado, volta ao menu de ação com aviso em vez de travar a UI.

**Objetivos — botão mostrava nome interno:**
`CreateQuestButton` agora exibe "Quest #1", "Quest #2", etc. O nome completo só aparece no painel de detalhes.

---

### Fix: Fluxo de uso de item em batalha usa mesma lógica de mira que ataques
**Files:** `Battle/CombatUIManager.cs`

`OnItemSelected` usa `StartTargeting` (TargetSelector nos sprites de personagem), idêntico ao fluxo de ataques. Apenas consumíveis (`ehConsumivel && usavelEmBatalha`) aparecem no grid de itens. `RemoverItem` funciona corretamente com o sistema não-empilhável (remove o primeiro slot com aquele item). **Corrigido:** string "Used {item}" traduzida para "Usou {item}".

---

## 2026-03-28 (session 37)

### Refactor: Itens não são mais empilháveis + redesign de prefabs
**Files:** `SistemaInventario.cs`, `SlotUI.cs`, `PartyMenuManager.cs`

**Sem empilhamento:** `AdicionarItem` agora sempre cria um novo `SlotInventario` por item (cada cópia tem seu próprio slot). Removida lógica `ehEmpilhavel` e stacking. `RemoverItem` agora remove a primeira instância encontrada; adicionado `RemoverSlot(SlotInventario)` para remover por referência direta. `quantityText` removido de `SlotUI`.

**Prefabs — campos necessários:**

**SlotUI** (`Assets/Scripts/Menu Scripts/SlotUI.cs`):
- `itemIcon` → Image (ícone do item)
- `itemNameText` → TextMeshProUGUI (nome)
- `highlightBorder` → Image (borda amarela, desativada por padrão)
- `clickButton` → Button (cobre o slot inteiro)
- `equippedIndicator` → Image (checkmark/X, desativado por padrão)
- `checkmarkSprite` → Sprite (exibido quando item está equipado)
- `unequipXSprite` → Sprite (exibido ao passar o mouse em item equipado)

**ItemDetails** (`Assets/Scripts/Menu Scripts/ItemDetails.cs`):
- `itemIcon` → Image
- `itemNameText` → TextMeshProUGUI
- `itemDescriptionText` → TextMeshProUGUI
- `itemStatsText` → TextMeshProUGUI (oculto automaticamente para não-equipáveis)

---

## 2026-03-28 (session 36)

### Refactor: ItemDetails simplificado para tooltip puro
**Files:** `Assets/Scripts/Menu Scripts/ItemDetails.cs`, `SlotUI.cs`

`ItemDetails` era um painel completo com botões Usar/Equipar/Largar/Fechar que já eram todos ocultados ao ser usado como tooltip. Removidos todos os botões, listeners, referências a `SistemaInventario`/`PartyMenuManager`/`SlotInventario`, e a lógica de `Start()`. Agora é só `Initialize(DadosItem)` que preenche ícone, nome, descrição e stats de modificadores. `SlotUI.ShowTooltipAfterDelay` simplificado: removido o bloco de ocultação de botões; mantém apenas `Initialize` e o `CanvasGroup.blocksRaycasts = false`.

**Inspector:** Remover os campos `useButton`, `equipButton`, `dropButton`, `closeButton` do prefab ItemDetails no Unity.

---

## 2026-03-28 (session 38)

### Fix: Sair durante combate agora vai para o menu principal
**Files:** `Assets/Scripts/Battle/CombatSystem.cs`, `Assets/Scripts/Dialogue Scripts/SaveLoadManager.cs`

`ConfirmLeave()` chamava `SaveAndQuitFromBattle()` que terminava com `Application.Quit()` — fecha o jogo em build e não faz nada no editor. Extraída a lógica de escrita do save para `WriteBattleSave()` e adicionado `SaveAndReturnToTitle()` que salva e faz `SceneManager.LoadScene("TitleScreen")`. `ConfirmLeave` agora chama `SaveAndReturnToTitle`. `SaveAndQuitFromBattle` ainda existe e chama `Application.Quit()` para uso futuro.

---

## 2026-03-28 (session 35)

### Fix: Aba Party mostrando cartões de equipamento em vez dos displays de personagem
**Files:** `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

`ShowCharacter()` ativava o painel mas não repopulava `statsDisplayContainer` após `UpdateEquipmentDisplay()` ter substituído os displays por cartões de equipamento. Adicionado `RepopulatePartyDisplays()` que destrói os filhos atuais do container e recria os `statsDisplayPrefab` com HP/AP/XP. Chamado em `ShowCharacter()` quando `_equipmentCards.Count > 0` ou `statsDisplays` está vazio.

---

## 2026-03-28 (session 34)

### Refactor: Arma → Acessório + correções do fluxo de equipamento
**Files:** `DadosItem.cs`, `PartyData.cs`, `SistemaInventario.cs`, `SaveLoadManager.cs`, `PartyMemberStatsDisplay.cs`, `EquipmentCharacterCard.cs`, `EquipmentSlotUI.cs`, `ItemDetails.cs`, `Shopkeeper.cs`, `SlotUI.cs`, `PartyMenuManager.cs`

**Renomeações:** `EquipmentSlot.Arma` → `Acessorio` (mesma posição na enum, assets não quebram). Campo `PartyMemberState.weapon` → `accessory`. Métodos `EquipWeapon/UnequipWeapon` → `EquipAccessory/UnequipAccessory` em `PartyData`, `SistemaInventario`, e todos os chamadores. Strings exibidas atualizadas: "arma" → "acessório", "Weapon" → "Acessório". `SaveLoadManager.SavedPartyMember.weaponID` → `accessoryID`.

**Bug: popup antigo ainda aparecia** — Removido `ShowPartyMemberSelector()` de `SlotUI.OnItemClick()` no modo normal; clicar num equippable fora do fluxo de equipe agora faz nada (fluxo correto é pela aba Equipment).

**Bug: painel não recarregava após equipar** — `OnEquipItemSelectedFromPanel()` chamava `ShowEquipment()` que retornava cedo (guard `currentNavText == equipmentNavText && pendingOp == None`). Substituído por `ReturnToEquipmentView()` privado que chama `UpdateEquipmentDisplay()` diretamente.

**Botão Voltar:** Adicionado campo `public Button backButton` ao `PartyMenuManager`. Ativado em `StartUseItemTargeting` e `StartEquipFromSlot`, desativado em `CancelPendingOperation`. `OnBackClicked()`: UseItem → cancela e vai para Itens; EquipItem → cancela e volta para equipamento.

**Remover Item:** Em modo equip-filter, `RefreshInventoryDisplay()` cria um slot extra "Remover Item" antes dos itens filtrados. Ao clicar, desequipa o slot pendente, devolve o item ao inventário e retorna ao painel de equipamento.

**Inspector:** Atribuir campo `backButton` no `PartyMenuManager`. Atualizar prefab `EquipmentCharacterCard`: renomear `weaponIcon/weaponNameText/weaponSlotButton` → `accessoryIcon/accessoryNameText/accessorySlotButton`, e reatribuir no inspector. Renomear campo `weaponText` → `accessoryText` no prefab `PartyMemberStatsDisplay`.

---

## 2026-03-28 (session 33)

### Feature: Novo fluxo de usar item + painel de equipamento por personagem
**Files:** `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`, `SlotUI.cs`, `PartyMemberStatsDisplay.cs`, `EquipmentCharacterCard.cs` (novo)

**Usar item:** Clicar num consumível na aba Itens agora fecha o painel de itens, abre o painel de personagens com um texto "Selecione um personagem:", e habilita outline pulsante amarelo (sin(t×4), 2–5px) ao passar o mouse sobre cada personagem. Clicar usa o item e volta ao painel de itens. Troca de aba ou ESC cancela a operação.

**Painel de equipamento:** Substituído o layout antigo (slots do personagem selecionado) por cartões por personagem via `EquipmentCharacterCard`. Cada cartão mostra ícone, nome, slot arma (ícone + nome ou "nenhuma") e slot armadura. Clicar num slot abre o painel de itens filtrado pelo tipo de slot, e clicar num item o equipa no personagem do cartão, retornando ao painel de equipamento.

**Cancelar operações:** `HideSubPanels()` (chamado por todos os ShowX) agora invoca `CancelPendingOperation()`, que desativa targeting em todos os displays e esconde o texto de prompt. Guards dos ShowX atualizados para sempre processar quando há operação pendente.

**State machine:** `PartyMenuManager.PendingOpType { None, UseItem, EquipItem }` + campos `_pendingItemSlot`, `_pendingEquipMember`, `_pendingEquipSlot`. `RefreshInventoryDisplay` filtra por `_pendingEquipSlot` quando `_pendingOp == EquipItem`.

**Inspector:** Atribuir `equipmentCardPrefab` e `selectPromptText` no PartyMenuManager. Criar prefab `EquipmentCharacterCard` com os campos: `characterIcon`, `characterNameText`, `weaponIcon`, `weaponNameText`, `weaponSlotButton`, `armorIcon`, `armorNameText`, `armorSlotButton`.

---

## 2026-03-28 (session 32)

### Fix: Botão Continuar — texto cinza em vez de transparente
**Files:** `Assets/Scripts/Dialogue Scripts/TitleScreenCleanup.cs`

Substituído `continueButton.interactable = false` + `TMP_Text.color = 50% alpha` por `ColorBlock.disabledColor = new Color(0.5, 0.5, 0.5, 1)`. O texto agora fica cinza sólido (não fantasmagórico) quando não há save.

### Fix: Descrições curta/longa trocadas nos itens da loja
**Files:** `Assets/RPG/Items/*.asset` (todos os 12 itens)

Todos os itens tinham texto narrativo em `descricao` e `descricaoNarrativa` vazio. Migrado: texto narrativo → `descricaoNarrativa`; gerada nova `descricao` mecânica curta (ex: "Aumenta defesa em +5", "Restaura 15 de HP") derivada dos stats/efeitos de cada item.

### Fix: Loja ESC abrindo configurações ao mesmo tempo
**Files:** `Assets/Scripts/Shopkeeper.cs`, `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

Adicionado `static bool _anyShopOpen` e `IsAnyShopOpen()` ao Shopkeeper. `PartyMenuManager.Update()` agora retorna imediatamente ao receber Escape se `Shopkeeper.IsAnyShopOpen()` for true, evitando que configurações abram ao fechar a loja.

### Fix: Overflow dos itens na grade da loja
**Files:** `Assets/Scripts/Shopkeeper.cs`

Em `Start()`, adiciona `RectMask2D` ao pai do `slotGrid` se não existir, clampando visualmente os itens dentro dos limites do painel.

---

## 2026-03-28 (session 31)

### Fix: Ícone do item na loja não aparecia + campo shopCloseButton + Escape para fechar
**Files:** `Assets/Scripts/Shopkeeper.cs`

Ícone não aparecia porque `color` nunca era definido e o RectTransform não era dimensionado. Corrigido em `FillSelectedInfo`: define `color = Color.white`, chama `Canvas.ForceUpdateCanvases()`, lê largura do container pai e ajusta `sizeDelta = (w, w/aspect)`, centraliza via `anchorMin/Max.x = 0.5` e `pivot = 0.5`. Adicionado campo `shopCloseButton` (Button) com listener para `CloseShop()` em `Start()`. Adicionado suporte à tecla Escape em `Update()` para fechar a loja.

---

## 2026-03-27 (session 3)

### Feature: Nova UI de loja + sistema de duas descrições
**Files:** `Assets/Scripts/Shopkeeper.cs`, `Assets/Scripts/DadosItem.cs`

**DadosItem:** Adicionado `descricaoNarrativa` (TextArea) — texto de sabor/narrativo. `descricao` passa a ser a descrição mecânica curta.

**Shopkeeper:** Reescrito para o novo layout — `itemNameText`, `itemShortDescText`, `itemLongDescText`, `itemIcon`, `itemPriceText`, `itemTypeText`. Grade usa `slotShopPrefab` no `ItemSelectorGrid`. Slots ficam cinzas sem estoque. Compra toca `successAcquired`.

---

## 2026-03-27 (session 2)

### Fix: Personagens mortos de combates anteriores não spawnavam visual corretamente no retry
**Files:** `Assets/Scripts/Battle/CombatSystem.cs`

`InitializeCombatWithData()` spawnava visuals e adicionava à lista `partyMembers` todos os membros do grupo — incluindo os com HP=0 de combates anteriores. `InitializeCombat()` os filtrava da fila de turnos, mas o visual já estava na cena. Corrigido com check `currentHP <= 0` antes de spawnar, usando `aliveIndex` separado para posicionamento (evita lacunas). Mesmo guard adicionado para inimigos. O `InitializeCombat()` ainda filtra como segunda camada de segurança.

---

## 2026-03-27

### Feature: Menu de pausa em combate + saída com reinício de batalha
**Files:** `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`, `Assets/Scripts/Battle/CombatSystem.cs`, `Assets/Scripts/EncounterS/PreviousScene.cs`, `Assets/Scripts/Dialogue Scripts/SaveLoadManager.cs`

**PartyMenuManager:** ESC durante batalha agora chama `CombatSystem.TogglePauseMenu()` em vez de abrir configurações. Refatorado bloco de ESC para cobrir todos os casos (em batalha / menu aberto / menu fechado) em um único if.

**CombatSystem:** Adicionado header `Menu de Pausa` com campos `pauseMenuPanel` e `leaveConfirmPanel` (arrastar no Inspector). Novos métodos: `TogglePauseMenu()` (toggle do painel), `ShowLeaveConfirmation()` (exibe popup de confirmação), `CancelLeave()`, `ConfirmLeave()` (salva estado e chama `Application.Quit()`).

**PreviousScene:** Adicionado `public static string LastExplorationScene` — gravado em `UnloadScene()` para que o SaveLoadManager saiba o nome da cena de exploração durante um combate.

**SaveLoadManager:** `SaveData` recebeu `hasPendingBattleRestart` e `pendingEncounterFileName`. `SaveGame()` refatorado para usar `BuildSaveData()`. Novo `SaveAndQuitFromBattle(EncounterData)`: restaura HP pré-batalha via BattleSaveManager, salva com flag de reinício e chama `Application.Quit()`. `LoadGameCoroutine` agora detecta a flag e relança o encontro via `EncounterStarter.StartEncounterFromCutscene()`, limpando a flag antes para evitar loops. `EncounterFile` precisa estar em uma pasta `Resources/` para ser encontrado por `FindEncounterFileByName()`.

---

## 2026-03-26 (session 40)

### Refactor: Painel de ataques substituído por painel de crafting no party menu
**Files:** `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

`attacksPanel` → `craftingPanel`, `attacksNavText/Bg` → `craftingNavText/Bg`, `ShowAttacks()` → `ShowCrafting()`. Removidos `UpdateAttacksDisplay()`, `attackListParent`, `attackDisplayPrefab` e a chamada em `OnPartyMemberSelected`. Novos campos: `craftingListParent` e `craftingSlotPrefab` para uso futuro. Botão de nav deve chamar `ShowCrafting()`.

---

## 2026-03-26 (session 39)

### Feature: Navegação do party menu com botões de texto coloridos
**Files:** `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

Adicionados campos `menuContainer` (container compartilhado de subpaneis), `characterPanel` (painel padrão grupo/personagem) e quatro referências `TextMeshProUGUI` para os botões de nav (characterNavText, attacksNavText, itemsNavText, equipmentNavText). `SelectNav()` muda o botão atual para branco e o anterior para cinza. Cada `Show*` retorna cedo se já estiver selecionado. `OpenMenu` ativa o menuContainer e chama `ShowCharacter()` como padrão. `CloseMenu` desativa o menuContainer e reseta o nav.

**Inspector:** Atribuir os quatro campos de texto dos botões de nav e o menuContainer/characterPanel.

---

## 2026-03-26 (session 38)

### Fix: Clique no retrato inimigo/aliado não registrava no ícone central
**Files:** `Assets/Scripts/Battle/EnemyDisplay.cs`, `Assets/Scripts/Battle/PartyMemberDisplay.cs`

Adicionado Image transparente (Color.clear, raycastTarget=true) no root de EnemyUI e CharacterUI em Initialize(). Garante que todo o rect do card captura cliques, mesmo sobre filhos com Raycast Target desativado, propagando o evento até o IPointerClickHandler do root.

---

## 2026-03-26 (session 37)

### Feature: Botão de abrir menu some via CanvasGroup ao abrir o menu
**Files:** `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

Adicionado campo `menuOpenerCanvasGroup` (CanvasGroup) no header "HUD Buttons". `OpenMenu` agora oculta o botão via CanvasGroup (alpha=0) em vez de SetActive, mantendo-o no layout. `CloseMenu` restaura (alpha=1). `HideAllPanels` não toca mais no MenuOpener — ele nunca sai do layout. Remova o `MenuOpener.SetActive` de qualquer chamada manual restante.

**Inspector:** Adicionar CanvasGroup no GameObject do botão de abrir menu e atribuir ao campo `Menu Opener Canvas Group`.

---

## 2026-03-26 (session 36)

### Feature: Botão de configurações vira toggle com save ao fechar
**Files:** `Assets/Scripts/ConfigScene.cs`, `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

Adicionada propriedade pública `IsConfigLoaded` em `ConfigSceneManager`. `LoadConfig` e `DeleteConfig` em PartyMenuManager substituídos por `ToggleSettings()`: se a cena de config estiver carregada, salva o jogo e descarrega; se não estiver, carrega. Redirecione o botão de settings para chamar `ToggleSettings()`.

---

## 2026-03-26 (session 35)

### Feature: Botão de objetivos se oculta quando painel abre
**Files:** `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

Adicionado campo `objectivesButtonCanvasGroup` (CanvasGroup) no header "HUD Buttons". `ToggleObjectives` agora oculta o botão de objetivos via CanvasGroup (alpha=0, interactable=false, blocksRaycasts=false) quando o painel abre, e restaura quando fecha. Não usa `SetActive` para não quebrar o Grid Layout. `CloseObjectivesPanel` também restaura o botão. O botão dentro do painel pode chamar `ToggleObjectives` diretamente — mesma lógica.

**Inspector:** Adicionar componente CanvasGroup no GameObject do botão de objetivos e atribuir ao campo `Objectives Button Canvas Group`.

---

## 2026-03-26 (session 34)

### Fix: Ouro desativado separadamente durante combate
**Files:** `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

Adicionado campo `goldContainer` (GameObject) no header "Gold Count". Em `SetBattleState(true)`, desativa `goldContainer` separadamente de `hudButtonsContainer` para evitar bug visual onde o texto de ouro aparecia distorcido quando estava dentro do container de HUD. Atribua o GameObject pai do texto de ouro no Inspector.

---

## 2026-03-26 (session 33)

### Feature: Barra de AP compartilhada no combat UI
**Files:** `Assets/Scripts/Battle/CombatSystem.cs`, `Assets/Scripts/Battle/CombatUIManager.cs`

Adicionado campo `sharedApBar` (Image) + `sharedApText` (TMP) no CombatUIManager (Header "AP Bar"). A barra mostra o AP do personagem ativo na vez do jogador e se atualiza a cada mudança de personagem ou gasto de AP. Zerada/apagada durante a vez do inimigo.

### Feature: Retratos Default/Selected para personagens e inimigos
**Files:** `Assets/Scripts/Battle/PartyMemberDisplay.cs`, `Assets/Scripts/Battle/EnemyDisplay.cs`, `Assets/Scripts/Battle/CombatSystem.cs`, `Assets/Scripts/Battle/CombatUIManager.cs`

Adicionados campos `defaultContainer` e `selectedContainer` (GameObject) em CharacterUI e EnemyUI. `SetSelected(bool)` troca entre os dois. Personagens do jogador: ativam Selected no início de sua vez, voltam para Default quando outra vez começa. Inimigos: Selected só fica ativo enquanto o inimigo está executando um ataque (`onAttackStarted`/`onAttackFinished` novos eventos no CombatSystem). HP reflete em ambos os estados via `selectedHpText`/`selectedHealthBar`.

### Feature: Retratos clicáveis para targeting + outline de hover
**Files:** `Assets/Scripts/Battle/PartyMemberDisplay.cs`, `Assets/Scripts/Battle/EnemyDisplay.cs`

Removido sistema de botão separado para selecionar alvo. CharacterUI e EnemyUI implementam `IPointerClickHandler`, `IPointerEnterHandler`, `IPointerExitHandler`. Quando um retrato se torna alvo válido: background sobe para 100% alpha (de 75%). Ao passar o mouse: `targetableOutline` aparece. Ao clicar: seleciona o alvo. Campo `portraitBackground` (Image) controla o alpha; campo `targetableOutline` (GameObject) controlado por hover.

---

## 2026-03-26 (session 32)

### Feature: Fila de ataques exibida em texto durante o combate
**Files:** `Assets/Scripts/Battle/CombatSystem.cs`

Adicionado campo `attackQueueText` (TextMeshProUGUI) no inspector (Header "UI"). Mostra as ações enfileiradas no formato `Ataque > Ataque > Ataque`. Atualiza ao adicionar ação (`SelectPlayerAction`), desfazer (`UndoLastAction`) e ao iniciar turno (limpa). Durante a execução (`ExecuteActionQueue`), cada ação é removida do texto no momento em que começa a ser executada, usando `GetRange(i, remaining)` para mostrar apenas o que ainda falta.

---

## 2026-03-25 (session 31)

### Fix: Componente Camera desabilitado após luta dos ratos
**Files:** `Assets/Scripts/EncounterS/PreviousScene.cs`

`LoadScene()` usava `FindGameObjectsWithTag("Ignore")` para reativar câmeras, mas essa busca percorre todas as cenas carregadas — no momento da chamada, a cena de Combate ainda está carregada junto com a de exploração. Isso podia encontrar câmeras erradas ou perder a câmera certa. Fix: câmeras desabilitadas em `UnloadScene()` agora são salvas em `disabledIgnoreCameras` (lista de referências explícitas). `LoadScene()` restaura exatamente essas referências, sem depender de tag search cross-scene.

---

## 2026-03-25 (session 30)

### Fix: Formação diagonal no combate + sorting order dos personagens
**Files:** `Assets/Scripts/Battle/CombatSystem.cs`

Personagens de combate sempre usavam posições viewport ignorando os `partySpawnPoints`/`enemySpawnPoints` do inspector. Corrigido: usa os transforms do inspector quando atribuídos, fallback para diagonal viewport (índice 0 = topo/fundo, índice 2 = frente/câmera). `ApplyDepthOrder()` define `sortingOrder = índice` em todos os `SpriteRenderer` do prefab — índice 0 atrás, índice maior na frente. Mesma lógica para inimigos (espelhada).

### Fix: Música de ambiente não tocando após combate
**Files:** `Assets/Scripts/MusicManager.cs`, `Assets/Scripts/Battle/CombatSystem.cs`

`ReturnToMapAfterDelay` chamava `StopMusic()` ao sair do combate mas nada retomava a música. Adicionado `lastAmbienceTrackName` (atualizado a cada `PlayMusicCommand`) e `ResumeAmbience()` ao `MusicManager`. `ReturnToMapAfterDelay` agora chama `ResumeAmbience()` após restaurar a cena. Adicionado `spatialBlend = 0f` ao AudioSource do MusicManager.

---

## 2026-03-25 (session 29)

### Fix: Camera apagada ao retornar do combate
**Files:** `Assets/Scripts/EncounterS/PreviousScene.cs`

A câmera da exploração (tag "Ignore") era reativada antes dos objetos em sceneObjects, então qualquer `OnEnable` disparado durante a reativação poderia desabilitá-la novamente. Movida a re-habilitação da câmera para o final de `LoadScene()`, após todos os objetos serem reativados, FixAudioListeners e FixEventSystems. Também removido o `ReactivateOriginalPlayer(Vector3.zero)` que teletransportava o jogador para a origem.

### Fix: Quests não sendo concluídas
**Files:** `Assets/Scripts/SistemaInventario.cs`

`AddProgress()` nunca notificava o `ObjectiveManager`. Adicionado `ObjectiveManager.Instance?.CheckAllObjectives()` ao final de `AddProgress()` — agora quests são marcadas como concluídas automaticamente quando as tags de progresso necessárias são adicionadas.

### Fix: Tremido dos seguidores (Simon/Joodie)
**Files:** `Assets/Scripts/LeaderFollower.cs`

A animação `Andando` era controlada por `horizontalDistance > stoppingDistance`, causando flickering quando a distância oscilava na fronteira. Corrigido para usar `velocity.magnitude > 0.15f` como condição de movimento. Deceleração trocada de `Vector3.Lerp` para `Vector3.MoveTowards` com fator maior (8×speed) para parada mais limpa.
**Inspector:** Para Joodie parecer mais afastada, aumente `Stopping Distance` no componente `FollowPlayer` dela para ~3.5 (Simon fica em ~2.0).

### Refactor: Save/Load migrado de BinaryFormatter para JSON
**Files:** `Assets/Scripts/Dialogue Scripts/SaveLoadManager.cs`, `Assets/Scripts/SaveHandler.cs`, `Assets/Scripts/Menu Scripts/PartyMenuManager.cs`

`BinaryFormatter` estava desabilitado por padrão no Unity moderno, causando erros ao salvar/carregar. Substituído por `JsonUtility` — salva um arquivo `savegame.json` legível em `Application.persistentDataPath`. Serializa: cena atual, ouro, progresso (tags), inventário (por ID de item) e membros do grupo (nome, nível, HP, AP, EXP, armas/armaduras por ID).
**Requisito:** `DadosItem` e `CharacterData` precisam estar dentro de uma pasta `Resources` para que `Resources.LoadAll` os encontre ao carregar.

---

## 2026-03-25 (session 28)

### Fix: Game Over sobreposto ao combate (em vez de tela em branco)
**Files:** `Assets/Scripts/Battle/CombatSystem.cs`, `Assets/Scripts/Battle/GameOver.cs`

Antes: Combat era descarregado antes de carregar GameOver — a tela de derrota aparecia sobre a cena de exploração vazia. Agora: Combat permanece carregado e GameOver carrega de forma aditiva por cima. No retry, GameOver e o Combat antigo são descarregados antes de recarregar o combate.

---

## 2026-03-25 (session 27)

### Fix: Botão Retry reiniciava combate com inimigos mortos
**Files:** `Assets/Scripts/EncounterS/EncounterData.cs`, `Assets/Scripts/Battle/GameOver.cs`

Ao tentar novamente após derrota, `EncounterData.enemyPartyMembers` ainda tinha os inimigos com HP = 0 da batalha anterior. `CombatSystem.InitializeCombat()` filtra membros com HP ≤ 0, então o combate terminava imediatamente sem inimigos. Adicionado `EncounterData.ResetEnemiesForRetry()` que reconstrói a lista de inimigos a partir do `EncounterFile` (igual ao setup original) e zera `combatVictory`. `GameOver.OnRetry()` agora chama esse método antes de recarregar a cena de combate. O snapshot de HP/AP do grupo (já existente em `BattleSaveManager`) é descartado na vitória via `ClearSnapshot()`.

---

## 2026-03-25 (session 26)

### Remoção: MenuWaveEffect e shader MenuWave
**Files removidos:** `Assets/Scripts/Menu Scripts/MenuWaveEffect.cs`, `Assets/Shaders/MenuWave.shader` (e seus `.meta`)

Sistema de wave effect do menu removido completamente. O arquivo de cena `TitleScreen.unity` ainda referencia o componente — abrir a cena no editor mostrará "Missing Script"; remover manualmente o componente e qualquer material que use o shader MenuWave.

---

## 2026-03-25 (session 25)

### Fix: BattleTransitionConfig nunca atribuído ao BattleTransitionManager
**Files:** `Assets/Scripts/Battle/BattleTransitionManager.cs`, `Assets/Scripts/EncounterS/EncounterStarter.cs`

`BattleTransitionManager` é criado em runtime via `GetOrCreate()`, então seu campo `[SerializeField]` fica sempre null e `Resources.Load` falha porque o asset não está em uma pasta `Resources`. Solução: (1) campo `transitionConfig` agora é `public` (visível no inspector se o objeto estiver na cena); (2) novo método `Configure(BattleTransitionConfig)` define o config e regenera os gradientes; (3) `EncounterStarter` ganhou campo `public BattleTransitionConfig transitionConfig` e chama `btm.Configure(transitionConfig)` antes de cada transição — basta atribuir o asset no inspector do EncounterStarter.

---

## 2026-03-25 (session 24)

### Fix: Som de ataque da batalha não tocava
**File:** `Assets/Scripts/Battle/Attacks/BattleAnimations.cs`

`PlayAnimation()` usava `AudioSource.PlayClipAtPoint(hitSound, targetTransform.position)` para reproduzir o som de acerto — isso cria uma AudioSource 3D temporária na posição do alvo. Com o AudioListener no SFXManager (posição de mundo ~0,0,0) e os personagens de batalha a 5-7 unidades de distância, o volume caia para ~15% pelo rolloff logarítmico, tornando o som praticamente inaudível. Corrigido: agora usa `SFXManager.Instance.Play(hitSound)` (2D, sem atenuação por distância).

---

## 2026-03-25 (session 23)

### Fix: SFX de combate não tocava
**Files:** `Assets/Scripts/EncounterS/PreviousScene.cs`, `Assets/Scripts/SFXManager.cs`

Dois problemas combinados: (1) `PreviousScene.UnloadScene()` desabilitava o componente `Camera` de objetos "Ignore", mas deixava o `AudioListener` ativo — durante o combate havia dois AudioListeners ativos e o Unity escolhia um imprevisível, muitas vezes o da câmera de exploração distante do SFXManager. Corrigido: agora desabilita também o `AudioListener` em objetos "Ignore". (2) As AudioSources do SFXManager tinham `spatialBlend = 1` (3D padrão), fazendo o volume depender da distância ao AudioListener. Corrigido: `spatialBlend = 0` (2D) garante volume total independente de posição.

---

## 2026-03-25 (session 22)

### Fix: Câmera de exploração permanecia ativa durante combate
**File:** `Assets/Scripts/EncounterS/PreviousScene.cs`

A câmera de exploração estava marcada com a tag "Ignore", fazendo com que `PreviousScene.UnloadScene()` a ignorasse completamente e nunca desabilitasse. Com duas câmeras ativas simultaneamente, o background não carregava (FitToCamera pegava a câmera errada) e os personagens pareciam ter escala errada. Corrigido: `UnloadScene()` agora desabilita o componente `Camera` em objetos "Ignore", e `LoadScene()` o reabilita ao restaurar a cena.

---

## 2026-03-25 (session 21)

### Fix: Personagens da batalha do rato aparecendo fora da câmera
**File:** `Assets/Scripts/Battle/CombatSystem.cs`

O código ignorava os spawn points configurados no inspector e usava sempre o cálculo por viewport. Corrigido: agora usa `partySpawnPoints[i].position` / `enemySpawnPoints[i].position` quando o slot está atribuído no inspector, caindo para o cálculo por viewport apenas quando o slot está vazio. Basta posicionar os GameObjects de spawn point dentro da área visível da câmera de combate.

### Fix: Diálogo `pos_batalha_nepal` nunca disparava após batalha do rato
**File:** `Assets/Scripts/Dialogue Scripts/prologue_parte2.yarn`

Adicionado `<<progress rat_fight_completed>>` no nó `rat_fight` antes de `<<startratfight>>`. Ao restaurar a cena após a batalha, esse flag já está em progresso e o ProgressCheck pode verificá-lo. Adicionado `<<progress pos_batalha_nepal_done>>` no início de `pos_batalha_nepal` para evitar replay. **Ação necessária no inspector:** no ProgressCheck de `pos_batalha_nepal`, defina condição `"rat_fight_completed"` com `conditionMeansItDoesNotLoad = false` (obrigatório) e condição `"pos_batalha_nepal_done"` com `conditionMeansItDoesNotLoad = true` (evitar repetição).

### Fix: Vazamento de comando Yarn `addsimon` no OnDestroy
**File:** `Assets/Scripts/Dialogue Scripts/SpecialCutscene.cs`

`OnDestroy()` removia todos os handlers registrados exceto `addsimon`. Corrigido: adicionado `RemoveCommandHandler("addsimon")`.

### Novo: Música de fundo com zonas e trilha de início
**Files:** `Assets/Scripts/MusicManager.cs`, `Assets/Scripts/ZoneMusicTrigger.cs` (novo)

`MusicManager`: adicionado campo `startupMusicName` — preencha com o nome de uma trilha cadastrada em `musicTracks` para que a música inicie automaticamente quando o jogo carrega. `ZoneMusicTrigger`: novo script para transição de música por zona. Adicione a um GameObject com `BoxCollider2D` (Is Trigger), preencha `musicaAoEntrar` (e opcionalmente `musicaAoSair`) com nomes de trilhas de `MusicManager.musicTracks`. Configure duas zonas para alternar entre música interna e externa da embarcação.

---

## 2026-03-25 (session 20)

### Redesign: Efeito de onda PixelWave — grade de pixels com escalonamento por coluna
**Files:** `Assets/Shaders/MenuWave.shader`, `Assets/Scripts/Menu Scripts/MenuWaveEffect.cs`

Shader e script completamente reescritos com algoritmo inspirado em vin-ni/PixelWave (MIT). O shader quantiza a tela em blocos de pixel (`floor(uv / _PixelSize)`), aplica um hash estável por coluna como limiar de escalonamento (equivalente ao Fisher-Yates do PixelWave), e divide cada banda em três zonas: lacuna (invisível), frente escalonada (colunas surgem progressivamente) e corpo cheio. Espuma aparece nas colunas que surgem mais cedo na frente. Gradiente de profundidade: #8BC6F6 → #3F75BA → #3468A9 → #10204A → #080C1B. Properties novas: `_ScrollSpeed`, `_WaveSpeed`, `_PixelSize`, `_NumBands`, `_GapRatio`, `_FrontWidth`. `MenuWaveEffect.cs` atualizado para expor e enviar as novas properties ao shader (removidas as antigas: `_Frequency`, `_Amplitude`, `_WaterRatio`).

---

## 2026-03-25 (session 19)

### Fix: Batalha do rato não carregava personagens
**File:** `Assets/Scripts/Dialogue Scripts/DynamicMovementDialogue.cs`

`<<startratfight>>` era um `[YarnCommand]` em método **estático** `IEnumerator`. O Yarn Spinner não consegue determinar qual MonoBehaviour deve hospedar a coroutine estática, fazendo com que o encontro não fosse iniciado corretamente. **Corrigido:** removido o atributo e o método estático; adicionado `dialogueRunner.AddCommandHandler("startratfight", StartRatFightCommand)` no `Start()` e método de instância `StartRatFightCommand()` — padrão idêntico ao `SpecialCutsceneScript` (batalha do Simon), que funciona corretamente. O arquivo `.yarn` e o nome do comando não mudam.

---

## 2026-03-24 (session 18)

### Redesign: Ondas em mosaico com rolagem vertical
**Files:** `Assets/Shaders/MenuWave.shader`, `Assets/Scripts/Menu Scripts/MenuWaveEffect.cs`

Shader reescrito com UV em mosaico (`frac(y * numBands - t * scrollSpeed)`): bandas de onda sobem continuamente, aparecem na base e somem no topo. Cada banda tem fase única via hash do índice, gradiente de profundidade (#8BC6F6 espuma → #080C1B abismo) e lacuna transparente entre ondas. Script expõe scrollSpeed, waveSpeed, frequency, amplitude, numBands e waterRatio. Rotacionar o GameObject -90° no eixo Z faz as ondas viajarem para a direita.

---

## 2026-03-24 (session 17)

### Redesign: Efeito de onda do menu — 3 linhas viajantes com paleta de cor
**Files:** `Assets/Shaders/MenuWave.shader`, `Assets/Scripts/Menu Scripts/MenuWaveEffect.cs`

Shader reescrito: substitui o preenchimento de água por 3 linhas finas animadas que aparecem pela esquerda, atingem opacidade máxima no centro e somem à direita (envelope horizontal `smoothstep`). Cada linha tem núcleo nítido + brilho suave. Cores fixas da paleta fornecida: espuma #8BC6F6, transição #3F75BA, azul médio #3468A9. `MenuWaveEffect.cs` expõe posições Y individuais das 3 ondas, velocidade, frequência e amplitude no inspector.

---

## 2026-03-24 (session 16)

### Feature: Frases motivacionais no Game Over
**File:** `Assets/Scripts/Battle/GameOver.cs`

Adicionado campo `quoteText` (TMP_Text) e array de 15 frases motivacionais em português com temática pirata/aventura. Em `Start()`, uma frase aleatória é exibida. Adicione um TextMeshPro à cena GameOver e arraste-o no campo **Quote Text** do inspector.

---

## 2026-03-24 (session 15)

### Fix: SFX duplo no cancelar mira + SFX faltando nos botões de ataque
**File:** `Assets/Scripts/Battle/CombatUIManager.cs`

**SFX duplo ao cancelar** — `CancelTargeting()` restaurava o painel chamando `OnAttacksSelected()`/`OnItemsSelected()`, que tocam UIForward. Resultado: cancelar tocava UIBackward (correto) + UIForward (errado). Extraída a lógica de painel em `ShowAttackGrid()` e `ShowItemGrid()` (sem SFX); `OnAttacksSelected`/`OnItemsSelected` chamam essas helpers + tocam UIForward; `CancelTargeting` chama as helpers diretamente.

**SFX faltando nos botões de ataque/item** — `OnAttackSelected` e `OnItemSelected` (clique em um ataque/item específico da grade) não tocavam nenhum SFX. Adicionado UIForward em ambos.

---

## 2026-03-24 (session 14)

### Fix: TitleScreenCleanup destruía singletons únicos (SFXManager, MusicManager)
**File:** `Assets/Scripts/Dialogue Scripts/TitleScreenCleanup.cs`

A limpeza destruía qualquer objeto DontDestroyOnLoad fora da cena ativa, inclusive gerenciadores únicos. Dois problemas: (1) objetos que se auto-destroem no `Awake` (singletons duplicados) ainda aparecem para `FindObjectsByType` no mesmo frame; (2) singletons únicos eram destruídos sem necessidade. **Correções:** `Start()` agora dispara a limpeza via coroutine com `yield return null` (espera um frame para que os `Destroy()` diferidos sejam processados), e `CleanupNonTitleScreenObjects()` conta as ocorrências de cada nome — objetos únicos são preservados, apenas duplicatas são destruídas.

---

## 2026-03-24 (session 13)

### Fix: TitleScreenCleanup destruía SFXManager e MusicManager
**File:** `Assets/Scripts/Dialogue Scripts/TitleScreenCleanup.cs`

`CleanupNonTitleScreenObjects()` destruía todos os objetos DontDestroyOnLoad que não fossem tagged "TitleScreen" ou nomeados "[Debug Updater]", incluindo os gerenciadores de áudio. Adicionadas verificações de componente para `SFXManager` e `MusicManager` para que sejam preservados durante a limpeza da tela de título.

---

## 2026-03-24 (session 12)

### Feature: Onda oceânica procedural no menu principal
**Files:** `Assets/Shaders/MenuWave.shader` (novo), `Assets/Scripts/Menu Scripts/MenuWaveEffect.cs` (novo)

Shader `Custom/MenuWave` gera duas camadas de ondas senoidais animadas (onda frontal + traseira) com espuma na crista e gradiente de profundidade. A onda flui da esquerda para a direita. `MenuWaveEffect` cria um Canvas ScreenSpaceOverlay e um RawImage cobrindo a tela inteira, aplicando o shader. Adicione `MenuWaveEffect` a qualquer GameObject na cena do título e ajuste no inspector: `waveY` (posição vertical da crista), `sortingOrder` (deve ser menor que o Canvas dos botões), cor, velocidade e amplitude.

---

## 2026-03-24 (session 11)

### Fix: Permanent AudioListener on SFXManager
**File:** `Assets/Scripts/SFXManager.cs`

Added `gameObject.AddComponent<AudioListener>()` in `Awake` so the SFXManager (DontDestroyOnLoad) always carries the single active listener. This prevents audio muting during scene transitions when the player object is deactivated by PreviousScene. Remove AudioListener from the player and any cameras that previously held one.

---

## 2026-03-24 (session 10)

### Fix: BattleTransitionConfig not found + SFX inspector field
**File:** `Assets/Scripts/Battle/BattleTransitionManager.cs`

`Resources.Load` requires the asset to live inside an `Assets/Resources/` folder; the config was at `Assets/RPG/Battle Transitions/`. Added `[SerializeField] private BattleTransitionConfig transitionConfig;` inspector field to `BattleTransitionManager`. `GenerateAllGradients()` now uses the inspector-assigned value first, with `Resources.Load` as a fallback. Place `BattleTransitionManager` in your starting scene and drag the config asset onto the field to resolve the warning permanently.

---

## 2026-03-24 (session 9)

### Fix: Music not playing + camera double-log + debug logs for all audio
**Files:** `Assets/Scripts/EncounterS/EncounterStarter.cs`, `Assets/Scripts/Battle/CombatSystem.cs`, `Assets/Scripts/MusicManager.cs`, `Assets/Scripts/SFXManager.cs`

**Music not playing** — Session 8 had moved music to `InitializeCombatWithData()` only, but that method's `StopMusic()` call was stopping whatever the callbacks played. Reverted architecture: music is started inside the `StartTransitionThen` callback in both `StartEncounter()` and `StartEncounterFromCutscene()` (fires when screen is fully black). `InitializeCombatWithData()` keeps only `PlayClip()` as a no-op fallback; its `StopMusic()` was removed so it can't cut the music started by the callback.

**Camera double-log** — `GetCombatCamera()` fallback now emits both `Debug.Log` (informational path) and `Debug.LogWarning` (camera-not-found path) so both appear in the console regardless of filter level.

**Debug logs for all audio** — Added `Debug.Log` to every audio start/stop path: `MusicManager.PlayClip` (null case, already-playing case, success case), `MusicManager.StopMusic`, `SFXManager.Play`, `SFXManager.PlayLoop`, `SFXManager.StopLoop`. All messages in PT-BR.

---

## 2026-03-24 (session 8)

### Fix: Battle music playing during forward transition then cutting out
**File:** `Assets/Scripts/EncounterS/EncounterStarter.cs`

`StartEncounter()` and `StartEncounterFromCutscene()` both called `StopMusic()` + `PlayClip()` immediately before starting the forward transition. This caused music to play while the screen was still visible (during the 0.7s fade-to-black), then `InitializeCombatWithData()` would stop and restart it a second time — audible as a cut.

**Removed** music calls from both methods. Music is now started exclusively in `CombatSystem.InitializeCombatWithData()`, which runs after the screen is fully black and immediately before `PlayReverseTransition()`. Result: silence during the forward transition, battle music begins under the backward transition as the combat scene is revealed.

---

## 2026-03-24 (session 7)

### Fix: Camera warning spam + transition frame skipping + music revert
**Files:** `Assets/Scripts/Battle/CombatSystem.cs`, `Assets/Shaders/BattleTransition.shader`

**Camera warning spam** — `GetCombatCamera()` was called 3+ separate times inside `InitializeCombatWithData()` (background fit, each party spawn, each enemy spawn), printing the warning once per call. **Resolved** `cam` once at the top of the method and passed it through — warning fires at most once per combat start. Assign `combatCamera` in the CombatSystem inspector to eliminate the warning entirely.

**Transition frame skipping** — The shader used `step(grad, _Cutoff)` which is binary: each pixel is either fully black or fully transparent, with no blending in between. As `_Cutoff` advances per frame, large bands of pixels flip at once, producing a visible "jump". **Replaced** with `smoothstep(_Cutoff - 0.03, _Cutoff + 0.03, grad)` to create a 6% feathered edge at the sweep boundary — pixels near the threshold fade gradually instead of snapping.

**Music revert** — The main branch merge had removed `StopMusic()` before `PlayClip()` in `InitializeCombatWithData`. Restored the `StopMusic()` call so the previous clip is always cleared before the battle music starts.

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

### Feature: Game Over screen (additive) with Retry and Quit
**Files:** `Assets/Scripts/Battle/GameOver.cs` *(new)*, `Assets/Scripts/Battle/BattleSaveManager.cs` *(new)*, `Assets/Scripts/EncounterS/EncounterStarter.cs`, `Assets/Scripts/Battle/CombatSystem.cs`

On defeat the game previously went straight to the main menu. Now it loads a "GameOver" scene additively, showing a black overlay with "GAME OVER" text and two buttons.

**BattleSaveManager** (DontDestroyOnLoad, created automatically):
- `SaveSnapshot(partyMembers)` — called by `EncounterStarter.BuildEncounterData()` before every fight; stores each member's HP and AP.
- `RestoreSnapshot(partyMembers)` — called by Retry; writes saved HP/AP back onto the live `PartyMemberState` objects (same references used by SistemaInventario).
- `ClearSnapshot()` — called on victory so the data is not reused for a different fight.

**GameOver script** (attach to parent object in GameOver scene):
- Retry: restores HP/AP via `BattleSaveManager`, unloads "GameOver", reloads "Combat" additively. The deactivated exploration scene remains untouched.
- Quit: clears snapshot, stops music, loads Menu in Single mode (clears everything).

**CombatSystem changes:**
- Added `gameOverSceneName = "GameOver"` inspector field.
- Defeat path: unloads "Combat", loads "GameOver" additively (instead of loading Menu).
- Victory path: calls `BattleSaveManager.Instance?.ClearSnapshot()` before restoring exploration.

**Setup required in Unity:**
1. Create a "GameOver" scene with: full-screen black Image, TMP "GAME OVER" text, Retry button, Quit button, EventSystem.
2. Attach `GameOver.cs` to a parent GameObject; wire Retry/Quit buttons in inspector.
3. Add the scene to Build Settings.
4. Set `gameOverSceneName` on the CombatSystem inspector if the scene is not named "GameOver".

---

## 2026-03-24 (session 4)

### Feature: CrashingWaves added as 8th battle transition type
**Files:** `Assets/Scripts/Battle/BattleTransitionManager.cs`, `Assets/Scripts/Battle/BattleTransitionConfig.cs`

User initially requested replacing Gooey with CrashingWaves, then revised to keep both.

**Added** `CrashingWaves = 7` to `BattleTransitionType` enum. **Added** procedural fallback in `GradientValue()` — two sinusoidal wave fronts (fromTop, fromBottom) collide at the horizontal center. **Added** `public Texture2D crashingWaves` field to `BattleTransitionConfig` and its corresponding `case` in `GetTexture()`. Gooey (procedural Voronoi) was retained at index 5; CrashingWaves is index 7.

**Inspector:** Assign `crashingWaves` texture in BattleTransitionConfig asset; leave `gooey` empty to keep procedural fallback.

---

### Fix: Simon battle music not playing — music now started at encounter trigger time
**Files:** `Assets/Scripts/EncounterS/EncounterStarter.cs`, `Assets/Scripts/Dialogue Scripts/DynamicMovementDialogue.cs`, `Assets/Scripts/Dialogue Scripts/SpecialCutscene.cs`, `Assets/Scripts/Battle/CombatSystem.cs`

Music was being started only inside `CombatSystem.InitializeCombatWithData()` (i.e., inside the Combat scene's `Start()`). For the Simon fight, the `<<startencounter>>` Yarn command is a void handler that doesn't block Yarn, so the dialogue completes and the scene loads asynchronously — by then, any timing-related AudioSource/AudioListener issues during the scene transition could silently swallow the `PlayClip` call.

**Added** `EncounterStarter.StartEncounterFromCutscene(EncounterFile, SistemaInventario)` — a shared static method that calls `BuildEncounterData`, plays battle music immediately (`StopMusic` + `PlayClip`), then starts the transition and loads Combat. Both cutscene scripts now delegate to this instead of duplicating the PreviousScene/SceneManager logic.

- `SpecialCutsceneScript.StartEncounterCoroutine()` now calls `StartEncounterFromCutscene` (void command approach kept, as it is the more correct approach)
- `DynamicCutsceneScript.StartRatEncounter()` now calls `StartEncounterFromCutscene` and no longer blocks Yarn with `WaitUntil`
- `CombatSystem.InitializeCombatWithData()` no longer calls `StopMusic()` before `PlayClip` — music is already playing from the trigger; `PlayClip`'s existing guard (`same clip + isPlaying → skip`) prevents double-play

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
