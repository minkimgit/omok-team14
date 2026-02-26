using UnityEngine;
using TMPro;
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

        // 3. 모든 조건을 통과했을 때만 NetworkManager를 통해 서버 연결 및 가입 요청
        Debug.Log("서버 연결 및 회원가입 시도...");
        NetworkManager.Instance.RequestRegister(email, pw);
    }

    // 서버에 연결된 경우에 실행
    private void HandleRegisterResponse(bool success, string message)
    {
        Debug.Log($"[회원가입 결과] 성공: {success}, 메시지: {message}");
        
        if (success)
        {
            Debug.Log("계정이 생성되었습니다.");
            // 성공 시 처리 (예: 패널 닫기)
            Hide();
        }
    }

    private void OnClickCloseButton() => Hide();
}