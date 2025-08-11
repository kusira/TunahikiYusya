using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text targetText;

    [SerializeField]
    private List<string> hintMessages = new List<string>();

    private void Reset()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TMP_Text>();
        }
    }

    private void Start()
    {
        if (targetText == null)
        {
            Debug.LogWarning("HintManager: No TMP_Text assigned.");
            return;
        }

        if (hintMessages == null || hintMessages.Count == 0)
        {
            Debug.LogWarning("HintManager: No hint messages set.");
            return;
        }

        int randomIndex = Random.Range(0, hintMessages.Count);
        targetText.text = hintMessages[randomIndex];
    }
}
