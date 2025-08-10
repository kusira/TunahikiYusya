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

    // ★追加: ポップアップなどで実際に表示される名前
    [Tooltip("ポップアップなどで実際に表示される名前。空欄の場合はenemyNameが使用されます。")]
    public string displayName;
    
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
    // シングルトンインスタンス
    private static EnemyDatabase _instance;
    
    [Tooltip("ここに全種類の敵のデータを設定します")]
    [SerializeField] private List<EnemyDataEntry> enemies;
    
    // 検索を高速化するための辞書
    private Dictionary<string, EnemyDataEntry> enemyDictionary;
    
    // シングルトンインスタンスへのアクセサ
    public static EnemyDatabase Instance
    {
        get 
        { 
            if (_instance == null)
            {
                // インスタンスが見つからない場合は、シーンから探して初期化
                _instance = FindAnyObjectByType<EnemyDatabase>();
                if (_instance != null)
                {
                    _instance.InitializeDictionary();
                }
            }
            return _instance; 
        }
    }
    
    void Awake()
    {
        // シングルトンパターンの実装
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDictionary();
        }
        else if (_instance != this)
        {
            // 既にインスタンスが存在する場合は、このオブジェクトを破壊
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Startでも初期化を確認（二重チェック）
        if (enemyDictionary == null)
        {
            InitializeDictionary();
        }
    }
    
    private void InitializeDictionary()
    {
        // 既に初期化済みの場合は何もしない
        if (enemyDictionary != null) return;
        
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
        // 辞書が初期化されていない場合は初期化を試行
        if (enemyDictionary == null)
        {
            InitializeDictionary();
        }
        
        // 辞書に敵名が存在するかチェック
        if (enemyDictionary.TryGetValue(name, out EnemyDataEntry entry))
        {
            return entry;
        }
        
        // 対応するデータが見つからなかった場合
        Debug.LogError($"敵データベースに「{name}」のデータが見つかりません！");
        return null;
    }
    
    /// <summary>
    /// データベースが初期化されているかチェックします
    /// </summary>
    /// <returns>初期化済みの場合true</returns>
    public bool IsInitialized()
    {
        return enemyDictionary != null;
    }
}