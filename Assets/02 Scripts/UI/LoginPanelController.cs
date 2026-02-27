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
    
    // 씬에 패널이 나타날 때 이벤트를 구독합니다.
    private void OnEnable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnLoginResponseReceived += HandleLoginResponse;
        }
    }

    // 패널이 사라지거나 파괴될 때 구독을 해제합니다.
    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnLoginResponseReceived -= HandleLoginResponse;
        }
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

    private void OnClickLoginButton()
    {
        string email = emailInputField.text;
        string pw = passwordInputField.text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pw))
        {
            Debug.Log("[유효성 검사] 이메일과 비밀번호를 모두 입력해주세요.");
            return;
        }

        // 이미 연결되어 있다면 바로 요청, 아니면 연결 후 요청
        if (NetworkManager.Instance.Socket != null && NetworkManager.Instance.Socket.Connected)
        {
            NetworkManager.Instance.RequestLogin(email, pw);
        }
        else
        {
            Debug.Log("[Network] 서버 재연결 시도 후 로그인 요청...");
            NetworkManager.Instance.ConnectToServer(() => {
                NetworkManager.Instance.RequestLogin(email, pw);
            });
        }
    }

    private void HandleLoginResponse(bool success, string message, int code)
    {
        // 0번은 성공이므로 즉시 처리
        if (success && code == 0)
        {
            Debug.Log("<color=green>[로그인 성공]</color> 환영합니다!");
            
            GameManager.Instance.SetLoginState(true, emailInputField.text);
            
            Hide();
            return;
        }

        // 실패 시 code에만 집중하여 처리
        string errorLog = "";
    
        switch (code)
        {
            case 1:
                errorLog = "<color=red>[계정 에러]</color> 존재하지 않는 계정입니다.";
                break;
            case 2:
                errorLog = "<color=yellow>[보안 에러]</color> 비밀번호가 일치하지 않습니다.";
                break;
            case 99:
                errorLog = "<color=white>[서버 에러]</color> 서버 내부 오류가 발생했습니다.";
                break;
            default:
                // 서버에서 정의되지 않은 코드가 올 경우를 대비한 방어 코드
                errorLog = $"[알 수 없는 에러] 에러 코드: {code}";
                break;
        }

        Debug.Log(errorLog);
    }

    private void OnClickRegisterButton()
    {
        GameManager.Instance.OpenRegisterPanel();
    }
    
    private void OnClickCloseButton() => Hide();
}