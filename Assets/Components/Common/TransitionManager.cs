using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform unmask;
    [SerializeField] private AudioSource audioSource; // 追加：フェードアウト時に再生する音

    [Header("設定")]
    [SerializeField] private float transitionDuration = 0.3f; // デフォルト0.3s
    [SerializeField] private float targetScale = 40f;         // 0 <-> 40
    [SerializeField] private Ease easeIn = Ease.OutQuad;      // 0 -> 40
    [SerializeField] private Ease easeOut = Ease.InQuad;      // 40 -> 0
    [SerializeField] private bool playFadeInOnStart = true;   // シーン開始時にフェードイン
    [SerializeField] private float fadeInDelay = 0f;          // フェードイン開始前の待機時間（秒）

    private Tween currentTween;

    void Start()
    {
        // 待機時間中はunmaskのスケールを0に設定
        if (unmask != null)
        {
            unmask.localScale = Vector3.zero;
        }
        
        if (playFadeInOnStart)
        {
            if (fadeInDelay > 0f)
            {
                StartCoroutine(DelayedFadeIn());
            }
            else
            {
                PlayFadeIn();
            }
        }
    }
    
    private IEnumerator DelayedFadeIn()
    {
        yield return new WaitForSeconds(fadeInDelay);
        PlayFadeIn();
    }

    public void PlayFadeIn()
    {
        if (unmask == null)
        {
            Debug.LogError("TransitionManager: Unmask が未アサインです。", this);
            return;
        }
        KillTween();
        unmask.localScale = Vector3.zero;
        currentTween = unmask
            .DOScale(Vector3.one * targetScale, transitionDuration)
            .SetEase(easeIn)
            .SetUpdate(true);
    }


    public void PlayFadeOutAndLoad(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("TransitionManager: 遷移先シーン名が空です。", this);
            return;
        }
        StartCoroutine(CoFadeOutAndLoad(sceneName));
    }

    private IEnumerator CoFadeOutAndLoad(string sceneName)
    {
        if (unmask == null)
        {
            Debug.LogError("TransitionManager: Unmask が未アサインです。", this);
            yield break;
        }

        KillTween();
        unmask.localScale = Vector3.one * targetScale;

        // フェードアウト時に音を再生
        if (audioSource != null)
        {
            audioSource.Play();
        }

        bool done = false;
        currentTween = unmask
            .DOScale(Vector3.zero, transitionDuration)
            .SetEase(easeOut)
            .SetUpdate(true)
            .OnComplete(() => done = true);

        while (!done) yield return null;

        SceneManager.LoadScene(sceneName);
    }

    private void KillTween()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill(false);
            currentTween = null;
        }
        if (unmask != null) unmask.DOKill(false);
    }

    void OnDisable() => KillTween();
    void OnDestroy() => KillTween();
}