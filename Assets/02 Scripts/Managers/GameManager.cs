using UnityEngine;
using UnityEngine.SceneManagement;
using static Constants;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private GameObject settingsPanelPrefab;
    [SerializeField] private GameObject confirmPanelPrefab;
    [SerializeField] private GameObject rankingPanelPrefab;

    // 캔버스
    private Canvas _canvas;

    // 게임 화면의 UI 컨트롤러
    private GameSceneController _gameSceneController; 
    //
    // // Game Logic
    // private GameLogic _gameLogic;

    // 게임의 종류 (싱글, 듀얼)
    private GameType _gameType;

    protected override void OnSceneLoad(Scene scene, LoadSceneMode mode)
    {
        _canvas = FindFirstObjectByType<Canvas>();
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
        var settingsPanelObject = Instantiate(settingsPanelPrefab, _canvas.transform);
        settingsPanelObject.GetComponent<SettingsPanelController>().Show();
    }

    public void OpenRankingPanel()
    {
        var rankingPanelObject = Instantiate(rankingPanelPrefab, _canvas.transform);
        //todo: rankingPanelController 만들기
    }

    // // Confirm 패널 열기
    public void OpenConfirmPanel(string message, ConfirmPanelController.OnClickConfirm onClickConfirm)
    {
        var confirmPanelObject = Instantiate(confirmPanelPrefab, _canvas.transform);
        confirmPanelObject.GetComponent<ConfirmPanelController>().Show(message, onClickConfirm);
    }

    // 씬 전환 (Main > Game)
    public void ChangeToGameScene(GameType gameType)
    {
        _gameType = gameType;
        SceneManager.LoadScene(SCENE_GAME);
    }

    // 씬 전환 (Game > Main)
    public void ChangeToMainScene()
    {
        SceneManager.LoadScene(SCENE_MAIN);
    }
}
