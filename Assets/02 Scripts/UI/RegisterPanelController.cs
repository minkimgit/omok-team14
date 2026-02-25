using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RegisterPanelController : PanelController
{
    [SerializeField] private TMP_InputField emailInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private TMP_InputField confirmPasswordInputField; // 비밀번호 확인 필드 추가
    
    [SerializeField] private Button closeButton;
    [SerializeField] private Button registerButton; // 계정 생성 버튼

    void Start()
    {
        closeButton.onClick.AddListener(OnClickCloseButton);
        registerButton.onClick.AddListener(OnClickRegisterButton);
        
        // 마지막 필드에서 엔터 감지를 위한 리스너
        confirmPasswordInputField.onEndEdit.AddListener(delegate { OnEndEditConfirmPassword(); });
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
    
        // 1. Tab 키로 순차적 포커스 이동 (Email -> PW -> Confirm PW -> Email)
        if (keyboard.tabKey.wasPressedThisFrame)
        {
            NavigateFields();
        }
    }

    private void NavigateFields()
    {
        if (emailInputField.isFocused) 
            passwordInputField.ActivateInputField();
        else if (passwordInputField.isFocused) 
            confirmPasswordInputField.ActivateInputField();
        else 
            emailInputField.ActivateInputField();
    }

    private void OnEndEditConfirmPassword()
    {
        // 마지막 칸에서 엔터를 눌렀을 때
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            // 버튼 애니메이션 연출 (기존 로직 동일)
            var btnInteraction = registerButton.GetComponent<ButtonInteraction>();
            if (btnInteraction != null)
            {
                btnInteraction.OnPointerDown(null);
                DOVirtual.DelayedCall(0.1f, () => {
                    btnInteraction.OnPointerUp(null);
                });
            }

            // 계정 생성 로직 실행
            OnClickRegisterButton();
        }
    }

    private void OnClickCloseButton()
    {
        Hide();
    }

    private void OnClickRegisterButton()
    {
        string email = emailInputField.text;
        string pw = passwordInputField.text;
        string confirmPw = confirmPasswordInputField.text;

        // 1. 비밀번호 일치 확인
        if (pw != confirmPw)
        {
            Debug.Log("[계정생성 실패] 비밀번호가 서로 일치하지 않습니다.");
            // 여기서 사용자에게 알려주는 UI 텍스트 등을 띄우면 좋습니다.
            return;
        }

        // 2. 데이터 유효성 검사 (간단히)
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            Debug.Log("[계정생성 실패] 모든 항목을 입력해주세요.");
            return;
        }

        Debug.Log($"[계정생성 시도] Email: {email}, PW: {pw} (비밀번호 일치 확인됨)");
        
        // !! 여기서 서버에 연결 시도 !!
        
        // TODO: 서버(Firebase 등)에 실제 계정 생성 요청 로직 작성
    }
}