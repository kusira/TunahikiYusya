using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// メインシーンのBGMを管理するマネージャー
/// シーン遷移しても音楽が途切れないようにシングルトンで実装
/// </summary>
public class MainBGMManager : MonoBehaviour
{
    [Header("BGM設定")]
    [Tooltip("メインシーンのBGMを再生するAudioSource")]
    [SerializeField] private AudioSource bgmAudioSource;
    
    [Tooltip("BGMの音量（0.0f - 1.0f）")]
    [SerializeField, Range(0.0f, 1.0f)] private float bgmVolume = 1.0f;
    
    // シングルトンインスタンス
    public static MainBGMManager Instance { get; private set; }
    
    // 現在のシーン名を追跡
    private string currentSceneName;
    
    // BGMが再生中かどうか
    public bool IsBGMPlaying => bgmAudioSource != null && bgmAudioSource.isPlaying;
    
    private void Awake()
    {
        // シングルトンパターンの実装
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初期設定
            SetupBGM();
            
            // シーン変更イベントを監視
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            // 既存のインスタンスが存在する場合は破棄
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // 現在のシーン名を取得
        currentSceneName = SceneManager.GetActiveScene().name;
        
        // MainSceneの場合のみBGMを開始
        if (IsMainScene())
        {
            StartBGM();
        }
        else
        {
            // MainScene以外の場合はBGMを停止
            StopBGM();
        }
    }
    
    private void Update()
    {
        // 現在のシーン名を監視
        string currentScene = SceneManager.GetActiveScene().name;
        if (!IsMainScene())
        {
            // MainScene以外に遷移した場合、BGMを停止
            Destroy(gameObject);
        }

    }
    
    private void SetupBGM()
    {
        if (bgmAudioSource != null)
        {
            // BGMの設定
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = bgmVolume;
            bgmAudioSource.playOnAwake = false;
        }
        else
        {
            Debug.LogWarning("MainBGMManager: BGM AudioSourceが設定されていません！", this);
        }
    }
    
    /// <summary>
    /// シーンが読み込まれた時の処理
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newSceneName = scene.name;
        
        // シーンが変更された場合
        if (newSceneName != currentSceneName)
        {
            currentSceneName = newSceneName;
            
            if (IsMainScene())
            {
                // MainSceneに遷移した場合、BGMを開始
                StartBGM();
            }
            else
            {
                // MainScene以外に遷移した場合、BGMを停止
                StopBGM();
            }
        }
    }
    
    /// <summary>
    /// 現在のシーンがMainSceneかどうかを判定
    /// </summary>
    private bool IsMainScene()
    {
        return currentSceneName == "MainScene";
    }
    
    /// <summary>
    /// BGMを開始する
    /// </summary>
    public void StartBGM()
    {
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Play();
            Debug.Log("MainBGMManager: BGMを開始しました");
        }
    }
    
    /// <summary>
    /// BGMを停止する
    /// </summary>
    public void StopBGM()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
            Debug.Log("MainBGMManager: BGMを停止しました");
        }
    }
    
    /// <summary>
    /// BGMを一時停止する
    /// </summary>
    public void PauseBGM()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Pause();
            Debug.Log("MainBGMManager: BGMを一時停止しました");
        }
    }
    
    /// <summary>
    /// BGMの一時停止を解除する
    /// </summary>
    public void UnpauseBGM()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying == false)
        {
            bgmAudioSource.UnPause();
            Debug.Log("MainBGMManager: BGMの一時停止を解除しました");
        }
    }
    
    /// <summary>
    /// BGMの音量を設定する
    /// </summary>
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = bgmVolume;
        }
    }
    
    /// <summary>
    /// BGMの音量を取得する
    /// </summary>
    public float GetBGMVolume()
    {
        return bgmVolume;
    }
    
    /// <summary>
    /// BGMをフェードインする
    /// </summary>
    public void FadeInBGM(float duration = 1.0f)
    {
        if (bgmAudioSource != null)
        {
            StartCoroutine(FadeBGM(0f, bgmVolume, duration));
        }
    }
    
    /// <summary>
    /// BGMをフェードアウトする
    /// </summary>
    public void FadeOutBGM(float duration = 1.0f)
    {
        if (bgmAudioSource != null)
        {
            StartCoroutine(FadeBGM(bgmVolume, 0f, duration));
        }
    }
    
    /// <summary>
    /// BGMのフェード処理
    /// </summary>
    private System.Collections.IEnumerator FadeBGM(float startVolume, float endVolume, float duration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float currentVolume = Mathf.Lerp(startVolume, endVolume, elapsedTime / duration);
            
            if (bgmAudioSource != null)
            {
                bgmAudioSource.volume = currentVolume;
            }
            
            yield return null;
        }
        
        // 最終的な音量を設定
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = endVolume;
        }
    }
    
    private void OnDestroy()
    {
        // シーン変更イベントの監視を解除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // アプリが一時停止された時の処理
        if (pauseStatus)
        {
            // アプリが一時停止された場合、BGMも一時停止
            if (IsMainScene() && IsBGMPlaying)
            {
                PauseBGM();
            }
        }
        else
        {
            // アプリが再開された場合、BGMも再開
            if (IsMainScene() && !IsBGMPlaying)
            {
                UnpauseBGM();
            }
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        // アプリのフォーカスが変更された時の処理
        if (!hasFocus)
        {
            // フォーカスが外れた場合、BGMを一時停止
            if (IsMainScene() && IsBGMPlaying)
            {
                PauseBGM();
            }
        }
        else
        {
            // フォーカスが戻った場合、BGMを再開
            if (IsMainScene() && !IsBGMPlaying)
            {
                UnpauseBGM();
            }
        }
    }
}
