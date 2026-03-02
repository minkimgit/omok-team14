using DG.Tweening;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RegisterPanelController : PanelController
{
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private TMP_InputField confirmPasswordInputField;
    
    [SerializeField] private Button closeButton;
    [SerializeField] private Button registerButton;

    void Start()
    {
        closeButton.onClick.AddListener(OnClickCloseButton);
        registerButton.onClick.AddListener(OnClickRegisterButton);
        
        confirmPasswordInputField.onEndEdit.AddListener(delegate { OnEndEditConfirmPassword(); });
    }

    // 패널이 활성화될 때 서버 응답 이벤트를 구독합니다.
    private void OnEnable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRegisterResponseReceived += HandleRegisterResponse;
        }
    }

    // 패널이 비활성화되거나 파괴될 때 구독을 해제합니다. (메모리 누수 방지)
    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnRegisterResponseReceived -= HandleRegisterResponse;
        }
    }
    
    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Tab 키로 포커스 순환 이동
        if (keyboard.tabKey.wasPressedThisFrame)
        {
            NavigateFields();
        }

        // Enter 키 처리
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            OnEnterPressed();
        }
    }
    
    // Tab 키를 누를 때 필드 순서대로 포커스 이동
    private void NavigateFields()
    {
        if (emailInputField.isFocused) passwordInputField.ActivateInputField();
        else if (passwordInputField.isFocused) confirmPasswordInputField.ActivateInputField();
        else emailInputField.ActivateInputField();
    }

    // Enter 키를 누를 때의 동작
    private void OnEnterPressed()
    {
        if (emailInputField.isFocused)
        {
            passwordInputField.ActivateInputField();
        }
        else if (passwordInputField.isFocused)
        {
            confirmPasswordInputField.ActivateInputField();
        }
        else if (confirmPasswordInputField.isFocused)
        {
            OnClickRegisterButton(); // 마지막 필드에서 엔터 시 가입 실행
        }
    }

    // 마지막 필드에서 엔터 입력 시 버튼 애니메이션 효과를 위한 함수
    private void OnEndEditConfirmPassword()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            var btnInteraction = registerButton.GetComponent<ButtonInteraction>();
            if (btnInteraction != null)
            {
                btnInteraction.OnPointerDown(null);
                DOVirtual.DelayedCall(0.1f, () => {
                    btnInteraction.OnPointerUp(null);
                });
            }
            // OnEnterPressed에서 이미 호출되므로 중복 호출 방지를 위해 로직 확인 후 사용
            // 여기서는 시각적 효과만 주고 실제 로직은 OnEnterPressed에 맡기거나 여기서 직접 호출합니다.
        }
    }

    private void OnClickRegisterButton()
    {
        string email = emailInputField.text;
        string pw = passwordInputField.text;
        string confirmPw = confirmPasswordInputField.text;

        // 1. 모든 필드가 채워져 있는지 확인 (공백 제거 후 검사)
        if (string.IsNullOrWhiteSpace(email) || 
            string.IsNullOrWhiteSpace(pw) || 
            string.IsNullOrWhiteSpace(confirmPw))
        {
            Debug.Log("모든 항목을 입력해야 합니다.");
            // Tip: 여기에 사용자에게 보여줄 에러 텍스트 UI를 띄우면 좋습니다.
            return; 
        }

        // 2. 비밀번호와 비밀번호 확인이 일치하는지 확인
        if (pw != confirmPw)
        {
            Debug.Log("비밀번호가 서로 일치하지 않습니다.");
            return;
        }
        
        NetworkManager.Instance.RequestRegister(email, pw);
    }

    private void HandleRegisterResponse(bool success, string message, int code)
    {
        // 1. 성공 케이스 (Code 0)
        if (success && code == 0)
        {
            Debug.Log("<color=green>[회원가입 성공]</color> 계정이 성공적으로 생성되었습니다. 이제 로그인할 수 있습니다!");
            Hide();
            return;
        }

        // 2. 실패 케이스 (Code 기반 분기)
        string registerErrorLog = "";

        switch (code)
        {
            case 1:
                registerErrorLog = "<color=yellow>[가입 실패]</color> 이미 존재하는 이메일입니다. 다른 이메일을 사용해주세요.";
                break;
            case 2:
                registerErrorLog = "<color=orange>[데이터 에러]</color> 입력 정보가 올바르지 않거나 누락되었습니다.";
                break;
            case 99:
                registerErrorLog = "<color=red>[서버 에러]</color> 서버 DB 문제로 가입에 실패했습니다. 잠시 후 다시 시도해주세요.";
                break;
            default:
                // 예외 상황 (예: 네트워크 연결 끊김 등)
                registerErrorLog = $"<color=white>[알 수 없는 에러]</color> {message} (Code: {code})";
                break;
        }

        Debug.Log(registerErrorLog);
    }

    private void OnClickCloseButton() => Hide();
}