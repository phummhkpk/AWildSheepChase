using System;
using System.Collections;
using AWildSheepChase.Enemies;
using AWildSheepChase.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AWildSheepChase.Core
{
    public enum GameState { Playing, Paused, GameOver, Victory }

    /// <summary>คุมสถานะเกม: หยุดเกม, Game Over, Victory และการโหลด Scene (ไม่ใช่ Singleton ลากอ้างอิงผ่าน Inspector)</summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private PlayerRespawn playerRespawn;
        [SerializeField] private PlayerInputHandler playerInput;
        [Tooltip("ไม่บังคับ: ถ้าใส่ ฆ่าบอสแล้วจะขึ้นหน้า Victory")]
        [SerializeField] private BossController boss;
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField, Min(0f)] private float gameOverDelay = 1.5f;
        [SerializeField, Min(0f)] private float victoryDelay = 3f;

        /// <summary>(state เดิม, state ใหม่)</summary>
        public event Action<GameState, GameState> StateChanged;

        public GameState State { get; private set; } = GameState.Playing;

        private InputAction pauseAction;

        private void Awake()
        {
            pauseAction = new InputAction("Pause", InputActionType.Button);
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.AddBinding("<Gamepad>/start");

            if (player == null)
            {
                Debug.LogError("GameManager: ยังไม่ได้ลาก Player ใส่ช่อง Player", this);
            }

            if (playerRespawn == null)
            {
                Debug.LogWarning("GameManager: ยังไม่ได้ลาก Player ใส่ช่อง Player Respawn (ปุ่ม Retry จะใช้ไม่ได้)", this);
            }

            if (playerInput == null)
            {
                Debug.LogWarning("GameManager: ยังไม่ได้ลาก Player ใส่ช่อง Player Input (คลิกปุ่มเมนูอาจถูกนับเป็นโจมตี)", this);
            }
        }

        private void OnEnable()
        {
            pauseAction.performed += OnPausePerformed;
            pauseAction.Enable();

            if (player != null) player.Died += OnPlayerDied;
            if (boss != null) boss.Defeated += OnBossDefeated;
        }

        private void OnDisable()
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Disable();

            if (player != null) player.Died -= OnPlayerDied;
            if (boss != null) boss.Defeated -= OnBossDefeated;
        }

        private void OnDestroy()
        {
            pauseAction?.Dispose();
            GameTime.Reset();
        }

        // ---------- Pause ----------

        private void OnPausePerformed(InputAction.CallbackContext ctx)
        {
            TogglePause();
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                // ตายแล้วรอขึ้น Game Over ห้ามหยุดเกม
                if (player != null && player.IsDead) return;
                Pause();
            }
            else if (State == GameState.Paused)
            {
                Resume();
            }
        }

        public void Pause()
        {
            // ถ้าเวลาถูกหยุดอยู่แล้ว (เช่น กำลังคุยกับ NPC) ห้ามเปิดเมนู Pause ซ้อน
            if (State != GameState.Playing || GameTime.IsPaused) return;

            GameTime.SetPaused(true);
            SetPlayerInput(false);
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;

            GameTime.SetPaused(false);
            SetPlayerInput(true);
            SetState(GameState.Playing);
        }

        // ---------- Game Over / Victory ----------

        private void OnPlayerDied()
        {
            StartCoroutine(GameOverRoutine());
        }

        private IEnumerator GameOverRoutine()
        {
            yield return new WaitForSecondsRealtime(gameOverDelay);

            if (State != GameState.Playing || player == null || !player.IsDead) yield break;

            SetPlayerInput(false);
            SetState(GameState.GameOver);
        }

        private void OnBossDefeated()
        {
            StartCoroutine(VictoryRoutine());
        }

        private IEnumerator VictoryRoutine()
        {
            yield return new WaitForSecondsRealtime(victoryDelay);

            if (State != GameState.Playing || (player != null && player.IsDead)) yield break;

            SetPlayerInput(false);
            SetState(GameState.Victory);
        }

        /// <summary>เกิดใหม่ที่ Checkpoint ล่าสุด (ปุ่ม Retry ในหน้า Game Over)</summary>
        public void RetryFromCheckpoint()
        {
            if (State != GameState.GameOver || playerRespawn == null) return;

            playerRespawn.Respawn();
            SetPlayerInput(true);
            SetState(GameState.Playing);
        }

        // ---------- Scene ----------

        public void RestartLevel()
        {
            GameTime.Reset();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadMainMenu()
        {
            GameTime.Reset();
            SceneManager.LoadScene(mainMenuScene);
        }

        // ---------- Helpers ----------

        private void SetPlayerInput(bool enabled)
        {
            if (playerInput != null)
            {
                playerInput.enabled = enabled;
            }
        }

        private void SetState(GameState next)
        {
            if (State == next) return;

            GameState previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}