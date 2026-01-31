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

        if (iconImage == null || colorImage == null)
        {
            return;
        }


        
        iconImage.sprite = danceIcons[danceInfo.DanceIndex-1];
        iconImage.enabled = true;
    


        // 색상 설정
        Color newColor = GetDisplayColor(danceInfo.Color);
        colorImage.color = newColor;

        colorImage.enabled = true;


        gameObject.SetActive(true);
    }

    public void HideCell()
    {
        gameObject.SetActive(false);
    }

    private Color GetDisplayColor(MaskColor maskColor)
    {
        foreach (var mapping in colorMappings)
        {
            if (mapping.maskColor == maskColor)
            {
                return mapping.displayColor;
            }
        }
        return Color.white; // 기본값
    }
}

