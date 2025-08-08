using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 1体の敵に関するステータスをまとめたデータ。
/// </summary>
[System.Serializable]
public class EnemyDataEntry
{
    [Tooltip("敵の名前。敵を識別するために使用します。")]
    public string enemyName;
    
    [Header("基本ステータス")]
    public int hp = 10;
    public int atk = 1;
    [Tooltip("この敵がクールダウン付きのスキルを持つか")]
    public bool hasCooldownSkill = false;
    [Tooltip("スキルのクールダウン時間（秒）")]
    public float skillCooldownTime = 5.0f;
    [Tooltip("スキルの説明など")]
    public string skillDescription = "スキル説明";
}

/// <summary>
/// ゲーム内に存在する全ての敵のステータスを管理するデータベース。
/// シーンに一つだけ配置します。
/// </summary>
public class EnemyDatabase : MonoBehaviour
{
    [Tooltip("ここに全種類の敵のデータを設定します")]
    [SerializeField] private List<EnemyDataEntry> enemies;
    
    // 検索を高速化するための辞書
    private Dictionary<string, EnemyDataEntry> enemyDictionary;
    
    void Awake()
    {
        // 起動時にリストを辞書に変換して、高速にアクセスできるようにする
        if (enemies != null)
        {
            enemyDictionary = enemies.ToDictionary(x => x.enemyName);
        }
        else
        {
            enemyDictionary = new Dictionary<string, EnemyDataEntry>();
            Debug.LogWarning("EnemyDatabaseに敵が一人も設定されていません。", this);
        }
    }

    /// <summary>
    /// 指定された敵名に応じたステータスを取得します。
    /// </summary>
    /// <param name="name">敵の名前</param>
    /// <returns>対応するステータスデータ。見つからなければnullを返します。</returns>
    public EnemyDataEntry GetStats(string name)
    {
        // 辞書に敵名が存在するかチェック
        if (enemyDictionary.TryGetValue(name, out EnemyDataEntry entry))
        {
            return entry;
        }
        
        // 対応するデータが見つからなかった場合
        Debug.LogError($"敵データベースに「{name}」のデータが見つかりません！");
        return null;
    }
}