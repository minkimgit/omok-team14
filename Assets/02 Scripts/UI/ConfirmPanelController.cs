using TMPro;
using UnityEngine;

public class ConfirmPanelController : PanelController
{
    [SerializeField] private TextMeshProUGUI messageText;
    
    public delegate void OnClickConfirm();
    private OnClickConfirm _onClickConfirm;
    
    //컨펌창 열기
    public void Show(string message, OnClickConfirm onClickConfirm = null)
    {
        _onClickConfirm = onClickConfirm;
        messageText.text = message;
        Show();
    }
    
    // x 버튼 누르면
    public void onClickYesButton()
    {
        Hide(() =>
        {
            _onClickConfirm?.Invoke();
        });
    }

    public void onClickNoButton()
    {
        Hide();
    }
}
