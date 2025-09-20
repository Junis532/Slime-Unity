//using UnityEngine;
//using System.IO;

//public class SaveManager : MonoSigleTone<SaveManager>
//{
//    string filePath = Application.dataPath + "/playerData.txt";


//    private void Start()
//    {
//        Debug.Log(Application.dataPath);
//    }
//    public void SaveData(PlayerData data)
//    {
//        string json = JsonUtility.ToJson(data, true); // true는 보기 좋게 포맷팅
//        File.WriteAllText(filePath, json);
//        Debug.Log("저장 완료: " + filePath);
//    }

//    public PlayerData LoadData()
//    {
//        if (File.Exists(filePath))
//        {
//            Directory.CreateDirectory(filePath);
//            Directory.Delete(filePath, true);
//            string json = File.ReadAllText(filePath);
//            PlayerData data = JsonUtility.FromJson<PlayerData>(json);
//            Debug.Log("불러오기 완료");
//            return data;
//        }
//        else
//        {
//            Debug.LogWarning("파일 없음");
//            return null;
//        }
//    }
//}
