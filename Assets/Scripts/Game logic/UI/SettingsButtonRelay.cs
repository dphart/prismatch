using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SettingsButtonRelay : MonoBehaviour, IPointerClickHandler
{
    private GameController controller;

    public void Configure(GameController gameController) => controller = gameController;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            controller?.ToggleSettings();
    }
}
