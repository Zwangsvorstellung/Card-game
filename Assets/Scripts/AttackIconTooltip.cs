using UnityEngine;
using UnityEngine.EventSystems;

public class AttackIconTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string attackName;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector3 iconScreenPos = RectTransformUtility.WorldToScreenPoint(null, ((RectTransform)transform).position);
        TooltipManager.Instance.Show(attackName, iconScreenPos + new Vector3(0, 0, 0));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.Hide();
    }
} 