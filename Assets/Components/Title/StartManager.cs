using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class StartManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private TransitionManager transitionManager;
    [SerializeField] private TextMeshProUGUI startText;
    
    [Header("音響設定")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("点滅設定")]
    [SerializeField] private float blinkDuration = 2f; // 1周期の時間（秒）
    [SerializeField] private float minAlpha = 0.2f;   // 最小透明度
    [SerializeField] private float maxAlpha = 1f;     // 最大透明度
    
    private void Start()
    {
        // コンポーネントの存在確認
        if (transitionManager == null)
        {
            transitionManager =  FindAnyObjectByType<TransitionManager>();
            if (transitionManager == null)
            {
                Debug.LogError("StartManager: TransitionManagerが見つかりません。", this);
            }
        }
        
        if (startText == null)
        {
            Debug.LogError("StartManager: StartTextが未アサインです。", this);
        }

        // AudioSourceの自動取得
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    
    private void Update()
    {
        // テキストの点滅処理（sin波）
        if (startText != null)
        {
            float time = Time.time;
            float sinValue = Mathf.Sin((2f * Mathf.PI * time) / blinkDuration);
            
            // sin波の値を0-1の範囲に変換し、透明度に適用
            float normalizedValue = (sinValue + 1f) * 0.5f; // -1~1 -> 0~1
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, normalizedValue);
            
            Color textColor = startText.color;
            textColor.a = alpha;
            startText.color = textColor;
        }
        
        // スペースキー入力の検出（新Input System対応）
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            OnStartButtonClicked();
        }
    }
    
    /// <summary>
    /// スタートボタンがクリックされた時の処理
    /// </summary>
    public void OnStartButtonClicked()
    {
        Debug.Log("StartManager: スタートボタンがクリックされました。メインシーンに遷移します。");
        
        // スタート音を再生
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        
        if (transitionManager != null)
        {
            transitionManager.PlayFadeOutAndLoad("MainScene");
        }
        else
        {
            Debug.LogError("StartManager: TransitionManagerが未設定のため、直接シーン遷移します。");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }
    }
}
