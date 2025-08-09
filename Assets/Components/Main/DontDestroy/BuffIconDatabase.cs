using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// バフのIDとアイコンスプライトの対応を管理するデータベース。
/// シーンに一つだけ配置して使用します。
/// </summary>
public class BuffIconDatabase : MonoBehaviour
{
    // シーン内のどこからでもアクセスできる静的インスタンス
    public static BuffIconDatabase Instance { get; private set; }

    // バフIDとスプライトをセットでInspectorから設定するための内部クラス
    [System.Serializable]
    public class BuffIconMapping
    {
        [Tooltip("バフを識別するための一意のID（例: warrior_atk_up）")]
        public string buffId;
        [Tooltip("上記IDに対応するアイコンのスプライト")]
        public Sprite iconSprite;
    }

    [Header("バフアイコンリスト")]
    [SerializeField]
    private List<BuffIconMapping> buffIconMappings;

    void Awake()
    {
        // シングルトンパターンの実装
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // シーンをまたいで使用する場合はこの行を有効化
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// バフIDを基に対応するアイコンのスプライトを取得します。
    /// </summary>
    /// <param name="buffId">取得したいアイコンのバフID</param>
    /// <returns>対応するスプライト。見つからなければnull。</returns>
    public Sprite GetBuffIcon(string buffId)
    {
        if (string.IsNullOrEmpty(buffId)) return null;

        var mapping = buffIconMappings.FirstOrDefault(m => m.buffId == buffId);
        if (mapping != null)
        {
            return mapping.iconSprite;
        }

        Debug.LogWarning($"BuffIconDatabaseにID '{buffId}' のアイコンが見つかりませんでした。");
        return null;
    }
}