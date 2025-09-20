using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class RoulettePiece : MonoBehaviour
{
    [SerializeField]
    private Image imageIcon; // 룰렛 조각의 아이콘 이미지
    //[SerializeField]
    //private TextMeshProUGUI textDescription; // 룰렛 조각의 설명 텍스트

    public void Setup(RoulettePieceData pieceData)
    {
        imageIcon.sprite = pieceData.icon;
        //textDescription.text = pieceData.description;
    }
}