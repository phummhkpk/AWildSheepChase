using AWildSheepChase.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AWildSheepChase.UI
{
    /// <summary>
    /// ผูกปุ่มของ Pause / Game Over / Victory เข้ากับ GameManager
    /// ต้องวางบน object ที่เปิดอยู่เสมอ (เช่น Canvas ชื่อ HUD) ห้ามวางบนตัวหน้าต่างที่ซ่อนอยู่
    /// </summary>
    public class GameMenuUI : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Pause")]
        [SerializeField] private UIPanel pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseRestartButton;
        [SerializeField] private Button pauseMainMenuButton;

        [Header("Game Over")]
        [SerializeField] private UIPanel gameOverPanel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button gameOverMainMenuButton;

        [Header("Victory")]
        [SerializeField] private UIPanel victoryPanel;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button victoryMainMenuButton;

        private void Awake()
        {
            if (gameManager == null)
            {
                Debug.LogWarning("GameMenuUI: ยังไม่ได้ลาก GameManager ใส่ช่อง Game Manager", this);
            }
        }

        private void OnEnable()
        {
            if (gameManager == null) return;

            gameManager.StateChanged += OnStateChanged;

            Bind(resumeButton, gameManager.Resume);
            Bind(pauseRestartButton, gameManager.RestartLevel);
            Bind(pauseMainMenuButton, gameManager.LoadMainMenu);
            Bind(retryButton, gameManager.RetryFromCheckpoint);
            Bind(gameOverMainMenuButton, gameManager.LoadMainMenu);
            Bind(playAgainButton, gameManager.RestartLevel);
            Bind(victoryMainMenuButton, gameManager.LoadMainMenu);
        }

        private void OnDisable()
        {
            if (gameManager == null) return;

            gameManager.StateChanged -= OnStateChanged;

            Unbind(resumeButton, gameManager.Resume);
            Unbind(pauseRestartButton, gameManager.RestartLevel);
            Unbind(pauseMainMenuButton, gameManager.LoadMainMenu);
            Unbind(retryButton, gameManager.RetryFromCheckpoint);
            Unbind(gameOverMainMenuButton, gameManager.LoadMainMenu);
            Unbind(playAgainButton, gameManager.RestartLevel);
            Unbind(victoryMainMenuButton, gameManager.LoadMainMenu);
        }

        private void Start()
        {
            if (gameManager != null)
            {
                Apply(gameManager.State);
            }
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            Apply(next);
        }

        private void Apply(GameState state)
        {
            SetPanel(pausePanel, state == GameState.Paused);
            SetPanel(gameOverPanel, state == GameState.GameOver);
            SetPanel(victoryPanel, state == GameState.Victory);
        }

        private static void SetPanel(UIPanel panel, bool visible)
        {
            if (panel == null) return;

            if (visible) panel.Show();
            else panel.Hide();
        }

        private static void Bind(Button button, UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void Unbind(Button button, UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }
    }
}