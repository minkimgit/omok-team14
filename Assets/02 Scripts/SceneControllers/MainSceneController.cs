using TMPro;
using UnityEngine;
using static Constants;

public class MainSceneController : MonoBehaviour
{
    [SerializeField] private GameObject loginOrRegisterButton;
    [SerializeField] private GameObject logoutButton;
    [SerializeField] private GameObject emailText;

    void Start()
    {
        UpdateAccountUI(GameManager.Instance.IsLoggedIn);
    }
    
    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoginStateChanged += UpdateAccountUI;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnLoginStateChanged -= UpdateAccountUI;
        }
    }
    
    public void OnClickSinglePlayButton()
    {
        GameManager.Instance.ChangeToGameScene(GameType.SinglePlay);
    }

    public void OnClickDualPlayButton()
    {
        GameManager.Instance.ChangeToGameScene(GameType.DualPlay);
    }

    public void OnClickMultiPlayButton()
    {
        if (GameManager.Instance.IsLoggedIn)
        {
            GameManager.Instance.StartMultiplayFlow();
            // 매칭 대기 중 팝업 — 상대를 찾으면 씬 전환 시 자동으로 닫힘
            GameManager.Instance.OpenConfirmPanel(
                "대결 상대 찾는중...",
                "숨기기",       // 녹색 버튼 (기능 없음)
                "취소",   // 빨간 버튼 — 매칭 취소 후 팝업 닫기
                null,
                () => NetworkManager.Instance.EmitCancelMatchmaking()
            );
        }
        else
        {
            Debug.Log("<color=red></color> 로그인을 해야합니다.");
        }
    }

    public void OnClickSettingsButton()
    {
        GameManager.Instance.OpenSettingsPanel();
    }

    public void OnClickRankingButton()
    {
        GameManager.Instance.OpenRankingPanel();
    }

    public void OnClickQuitGameButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnClickLoginOrRegisterButton()
    {
        GameManager.Instance.OpenLoginOrRegisterPanel();
    }

    public void OnClickLogoutButton()
    {
        GameManager.Instance.OpenLogoutPanel();
    }

    // 로그인/로그아웃 상태 변경에 따른 UI 업데이트 적용
    private void UpdateAccountUI(bool isLoggedIn)
    {
        loginOrRegisterButton.SetActive(!isLoggedIn);
        logoutButton.SetActive(isLoggedIn);

        if (isLoggedIn)
        {
            emailText.GetComponent<TextMeshProUGUI>().text = $"계정 : {GameManager.Instance.UserEmail}";
            emailText.SetActive(true);
        }
        else
        {
            emailText.GetComponent<TextMeshProUGUI>().text = "";
            emailText.SetActive(false);
        }
    }
}