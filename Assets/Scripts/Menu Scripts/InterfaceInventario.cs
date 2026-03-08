using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class InterfaceInventario : MonoBehaviour
{
    public SistemaInventario sistemaInventario;
    public Transform containerGrid;
    public GameObject prefabSlot;

    [Header("Economia")]
    public TextMeshProUGUI textoMoedas;

    [Header("Selection")]
    public Color normalColor = Color.white;
    public Color highlightedColor = Color.yellow;
    private SlotUI currentlyHighlightedSlot;

    private List<SlotUI> allSlots = new List<SlotUI>();

    private void Start()
    {
        // Subscribe to inventory change events
        if (sistemaInventario != null)
        {
            sistemaInventario.onInventarioMudou += AtualizarInterface;
        }

        // Update inventory at start
        AtualizarInterface();
    }

    public void AtualizarInterface()
    {
        // 1. Update coins
        if (textoMoedas != null)
        {
            textoMoedas.text = "Ouro: " + sistemaInventario.moedas.ToString();
        }

        // 2. Clear the grid
        foreach (Transform item in containerGrid)
        {
            Destroy(item.gameObject);
        }
        allSlots.Clear();

        // 3. Build the inventory
        foreach (SlotInventario slot in sistemaInventario.inventario)
        {
            GameObject novoSlot = Instantiate(prefabSlot, containerGrid);
            SlotUI slotUI = novoSlot.GetComponent<SlotUI>();
            slotUI.ConfigurarSlot(slot);
            allSlots.Add(slotUI);
        }

        // Clear highlight
        currentlyHighlightedSlot = null;
    }

    public void HighlightSlot(SlotUI selectedSlot)
    {
        // Unhighlight previous slot
        if (currentlyHighlightedSlot != null)
        {
            currentlyHighlightedSlot.SetHighlight(false);
        }

        // Highlight new slot
        currentlyHighlightedSlot = selectedSlot;
        if (currentlyHighlightedSlot != null)
        {
            currentlyHighlightedSlot.SetHighlight(true);
        }
    }

    private void OnDestroy()
    {
        if (sistemaInventario != null)
        {
            sistemaInventario.onInventarioMudou -= AtualizarInterface;
        }
    }
}