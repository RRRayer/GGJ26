using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI content;
    [SerializeField] private Button closeButton;
    [SerializeField] private UIGenericButton button1; // Yes button
    [SerializeField] private UIGenericButton button2; // No  button

    // UI 매니저가 할당
    public event UnityAction<bool> ConfirmationResponseAction; // Yes button을 눌렀을 때 Action
    public event UnityAction ClosePopupAction; // 팝업창 닫는 액션
    
    public void SetPopup(PopupType popupType)
    {
        bool isConfirmation = false;
        bool hasCloseButton = false;
        switch (popupType)
        {
            case PopupType.NewGame:
                isConfirmation = true;
                hasCloseButton = false;
                title.text = "새 게임";
                content.text = "새 게임을 시작하시겠습니까?\n 기존 데이터는 사라집니다.";
                button1.SetButton("예");
                button2.SetButton("아니오");
                break;
            case PopupType.Quit:
                isConfirmation = true;
                hasCloseButton = false;
                title.text = "나가기";
                content.text = "정말 게임을 나가시겠습니까?\n그러지 마세요";
                button1.SetButton("예");
                button2.SetButton("아니오");
                break;
            case PopupType.BackToMenu:
                isConfirmation = true;
                hasCloseButton = false;
                title.text = "메인 메뉴로 이동";
                content.text = "메인 메뉴로 나가시겠습니까?";
                button1.SetButton("예");
                button2.SetButton("아니오");
                break;
            case PopupType.Prototype:
                isConfirmation = false;
                hasCloseButton = false;
                title.text = "프로토타입";
                content.text = "여기까지가 드릴이의 여정이었습니다.\n앞으로도 드릴이 사랑해주실거죠..?";
                button1.SetButton("확인");
                break;
        }

        if (isConfirmation) // 확인, 취소 2개의 버튼이 필요한 경우
        {
            button1.gameObject.SetActive(true);
            button2.gameObject.SetActive(true);
            button1.Clicked += ConfirmButtonClicked;
            button2.Clicked += ClosePopupButtonClicked;
        }
        else // 하나의 버튼만 필요한 경우(정보 팝업)
        {
            button1.gameObject.SetActive(true);
            button2.gameObject.SetActive(false);
            button1.Clicked += ConfirmButtonClicked;   
        }

        if (hasCloseButton) // 닫기 버튼이 존재하면 이벤트 할당
        {
            closeButton.gameObject.SetActive(true);
            closeButton.onClick.AddListener(ClosePopupButtonClicked);    
        }
        else
        {
            closeButton.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 확인용 팝업창을 띄우고, "예" 대답만 존재한다.
    /// </summary>
    public void SetPopup(string str)
    {
        title.text = "";
        content.text = str;
        button1.SetButton("예");
        
        button1.gameObject.SetActive(true);
        button2.gameObject.SetActive(false);
        
        button1.Clicked += ClosePopupButtonClicked;
        closeButton.gameObject.SetActive(false);
    }

    private void ConfirmButtonClicked()
    {
        ConfirmationResponseAction?.Invoke(true);
    }
    
    private void ClosePopupButtonClicked()
    {
        button1.Clicked -= ConfirmButtonClicked;
        button2.Clicked -= ClosePopupButtonClicked;
        closeButton.onClick.RemoveListener(ClosePopupButtonClicked);
        
        ClosePopupAction?.Invoke();
    }
}

public enum PopupType
{
    Quit,       // 게임 종료
    NewGame,    // 새 게임
    BackToMenu, // 메인메뉴롷 돌아가기
    Prototype   // 프로토타입 끝
}