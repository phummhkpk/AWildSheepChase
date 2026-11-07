using System;
using System.IO;
using UnityEngine;

namespace AWildSheepChase.SaveLoad
{
    /// <summary>อ่าน/เขียน/ลบไฟล์เซฟ และจำว่า Scene เกมที่กำลังจะโหลดต้องโหลดเซฟหรือไม่</summary>
    public static class SaveSystem
    {
        private const string FileName = "save.json";

        private static bool loadRequested;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        public static bool HasSave => File.Exists(FilePath);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            loadRequested = false;
        }

        /// <summary>เมนูหลักเรียกก่อนโหลด Scene เกม (ปุ่ม Continue)</summary>
        public static void RequestLoadOnNextScene() => loadRequested = true;

        public static void CancelLoadRequest() => loadRequested = false;

        /// <summary>SaveManager เรียกตอนเริ่ม Scene: คืน true ครั้งเดียวแล้วเคลียร์ค่า</summary>
        public static bool ConsumeLoadRequest()
        {
            bool value = loadRequested;
            loadRequested = false;
            return value;
        }

        public static bool Write(SaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                string temp = FilePath + ".tmp";

                File.WriteAllText(temp, json);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temp, FilePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("SaveSystem: บันทึกไม่สำเร็จ " + e.Message);
                return false;
            }
        }

        public static bool TryRead(out SaveData data)
        {
            data = null;

            try
            {
                if (!File.Exists(FilePath)) return false;

                string json = File.ReadAllText(FilePath);
                data = JsonUtility.FromJson<SaveData>(json);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogError("SaveSystem: อ่านไฟล์เซฟไม่สำเร็จ " + e.Message);
                data = null;
                return false;
            }
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError("SaveSystem: ลบไฟล์เซฟไม่สำเร็จ " + e.Message);
            }
        }
    }
}