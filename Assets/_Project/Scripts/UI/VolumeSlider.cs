using AWildSheepChase.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AWildSheepChase.UI
{
    /// <summary>ผูก Slider เข้ากับช่องเสียงหนึ่งช่อง ผูกด้วยโค้ด ไม่ต้องตั้ง On Value Changed ใน Inspector</summary>
    [RequireComponent(typeof(Slider))]
    public class VolumeSlider : MonoBehaviour
    {
        [SerializeField] private VolumeChannel channel;
        [Tooltip("ไม่บังคับ: ข้อความแสดงเปอร์เซ็นต์")]
        [SerializeField] private TMP_Text valueText;

        private Slider slider;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }

        private void OnEnable()
        {
            slider.SetValueWithoutNotify(VolumeSettings.Get(channel));
            UpdateText(slider.value);
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(OnValueChanged);
            VolumeSettings.Save();
        }

        private void OnValueChanged(float value)
        {
            VolumeSettings.Set(channel, value);
            UpdateText(value);
        }

        private void UpdateText(float value)
        {
            if (valueText != null)
            {
                valueText.text = Mathf.RoundToInt(value * 100f) + "%";
            }
        }
    }
}