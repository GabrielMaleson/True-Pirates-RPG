using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Salva um snapshot do estado do grupo no início de cada batalha.
/// Em caso de derrota, permite restaurar o estado para tentar novamente.
/// Em caso de vitória, o snapshot é descartado automaticamente.
///
/// Setup: Não precisa estar em cena — criado automaticamente via GetOrCreate().
/// </summary>
public class BattleSaveManager : MonoBehaviour
{
    public static BattleSaveManager Instance { get; private set; }

    [System.Serializable]
    private class PartyMemberSnapshot
    {
        public string characterName;
        public int hp;
        public int ap;
    }

    private List<PartyMemberSnapshot> savedParty;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static BattleSaveManager GetOrCreate()
    {
        if (Instance != null) return Instance;
        GameObject go = new GameObject("BattleSaveManager");
        return go.AddComponent<BattleSaveManager>();
    }

    /// <summary>
    /// Salva HP e AP de cada membro do grupo antes da batalha começar.
    /// Chamado automaticamente por EncounterStarter.BuildEncounterData().
    /// </summary>
    public void SaveSnapshot(List<PartyMemberState> partyMembers)
    {
        savedParty = new List<PartyMemberSnapshot>();
        foreach (var member in partyMembers)
        {
            savedParty.Add(new PartyMemberSnapshot
            {
                characterName = member.CharacterName,
                hp = member.currentHP,
                ap = member.currentAP
            });
        }
        Debug.Log($"[BattleSaveManager] Snapshot salvo — {savedParty.Count} membro(s).");
    }

    /// <summary>
    /// Restaura HP e AP a partir do snapshot salvo antes da batalha.
    /// Chamado pelo botão de Tentar Novamente na tela de Game Over.
    /// </summary>
    public void RestoreSnapshot(List<PartyMemberState> partyMembers)
    {
        if (savedParty == null)
        {
            Debug.LogWarning("[BattleSaveManager] Nenhum snapshot para restaurar.");
            return;
        }

        foreach (var member in partyMembers)
        {
            PartyMemberSnapshot snap = savedParty.Find(s => s.characterName == member.CharacterName);
            if (snap != null)
            {
                member.currentHP = snap.hp;
                member.currentAP = snap.ap;
            }
        }
        Debug.Log("[BattleSaveManager] Estado do grupo restaurado para tentativa anterior.");
    }

    /// <summary>
    /// Descarta o snapshot após uma vitória — não deve ser reaproveitado para outra batalha.
    /// </summary>
    public void ClearSnapshot()
    {
        savedParty = null;
        Debug.Log("[BattleSaveManager] Snapshot descartado após vitória.");
    }

    public bool HasSnapshot => savedParty != null;
}
