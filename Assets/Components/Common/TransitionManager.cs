using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections;

public class TransitionManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform unmask;

    [Header("設定")]
    [SerializeField] private float transitionDuration = 0.3f; // デフォルト0.3s
    [SerializeField] private float targetScale = 40f;         // 0 <-> 40
    [SerializeField] private Ease easeIn = Ease.OutQuad;      // 0 -> 40
    [SerializeField] private Ease easeOut = Ease.InQuad;      // 40 -> 0
    [SerializeField] private bool playFadeInOnStart = true;   // シーン開始時にフェードイン

    private Tween currentTween;

    void Start()
    {
        if (playFadeInOnStart)
        {
            PlayFadeIn();
        }
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