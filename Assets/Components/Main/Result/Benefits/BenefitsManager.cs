using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class BenefitsManager : MonoBehaviour
{
    [Header("確率調整")]
    [Tooltip("アンロック出現確率の倍率（例: 1.5 で約1.5倍）")]
    [SerializeField] private float unlockProbabilityBoost = 1.5f;
    [Tooltip("増員(Count)の重み低下度合い。大きいほどCountが多いキャラは出にくくなる。1.0〜2.0推奨")]
    [SerializeField] private float addCountWeightPower = 1.2f;
    [Header("生成するPrefab")]
    [SerializeField] private GameObject addPanelPrefab;      // AddPanelManage を持つ
    [SerializeField] private GameObject levelUpPanelPrefab;  // LevelUpPanelManager を持つ
    [SerializeField] private GameObject unlockPanelPrefab;   // UnlockPanelManager を持つ

    [Header("生成位置・遅延")]
    [SerializeField] private float spawnY = -80f;
    [SerializeField] private float spawnXMin = -100f;
    [SerializeField] private float spawnXMax = 100f;
    [SerializeField] private float perPanelDelay = 0.15f; // 1つ目×1, 2つ目×2, 3つ目×3

    [Header("生成条件")]
    [Tooltip("レベルアップを出す上限レベル（この値以下ならレベルアップ表示候補）")]
    [SerializeField] private int maxLevelForGeneratePanel = 2;
    [Tooltip("キャラ増加を出す上限カウント（この値以下なら増加表示候補）")]
    [SerializeField] private int maxCountForGeneratePanel = 5;

    [Header("候補キャラ一覧（CharacterDatabase/CardDatabaseのキーと一致）")]
    [SerializeField] private List<string> allCharacterNames = new List<string>();

    [Header("他パネルの退場設定")]
    [SerializeField] private float othersMoveDistance = 80f;
    [SerializeField] private float othersFadeDuration = 0.3f;

    private readonly List<MonoBehaviour> spawnedPanels = new List<MonoBehaviour>();

    [Header("トランジション設定")]
    [SerializeField] private float waitBeforeTransition = 0.5f; // 非スケール秒
    private bool transitionScheduled = false;
    [Tooltip("この値以上のステージなら閾値到達扱い")]
    [SerializeField] private int thresholdStage = 8;
    [Tooltip("閾値未満のときに遷移するシーン名")]
    [SerializeField] private string sceneBeforeThreshold = "MainScene";
    [Tooltip("閾値以上のときに遷移するシーン名")]
    [SerializeField] private string sceneAtOrBeyondThreshold = "EndScene";

    void Start()
    {
        Show();
    }

    // 入口: 外部から呼ぶ
    public void Show()
    {
        spawnedPanels.Clear();

        // 抽選のためのデータ準備
        var cardDb = CardDatabase.Instance != null ? CardDatabase.Instance : FindAnyObjectByType<CardDatabase>();
        if (cardDb == null)
        {
            Debug.LogError("CardDatabaseが見つかりません。シーンに配置してください。", this);
            return;
        }

        // 利用可能なキャラに限定（CardDatabaseに存在するもののみ）
        var usable = new List<(string name, CardDeckData deck)>();
        foreach (var n in allCharacterNames)
        {
            var d = cardDb.GetCardData(n);
            if (d != null) usable.Add((n, d));
        }
        if (usable.Count == 0) return;

        var locked = usable.Where(t => Mathf.Max(0, t.deck.level) == 0).Select(t => t.name).ToList();
        var unlocked = usable.Where(t => Mathf.Max(0, t.deck.level) > 0).Select(t => t.name).ToList();

        var spawnQueue = new List<(GameObject prefab, string character)>();

        // 1) アンロックの抽選（確率 = 未解放/全体）
        if (locked.Count > 0)
        {
            float prob = Mathf.Min(1f, ((float)locked.Count / Mathf.Max(1, usable.Count)) * Mathf.Max(0f, unlockProbabilityBoost));
            if (Random.value < prob && unlockPanelPrefab != null)
            {
                string pick = locked[Random.Range(0, locked.Count)];
                spawnQueue.Add((unlockPanelPrefab, pick));
            }
        }

        // 2) レベルアップ or キャラ増加（解放済みから）
        if (unlocked.Count > 0)
        {
            bool TryPickLevelUp(out string name)
            {
                var pool = unlocked.Where(n =>
                {
                    var d = cardDb.GetCardData(n);
                    return d != null && d.level > 0 && d.level <= maxLevelForGeneratePanel;
                }).ToList();
                name = BenefitsSelectionHelper.WeightedPickByLowLevelCount(pool, cardDb);
                return !string.IsNullOrEmpty(name);
            }

            bool TryPickAdd(out string name)
            {
                var pool = unlocked.Where(n =>
                {
                    var d = cardDb.GetCardData(n);
                    return d != null && d.level > 0 && d.count <= maxCountForGeneratePanel;
                }).ToList();
                name = BenefitsSelectionHelper.WeightedPickByCount(pool, cardDb, addCountWeightPower);
                return !string.IsNullOrEmpty(name);
            }

            var types = new List<string> { "LevelUp", "Add" };
            for (int i = 0; i < types.Count; i++)
            {
                int r = Random.Range(i, types.Count);
                (types[i], types[r]) = (types[r], types[i]);
            }

            foreach (var t in types)
            {
                if (t == "LevelUp" && levelUpPanelPrefab != null)
                {
                    if (TryPickLevelUp(out var name))
                    {
                        if (!spawnQueue.Any(s => s.prefab == levelUpPanelPrefab && s.character == name))
                            spawnQueue.Add((levelUpPanelPrefab, name));
                    }
                }
                else if (t == "Add" && addPanelPrefab != null)
                {
                    if (TryPickAdd(out var name))
                    {
                        if (!spawnQueue.Any(s => s.prefab == addPanelPrefab && s.character == name))
                            spawnQueue.Add((addPanelPrefab, name));
                    }
                }
            }
        }

        // 追加: 3枚を目標に最大再抽選
        int targetCount = 3;
        int rerollMax = 20;
        int reroll = 0;

        System.Func<string, bool> NotDupLevelUp = name =>
            !spawnQueue.Any(s => s.prefab == levelUpPanelPrefab && s.character == name);
        System.Func<string, bool> NotDupAdd = name =>
            !spawnQueue.Any(s => s.prefab == addPanelPrefab && s.character == name);
        System.Func<string, bool> NotDupUnlock = name =>
            !spawnQueue.Any(s => s.prefab == unlockPanelPrefab && s.character == name);

        while (spawnQueue.Count < targetCount && reroll < rerollMax)
        {
            reroll++;
            bool added = false;

            var types = new List<string> { "Unlock", "LevelUp", "Add" };
            for (int i = 0; i < types.Count; i++)
            {
                int r = Random.Range(i, types.Count);
                (types[i], types[r]) = (types[r], types[i]);
            }

            foreach (var t in types)
            {
                if (t == "Unlock" && locked.Count > 0 && unlockPanelPrefab != null)
                {
                    float prob = Mathf.Min(1f, ((float)locked.Count / Mathf.Max(1, usable.Count)) * Mathf.Max(0f, unlockProbabilityBoost));
                    if (Random.value < prob)
                    {
                        string pick = locked[Random.Range(0, locked.Count)];
                        if (NotDupUnlock(pick))
                        {
                            spawnQueue.Add((unlockPanelPrefab, pick));
                            added = true;
                            break;
                        }
                    }
                }
                else if (t == "LevelUp" && levelUpPanelPrefab != null && unlocked.Count > 0)
                {
                    var pool = unlocked.Where(n =>
                    {
                        var d = cardDb.GetCardData(n);
                        return d != null && d.level > 0 && d.level <= maxLevelForGeneratePanel;
                    }).ToList();

                    if (pool.Count > 0)
                    {
                        string name = BenefitsSelectionHelper.WeightedPickByLowLevelCount(pool, cardDb);
                        if (NotDupLevelUp(name))
                        {
                            spawnQueue.Add((levelUpPanelPrefab, name));
                            added = true;
                            break;
                        }
                    }
                }
                else if (t == "Add" && addPanelPrefab != null && unlocked.Count > 0)
                {
                    var pool = unlocked.Where(n =>
                    {
                        var d = cardDb.GetCardData(n);
                        return d != null && d.level > 0 && d.count <= maxCountForGeneratePanel;
                    }).ToList();

                    if (pool.Count > 0)
                    {
                        string name = BenefitsSelectionHelper.WeightedPickByCount(pool, cardDb, addCountWeightPower);
                        if (NotDupAdd(name))
                        {
                            spawnQueue.Add((addPanelPrefab, name));
                            added = true;
                            break;
                        }
                    }
                }
            }

            if (!added)
            {
                Debug.Log($"パネル抽選 {reroll}/{rerollMax}: 候補が見つかりませんでした。再抽選します。", this);
            }
        }

        if (spawnQueue.Count == 0)
        {
            Debug.LogWarning($"パネル抽選: 最大{rerollMax}回再抽選しても生成できなかったため、今回はパスします。", this);
            return;
        }
        else if (spawnQueue.Count < targetCount)
        {
            Debug.Log($"パネル抽選: {spawnQueue.Count}/{targetCount}件のみ生成できました。", this);
        }

        // 3) 生成・配置・遅延して各パネルのShow()を呼ぶ
        int order = 0;

        int countToSpawn = Mathf.Min(3, spawnQueue.Count);
        float[] tMap = { 0f, 0.5f, 1f }; // 左, 中央, 右

        for (int i = 0; i < countToSpawn; i++)
        {
            var (prefab, ch) = spawnQueue[i];
            order++;

            var go = Instantiate(prefab, transform);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                float x = Mathf.Lerp(spawnXMin, spawnXMax, tMap[i]);
                rt.anchoredPosition = new Vector2(x, spawnY);
            }

            var add = go.GetComponent<AddPanelManager>();
            var lv  = go.GetComponent<LevelUpPanelManager>();
            var ul  = go.GetComponent<UnlockPanelManager>();

            if (add != null)
            {
                spawnedPanels.Add(add);
                add.CharacterName = ch;
                DOVirtual.DelayedCall(perPanelDelay * order, () => add.Show());
            }
            else if (lv != null)
            {
                // ここで再確認: 本当にアンロックされているか？
                var data = cardDb.GetCardData(ch);
                bool isUnlocked = (data != null && Mathf.Max(0, data.level) > 0);

                if (!isUnlocked)
                {
                    // レベルアップではなくアンロックに差し替え
                    Destroy(go);
                    if (unlockPanelPrefab != null)
                    {
                        var go2 = Instantiate(unlockPanelPrefab, transform);
                        var rt2 = go2.GetComponent<RectTransform>();
                        if (rt2 != null)
                        {
                            float x = Mathf.Lerp(spawnXMin, spawnXMax, tMap[i]);
                            rt2.anchoredPosition = new Vector2(x, spawnY);
                        }
                        var ul2 = go2.GetComponent<UnlockPanelManager>();
                        if (ul2 != null)
                        {
                            spawnedPanels.Add(ul2);
                            ul2.CharacterName = ch;
                            DOVirtual.DelayedCall(perPanelDelay * order, () => ul2.Show());
                        }
                        else
                        {
                            Debug.LogWarning("unlockPanelPrefab から UnlockPanelManager が見つかりません。", go2);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("unlockPanelPrefab が未設定のため、差し替えできません。", this);
                    }
                }
                else
                {
                    spawnedPanels.Add(lv);
                    lv.CharacterName = ch;
                    DOVirtual.DelayedCall(perPanelDelay * order, () => lv.Show());
                }
            }
            else if (ul != null)
            {
                spawnedPanels.Add(ul);
                ul.CharacterName = ch;
                DOVirtual.DelayedCall(perPanelDelay * order, () => ul.Show());
            }
            else
            {
                Debug.LogWarning($"生成したPrefabに想定のコンポーネントが見つかりません: {prefab.name}", go);
            }
        }
    }

    // パネルから直接呼ばれる（FindAnyObjectByType で呼び出し）
    public void OnPanelSelected(MonoBehaviour selected)
    {
        foreach (var p in spawnedPanels)
        {
            if (p == null || p == selected) continue;
            if (p is AddPanelManager a) a.HideAsOther(othersMoveDistance, othersFadeDuration);
            else if (p is LevelUpPanelManager l) l.HideAsOther(othersMoveDistance, othersFadeDuration);
            else if (p is UnlockPanelManager u) u.HideAsOther(othersMoveDistance, othersFadeDuration);
        }
        spawnedPanels.Clear();

        if (!transitionScheduled)
        {
            transitionScheduled = true;
            StartCoroutine(CoTransitionAfterSelection());
        }
    }

    private IEnumerator CoTransitionAfterSelection()
    {
        // Time.timeScale=0 を想定 → リアルタイム待機
        yield return new WaitForSecondsRealtime(waitBeforeTransition);

        // ステージ更新（先にインクリメント）と遷移先決定
        var sm = StageManager.Instance ?? FindAnyObjectByType<StageManager>();
        int stage = 1;
        if (sm != null)
        {
            sm.IncrementStage();             // 遷移前にインクリメント
            stage = sm.CurrentStage;
        }
        string scene = (stage >= thresholdStage) ? sceneAtOrBeyondThreshold : sceneBeforeThreshold;

        // フェードアウト→シーン遷移
        var tm = FindAnyObjectByType<TransitionManager>();
        if (tm != null) tm.PlayFadeOutAndLoad(scene);
        else Debug.LogError("TransitionManager が見つかりません。", this);
    }

}

// 低レベル・低カウント優先の重み付き抽選ヘルパー
internal static class BenefitsSelectionHelper
{
    // 出現確率は (level+count)/(合計level+count) の逆数に比例
    // level+count==0 の場合は個別値を 1 として扱う
    public static string WeightedPickByLowLevelCount(List<string> candidates, CardDatabase cardDb)
    {
        if (candidates == null || candidates.Count == 0 || cardDb == null) return null;

        int total = 0;
        var perValues = new Dictionary<string, int>(candidates.Count);
        foreach (var name in candidates)
        {
            var d = cardDb.GetCardData(name);
            int v = 0;
            if (d != null)
            {
                v = Mathf.Max(0, d.level) + Mathf.Max(0, d.count);
            }
            total += v;
            perValues[name] = v;
        }

        if (total == 0)
        {
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        float sumWeights = 0f;
        var weights = new Dictionary<string, float>(candidates.Count);
        foreach (var name in candidates)
        {
            int v = perValues[name];
            float w = 1f / Mathf.Max(1, v);
            weights[name] = w;
            sumWeights += w;
        }

        float r = UnityEngine.Random.value * sumWeights;
        foreach (var name in candidates)
        {
            r -= weights[name];
            if (r <= 0f) return name;
        }
        return candidates[candidates.Count - 1];
    }

    // Count が増えるほど出にくくする重み付き抽選
    // weight = 1 / pow(max(1, count), power)
    public static string WeightedPickByCount(List<string> candidates, CardDatabase cardDb, float power)
    {
        if (candidates == null || candidates.Count == 0 || cardDb == null) return null;
        power = Mathf.Max(0.01f, power);

        float sumWeights = 0f;
        var weights = new Dictionary<string, float>(candidates.Count);
        foreach (var name in candidates)
        {
            var d = cardDb.GetCardData(name);
            int c = d != null ? Mathf.Max(0, d.count) : 0;
            float w = 1f / Mathf.Pow(Mathf.Max(1, c), power);
            weights[name] = w;
            sumWeights += w;
        }

        if (sumWeights <= 0f)
        {
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        float r = UnityEngine.Random.value * sumWeights;
        foreach (var name in candidates)
        {
            r -= weights[name];
            if (r <= 0f) return name;
        }
        return candidates[candidates.Count - 1];
    }
}