using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BenefitsManager : MonoBehaviour
{
    [Header("テキスト表示")]
    [SerializeField] private TMP_Text benefitsText;
    [SerializeField] private float textFadeDuration = 0.25f;
    [SerializeField] private float textMoveY = 20f; // 少し下から表示

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

    // 入口: 外部から呼ぶ
    public void Show()
    {
        AnimateText();
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
            float prob = (float)locked.Count / usable.Count;
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
                name = pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
                return name != null;
            }

            bool TryPickAdd(out string name)
            {
                var pool = unlocked.Where(n =>
                {
                    var d = cardDb.GetCardData(n);
                    return d != null && d.level > 0 && d.count <= maxCountForGeneratePanel;
                }).ToList();
                name = pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : null;
                return name != null;
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
                    float prob = (float)locked.Count / Mathf.Max(1, usable.Count);
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
                        string name = pool[Random.Range(0, pool.Count)];
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
                        string name = pool[Random.Range(0, pool.Count)];
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

            var add = go.GetComponent<AddPanelManage>();
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
                spawnedPanels.Add(lv);
                lv.CharacterName = ch;
                DOVirtual.DelayedCall(perPanelDelay * order, () => lv.Show());
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

            if (p is AddPanelManage a) a.HideAsOther(othersMoveDistance, othersFadeDuration);
            else if (p is LevelUpPanelManager l) l.HideAsOther(othersMoveDistance, othersFadeDuration);
            else if (p is UnlockPanelManager u) u.HideAsOther(othersMoveDistance, othersFadeDuration);
        }
        spawnedPanels.Clear();
    }

    private void AnimateText()
    {
        if (benefitsText == null) return;

        var rt = benefitsText.GetComponent<RectTransform>();
        if (rt == null) return;

        Color c = benefitsText.color;
        c.a = 0f;
        benefitsText.color = c;

        Vector2 orig = rt.anchoredPosition;
        rt.anchoredPosition = new Vector2(orig.x, orig.y - textMoveY);

        benefitsText.DOFade(1f, textFadeDuration);
        rt.DOAnchorPosY(orig.y, textFadeDuration).SetEase(Ease.OutQuad);
    }
}