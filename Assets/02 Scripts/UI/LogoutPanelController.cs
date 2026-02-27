using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogoutPanelController : PanelController
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    
    void Start()
    {
        confirmButton.onClick.AddListener(OnClickConfirmButton);
        cancelButton.onClick.AddListener(OnClickCancelButton);
    }

    private void OnClickConfirmButton()
    {
        // 로그이웃 절차 수행
        GameManager.Instance.SetLoginState(false, String.Empty);

        Hide();
    }

    private void OnClickCancelButton() => Hide();
}
