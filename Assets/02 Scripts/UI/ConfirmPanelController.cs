using TMPro;
using UnityEngine;

public class ConfirmPanelController : PanelController
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI greenButtonText;
    [SerializeField] private TextMeshProUGUI redButtonText;
    
    public delegate void OnClickConfirm();
    private OnClickConfirm _onClickConfirm;

    public delegate void OnClickCancel();
    private  OnClickCancel _onClickCancel;
    
    //컨펌창 열기
    public void Show(string message, string confirmText, string cancelText, OnClickConfirm onClickConfirm = null, OnClickCancel onClickCancel =null)
    {
        _onClickConfirm = onClickConfirm;
        _onClickCancel = onClickCancel;
        messageText.text = message;
        greenButtonText.text = confirmText;
        redButtonText.text = cancelText;
        Show();
    }
    
    // x 버튼 누르면
    public void onClickGreenButton()
    {
        Hide(() =>
        {
            _onClickConfirm?.Invoke();
        });
    }

    public void onClickRedButton()
    {
        Hide(() =>
        {
            _onClickCancel?.Invoke();
        });
    }
}
