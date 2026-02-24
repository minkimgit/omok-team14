using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Constants;

public class GameSceneController : MonoBehaviour
{
    [SerializeField] private Image playerATurnImage;
    [SerializeField] private Image playerBTurnImage;
    [SerializeField] private RectTransform playerATurnRect;
    [SerializeField] private RectTransform playerBTurnRect;
    [SerializeField] private BoardRenderer boardRenderer;
    private string surrenderText = "게임을 기권하시겠습니까? 기권시 패배로 처리됩니다.";
    private string victoryText = "승리하였습니다.";
    private string defeatText = "패배하였습니다.";
    

    private AudioManager _audioManager;
    private OmokBoard _omokBoard;
    private int _currentPlayer = 1; // 1 = 흑, 2 = 백

    private void Start()
    {
        _audioManager = FindObjectOfType<AudioManager>();
        _omokBoard = new OmokBoard();
        boardRenderer.OnCellClicked += OnCellClicked;
        GameManager.Instance.SetGameTurn(PlayerType.Player1);
    }
    
    private void OnCellClicked(int row, int col)
    {
        if (!_omokBoard.PlaceStone(col, row, _currentPlayer)) return; 
        
        boardRenderer.PlaceStoneAt(row, col, _currentPlayer);
        _audioManager.PlayStonePlaceSfx();

        if (_omokBoard.CheckWin(col, row, _currentPlayer))
        {
            Debug.Log($"플레이어 {_currentPlayer} 승리!");
            _audioManager.PlayWinSfx();
            OpenGameOverPanel();
            return;
        }

        // 턴 교체
        _currentPlayer = (_currentPlayer == 1) ? 2 : 1;
        GameManager.Instance.SetGameTurn(
            _currentPlayer == 1 ? PlayerType.Player1 : PlayerType.Player2
        );
    }
    
    private void OnDestroy()
    {
        boardRenderer.OnCellClicked -= OnCellClicked;
    }
    
    public void OnClickSurrenderButton()
    {
        GameManager.Instance.OpenConfirmPanel(surrenderText, "네", "아니요",() =>
        {
            GameManager.Instance.ChangeToMainScene();
        });
    }

    public void OpenGameOverPanel()
    {
        GameManager.Instance.OpenConfirmPanel(victoryText, "다시하기", "나가기", () =>
        {
            ResetGame();
        },
        () =>
        {
            GameManager.Instance.ChangeToMainScene();
        });
    }

    private void ResetGame()
    {
        boardRenderer.ClearBoard();
        _omokBoard = new OmokBoard();
        _currentPlayer = 1;
        GameManager.Instance.SetGameTurn(PlayerType.Player1);
    }
    
    public void SetPlayerTurnPanel(PlayerType playerTurnType)
    {
        switch (playerTurnType)
        {
            case PlayerType.None:
                playerATurnImage.color = Color.white;
                playerBTurnImage.color = Color.white;
                break;
            case PlayerType.Player1:
                playerATurnImage.color = Color.lightGreen;
                playerBTurnImage.color = Color.white;
                playerATurnRect.DOLocalMove(new Vector3(0, 25f, 0), 1).SetEase(Ease.OutBack);
                playerATurnRect.DOScale(1.05f, 1).SetEase(Ease.OutBack);
                playerBTurnRect.DOLocalMove(Vector3.zero, 1).SetEase(Ease.OutBack);
                playerBTurnRect.DOScale(1f, 1).SetEase(Ease.OutBack);
                break;
            case PlayerType.Player2:
                playerATurnImage.color = Color.white;
                playerBTurnImage.color = Color.lightGreen;
                playerBTurnRect.DOLocalMove(new Vector3(0, 25f, 0), 1).SetEase(Ease.OutBack);
                playerBTurnRect.DOScale(1.05f, 1).SetEase(Ease.OutBack);
                playerATurnRect.DOLocalMove(Vector3.zero, 1).SetEase(Ease.OutBack);
                playerATurnRect.DOScale(1f, 1).SetEase(Ease.OutBack);
                break;
        }
    }
}
