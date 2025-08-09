using UnityEngine;
using TMPro;

public class GetRopeManager : MonoBehaviour
{
    [Header("表示用テキスト (TMP)")]
    [SerializeField] private TMP_Text leftText;
    [SerializeField] private TMP_Text rightText;

    private void Start()
    {
        if (leftText != null) leftText.text = "0";
        if (rightText != null) rightText.text = "0";
    }
}

