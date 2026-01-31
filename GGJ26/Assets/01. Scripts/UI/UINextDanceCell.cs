using UnityEngine;
using UnityEngine.UI;

public class UINextDanceCell : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image colorImage; // 배경색 등으로 색상을 표시할 이미지

    [System.Serializable]
    public struct ColorMapping
    {
        public MaskColor maskColor;
        public Color displayColor;
    }
    [SerializeField] private ColorMapping[] colorMappings;
    
    [SerializeField] private Sprite[] danceIcons;

    public void UpdateCell(DanceInfo danceInfo)
    {
        Debug.Log($"[{name}] UpdateCell Called with Color: {danceInfo.Color}, DanceIndex: {danceInfo.DanceIndex}", this);

        if (iconImage == null || colorImage == null)
        {
            return;
        }

        // 아이콘 설정 (danceIndex 1부터 시작한다고 가정)
        if (danceInfo.DanceIndex > 0)
        {
            iconImage.sprite = danceIcons[danceInfo.DanceIndex -1];
            iconImage.enabled = true;
            Debug.Log($"[{name}] 아이콘 설정: {danceIcons[danceInfo.DanceIndex-1].name}", this);
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            Debug.LogWarning($"[{name}] 유효하지 않은 DanceIndex({danceInfo.DanceIndex-1}) 또는 danceIcons 배열이 설정되지 않음. 아이콘을 숨깁니다.", this);
        }

        // 색상 설정
        Color newColor = GetDisplayColor(danceInfo.Color);
        colorImage.color = newColor;
        Debug.Log($"[{name}] 색상 설정: {newColor}", this);

        colorImage.enabled = true;


        gameObject.SetActive(true);
        Debug.Log($"[{name}] 활성화됨 (SetActive(true))", this);
    }

    public void HideCell()
    {
        Debug.Log($"[{name}] HideCell Called. 비활성화됩니다.", this);
        gameObject.SetActive(false);
    }

    private Color GetDisplayColor(MaskColor maskColor)
    {
        foreach (var mapping in colorMappings)
        {
            if (mapping.maskColor == maskColor)
            {
                Debug.Log($"[{name}] ColorMapping 발견: {maskColor} -> {mapping.displayColor}", this);
                return mapping.displayColor;
            }
        }
        Debug.LogWarning($"[{name}] {maskColor}에 해당하는 ColorMapping을 찾지 못했습니다. 기본 흰색을 반환합니다.", this);
        return Color.white; // 기본값
    }
}

