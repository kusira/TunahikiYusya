using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class CurrentStageManager : MonoBehaviour
{
    [Header("表示先（未指定なら自動取得）")]
    [SerializeField] private TMP_Text targetText;

    [Header("表示設定")]
    [Tooltip("表示フォーマット。{0} にステージ番号が入ります")]
    [SerializeField] private string format = "Stage {0}";

    [Tooltip("毎フレーム更新するか（外部でステージが変化する可能性がある場合はON推奨）")]
    [SerializeField] private bool updateEveryFrame = false;

    private StageManager stageManager;

    void Awake()
    {
        if (targetText == null) targetText = GetComponent<TMP_Text>();
    }

    void Start()
    {
        stageManager = StageManager.Instance != null
            ? StageManager.Instance
            : FindAnyObjectByType<StageManager>();

        Refresh();
    }

    void Update()
    {
        if (updateEveryFrame) Refresh();
    }

    public void Refresh()
    {
        if (targetText == null) return;

        int stage = 1;
        if (stageManager == null)
        {
            stageManager = StageManager.Instance != null
                ? StageManager.Instance
                : FindAnyObjectByType<StageManager>();
        }
        if (stageManager != null) stage = Mathf.Max(1, stageManager.CurrentStage);

        targetText.text = string.Format(format, stage);
    }
}
