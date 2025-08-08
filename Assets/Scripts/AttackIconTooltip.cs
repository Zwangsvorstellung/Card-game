using UnityEngine;
using UnityEngine.EventSystems;

public class AttackIconTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string attackName;
    [SerializeField] private Vector3 tooltipOffset = new Vector3(0, 0, 0);


    public void OnPointerEnter(PointerEventData eventData)
    {
        RectTransform rectTransform = transform as RectTransform;
        Vector3 iconScreenPos = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
        TooltipManager.Instance.Show(attackName, iconScreenPos + tooltipOffset);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.Hide();
    }
} 