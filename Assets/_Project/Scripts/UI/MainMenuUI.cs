using AWildSheepChase.Core;
using AWildSheepChase.SaveLoad;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AWildSheepChase.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Tooltip("ชื่อ Scene ของเกม ต้องเพิ่มใน Build Settings")]
        [SerializeField] private string gameScene = "Prototype";
        [SerializeField] private Button continueButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            // กันกรณีกลับมาจากเกมที่ timeScale ค้างอยู่
            GameTime.Reset();
            SaveSystem.CancelLoadRequest();

            if (startButton == null)
            {
                Debug.LogWarning("MainMenuUI: ยังไม่ได้ลากปุ่ม Start ใส่ช่อง Start Button", this);
            }

            // ซ่อนปุ่ม Continue ถ้ายังไม่มีเซฟ
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(SaveSystem.HasSave);
            }
        }

        private void OnEnable()
        {
            if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
            if (startButton != null) startButton.onClick.AddListener(StartNewGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        }

        private void OnDisable()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(ContinueGame);
            if (startButton != null) startButton.onClick.RemoveListener(StartNewGame);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        }

        private void Start()
        {
            if (EventSystem.current == null) return;

            bool hasContinue = continueButton != null && continueButton.gameObject.activeSelf;
            Button first = hasContinue ? continueButton : startButton;
            if (first != null)
            {
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
        }

        private void ContinueGame()
        {
            SaveSystem.RequestLoadOnNextScene();
            SceneManager.LoadScene(gameScene);
        }

        private void StartNewGame()
        {
            // ไม่ลบเซฟเดิม จะถูกเขียนทับเมื่อผ่าน Checkpoint แรกของเกมใหม่
            SaveSystem.CancelLoadRequest();
            SceneManager.LoadScene(gameScene);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}