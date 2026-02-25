using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LoginPanelController : PanelController
{
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    
    [SerializeField] private Button closeButton;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;

    void Start()
    {
        closeButton.onClick.AddListener(OnClickCloseButton);
        loginButton.onClick.AddListener(OnClickLoginButton);
        registerButton.onClick.AddListener(OnClickRegisterButton);
        
        passwordInputField.onEndEdit.AddListener(delegate { OnEndEditPassword(); });
    }
    
    private void OnEndEditPassword()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            // 1. 로그인 버튼에서 ButtonInteraction 컴포넌트를 찾습니다.
            var btnInteraction = loginButton.GetComponent<ButtonInteraction>();
        
            if (btnInteraction != null)
            {
                // 2. 마우스로 누른 것처럼 이벤트를 강제로 보냅니다 (PointerEventData는 null로 줘도 무방합니다)
                btnInteraction.OnPointerDown(null);
            
                // 3. 아주 잠깐 뒤에(애니메이션이 보일 수 있게) 떼는 효과를 줍니다.
                // 간단하게 하기 위해 DOTween의 연기(Delay) 기능을 활용하거나 직접 호출합니다.
                DOVirtual.DelayedCall(0.1f, () => {
                    btnInteraction.OnPointerUp(null);
                });
            }

            // 4. 실제 로그인 로직 실행
            OnClickLoginButton();
        }
    }

    void Update()
    {
        // 1. 키보드 입력 체크 (New Input System 방식)
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
    
        // 2. Tab 키로 포커스 이동
        if (keyboard.tabKey.wasPressedThisFrame)
        {
            NavigateFields();
        }
    
        // 3. Enter 키로 로그인/필드이동
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            OnEnterPressed();
        }
    }

    private void NavigateFields()
    {
        if (emailInputField.isFocused) passwordInputField.ActivateInputField();
        else emailInputField.ActivateInputField();
    }

    private void OnEnterPressed()
    {
        if (emailInputField.isFocused)
        {
            passwordInputField.ActivateInputField();
        }
        else if (passwordInputField.isFocused)
        {
            OnClickLoginButton(); // 마지막 필드에서 엔터 치면 로그인 실행
        }
    }

    private void OnClickCloseButton()
    {
        Hide();
    }

    private void OnClickLoginButton()
    {
        string email = emailInputField.text;
        string pw = passwordInputField.text;
        
        Debug.Log($"[로그인 시도] Email: {email}, PW: {pw}");
        
        // !! 여기서 서버에 연결 시도 !!
        
        // TODO: 서버 로그인 로직 연결
    }

    private void OnClickRegisterButton()
    {
        GameManager.Instance.OpenRegisterPanel();
    }
}