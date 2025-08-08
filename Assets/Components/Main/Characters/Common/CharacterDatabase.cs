using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// キャラクターの特定のレベルにおけるステータスのデータ。
/// </summary>
[System.Serializable]
public class CharacterLevelStats
{
    public int hp = 10;
    public int atk = 1;
    [Tooltip("このキャラクターがクールダウン付きのスキルを持つか")]
    public bool hasCooldownSkill = false;
    [Tooltip("スキルのクールダウン時間（秒）")]
    public float skillCooldownTime = 5.0f;
    [Tooltip("スキルの説明など")]
    public string skillDescription = "スキル説明";
}

/// <summary>
/// 1人のキャラクターに関する全レベルのステータスをまとめたデータ。
/// </summary>
[System.Serializable]
public class CharacterDataEntry
{
    [Tooltip("キャラクターの名前。PlacedCharacterのキャラ名と完全に一致させる必要があります。")]
    public string characterName;
    [Tooltip("レベル1から順にステータスを設定します (Element 0 = Lv1, Element 1 = Lv2, ...)")]
    public List<CharacterLevelStats> levelStats = new List<CharacterLevelStats>();
}

/// <summary>
/// ゲーム内に存在する全てのキャラクターのステータスを管理するデータベース。
/// シーンに一つだけ配置します。
/// </summary>
public class CharacterDatabase : MonoBehaviour
{
    [Tooltip("ここに全キャラクターのデータを設定します")]
    [SerializeField] private List<CharacterDataEntry> characters;
    
    // 検索を高速化するための辞書
    private Dictionary<string, CharacterDataEntry> characterDictionary;
    
    void Awake()
    {
        // 起動時にリストを辞書に変換して、高速にアクセスできるようにする
        characterDictionary = characters.ToDictionary(x => x.characterName);
    }

    /// <summary>
    /// 指定されたキャラクター名とレベルに応じたステータスを取得します。
    /// </summary>
    /// <param name="name">キャラクター名</param>
    /// <param name="level">レベル (1, 2, 3, ...)</param>
    /// <returns>対応するステータスデータ。見つからなければnullを返します。</returns>
    public CharacterLevelStats GetStats(string name, int level)
    {
        // 辞書にキャラクター名が存在するかチェック
        if (characterDictionary.TryGetValue(name, out CharacterDataEntry entry))
        {
            // レベルは1から始まるが、リストのインデックスは0からなので-1する
            int index = level - 1;
            // レベルがステータスリストの範囲内かチェック
            if (index >= 0 && index < entry.levelStats.Count)
            {
                return entry.levelStats[index];
            }
        }
        
        // 対応するデータが見つからなかった場合
        Debug.LogError($"キャラクターデータベースに「{name}」のレベル{level}のデータが見つかりません！");
        return null;
    }
}