using UnityEngine;
using UnityEngine.SceneManagement;
using static Constants;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameObject settingsPanelPrefab;
    [SerializeField] private GameObject confirmPanelPrefab;
    [SerializeField] private GameObject rankingPanelPrefab;
    
    // 계정 관련
    [SerializeField] private GameObject loginPanelPrefab;
    [SerializeField] private GameObject registerPanelPrefab;
    [SerializeField] private GameObject logoutPanelPrefab;

    // 캔버스
    private Canvas _canvas;
    private GameObject _popups;
    
    // 게임 화면의 UI 컨트롤러
    private GameSceneController _gameSceneController; 
    //
    // // Game Logic
    // private GameLogic _gameLogic;

    // 게임의 종류 (싱글, 듀얼)
    public GameType CurrentGameType { get; private set; }

    protected override void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        _canvas = FindFirstObjectByType<Canvas>();
        _popups = GameObject.Find("Popups");
        _gameSceneController = FindFirstObjectByType<GameSceneController>();
    }

    // Game O/X UI 업데이트
    public void SetGameTurn(PlayerType playerTurnType)
    {
        _gameSceneController.SetPlayerTurnPanel(playerTurnType);
    }

    // Settings 패널 열기
    public void OpenSettingsPanel()
    {
        var settingsPanelObject = Instantiate(settingsPanelPrefab, _popups.transform);
        settingsPanelObject.GetComponent<SettingsPanelController>().Show();
    }

    public void OpenRankingPanel()
    {
        var rankingPanelObject = Instantiate(rankingPanelPrefab, _popups.transform);
        
        //todo: rankingPanelController 만들기
    }

    // // Confirm 패널 열기
    public void OpenConfirmPanel(string message, string confirmText, string cancelText, ConfirmPanelController.OnClickConfirm onClickConfirm, ConfirmPanelController.OnClickCancel onClickCancel = null)
    {
        var confirmPanelObject = Instantiate(confirmPanelPrefab, _canvas.transform);
        confirmPanelObject.GetComponent<ConfirmPanelController>().Show(message, confirmText, cancelText, onClickConfirm,  onClickCancel);
    }

    // 회원가입/로그인 팝업 열기
    public void OpenLoginOrRegisterPanel()
    {
        var loginPanelObject = Instantiate(loginPanelPrefab, _popups.transform);
        loginPanelObject.GetComponent<LoginPanelController>().Show();
    }

    public void OpenLogoutPanel()
    {
        var logoutPanelObject = Instantiate(logoutPanelPrefab, _popups.transform);
        logoutPanelObject.GetComponent<LogoutPanelController>().Show();
    }
    
    public void OpenRegisterPanel()
    {
        var registerPanelObject = Instantiate(registerPanelPrefab, _popups.transform);
        registerPanelObject.GetComponent<RegisterPanelController>().Show();
    }

    // 씬 전환 (Main > Game)
    public void ChangeToGameScene(GameType gameType)
    {
        CurrentGameType = gameType;
        SceneManager.LoadScene(SCENE_GAME);
    }

    // 씬 전환 (Game > Main)
    public void ChangeToMainScene()
    {
        SceneManager.LoadScene(SCENE_MAIN);
    }
}
