using UnityEngine;
using System.Collections.Generic;
using System.Linq;


// Inspectorで設定するためのデータ構造
[System.Serializable]
public class EnemyData
{
    public GameObject prefab;
    public int cost;
}

/// <summary>
/// 設定に基づいて、敵が配置されたロープを自動生成するクラス。
/// </summary>
public class RopeGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("敵が4体まで配置されるロープのPrefab")]
    [SerializeField] private GameObject ropeAPrefab;
    [Tooltip("敵が6体まで配置されるロープのPrefab")]
    [SerializeField] private GameObject ropeBPrefab;

    [Header("敵のデータベース")]
    [Tooltip("出現する可能性のある敵のリスト（Prefabとコスト）")]
    [SerializeField] private List<EnemyData> enemyDatabase;
    
    [Header("生成設定")]
    [Tooltip("生成したロープを格納する親オブジェクト")]
    [SerializeField] private Transform spawnParent;
    [Tooltip("生成するロープの数")]
    [SerializeField] private int numberOfRopes = 5;
    [Tooltip("1本のロープに配置される敵の合計コストの上限")]
    [SerializeField] private int maxTotalCost = 15;
    [Tooltip("ゲーム開始時に自動で生成を実行するか")]
    [SerializeField] private bool generateOnStart = true;
    // ★追加: RopeAに収まる場合でもRopeBを生成する確率
    [Tooltip("敵が4体以下の場合に、あえて大きなRopeBを使用する確率")]
    [SerializeField] [Range(0f, 1f)] private float upgradeToRopeBChance = 0.25f;

    [Header("配置座標の設定")]
    [Tooltip("ロープを生成するY座標")]
    [SerializeField] private float spawnYPosition = 0f;
    [Tooltip("ロープを等間隔に配置するX座標の最小値")]
    [SerializeField] private float minXPosition = -8f;
    [Tooltip("ロープを等間隔に配置するX座標の最大値")]
    [SerializeField] private float maxXPosition = 8f;

    private System.Random random = new System.Random();


    void Awake()
    {
        if (generateOnStart)
        {
            Generate();
        }
    }

    /// <summary>
    /// 設定に基づいてロープの生成を実行します。
    /// </summary>
    public void Generate()
    {
        if (ropeAPrefab == null || ropeBPrefab == null || enemyDatabase == null || enemyDatabase.Count == 0)
        {
            Debug.LogError("RopeGeneratorの必須コンポーネントが設定されていません。");
            return;
        }
        
        for (int i = 0; i < numberOfRopes; i++)
        {
            List<EnemyData> selectedEnemies = SelectEnemies(maxTotalCost);
            if (selectedEnemies.Count == 0)
            {
                Debug.LogWarning("コスト上限内で配置できる敵がいませんでした。");
                continue;
            }

            // ★変更: 敵の数と確率に応じて使用するロープPrefabとスロット数を決定
            GameObject ropePrefabToUse;
            int totalSlots;

            if (selectedEnemies.Count <= 4 && Random.value > upgradeToRopeBChance)
            {
                // 4体以下で、かつアップグレード抽選に外れた場合はRopeA
                ropePrefabToUse = ropeAPrefab;
                totalSlots = 4;
            }
            else
            {
                // 5体以上、または4体以下でもアップグレード抽選に当たった場合はRopeB
                ropePrefabToUse = ropeBPrefab;
                totalSlots = 6;
            }
            
            // 生成位置の計算
            float xPos = (numberOfRopes > 1) 
                ? Mathf.Lerp(minXPosition, maxXPosition, (float)i / (numberOfRopes - 1)) 
                : (minXPosition + maxXPosition) / 2f;
            Vector3 spawnPosition = new Vector3(xPos, spawnYPosition, 0);

            GameObject newRope = Instantiate(ropePrefabToUse, spawnParent);
            newRope.transform.localPosition = spawnPosition;
            RopeManager ropeManager = newRope.GetComponent<RopeManager>();
            if (ropeManager == null)
            {
                Debug.LogError("Rope PrefabにRopeManagerがアタッチされていません！");
                Destroy(newRope);
                continue;
            }

            // ★変更: ランダムな位置に敵を配置する
            PlaceEnemiesOnRopeRandomly(ropeManager, selectedEnemies, totalSlots);
        }
    }

    private List<EnemyData> SelectEnemies(int maxCost)
    {
        var selected = new List<EnemyData>();
        var available = new List<EnemyData>(enemyDatabase);
        int currentCost = 0;

        while (available.Count > 0 && selected.Count < 6)
        {
            int randomIndex = Random.Range(0, available.Count);
            EnemyData candidate = available[randomIndex];

            if (currentCost + candidate.cost <= maxCost)
            {
                selected.Add(candidate);
                currentCost += candidate.cost;
            }
            available.RemoveAt(randomIndex);
        }
        return selected;
    }

    // ★関数名を変更し、ロジックを全面的に書き換え
    /// <summary>
    /// 選択された敵を、ロープ上の空いているスロットにランダムに配置する
    /// </summary>
    private void PlaceEnemiesOnRopeRandomly(RopeManager ropeManager, List<EnemyData> enemies, int totalSlots)
    {
        // 1. 利用可能なすべてのスロットインデックスを作成 (例: 0, 1, 2, 3)
        List<int> availableSlotIndices = Enumerable.Range(0, totalSlots).ToList();
        
        // 2. スロットインデックスをシャッフルして、ランダムな配置順にする
        Shuffle(availableSlotIndices);

        // 3. RopeManagerの行リストをスロット数に合わせて初期化 (空の行を作成)
        ropeManager.enemyRows = new List<EnemyRow>();
        int numRows = totalSlots / 2;
        for (int i = 0; i < numRows; i++)
        {
            ropeManager.enemyRows.Add(new EnemyRow());
        }

        // 4. 選択された敵を、シャッフルされたスロット順に配置していく
        for (int i = 0; i < enemies.Count; i++)
        {
            int slotIndex = availableSlotIndices[i]; // シャッフルされたスロット番号を取得

            GameObject enemyInstance = Instantiate(enemies[i].prefab, ropeManager.transform);

            // スロット番号から行と左右を決定
            int rowIndex = slotIndex / 2;
            bool isLeft = (slotIndex % 2 == 0);
            
            // 座標を決定
            float xPos = isLeft ? -0.7f : 0.7f;
            float yPos = rowIndex + 1;
            enemyInstance.transform.localPosition = new Vector3(xPos, yPos, 0);

            // RopeManagerの対応する場所に登録
            if (isLeft)
            {
                ropeManager.enemyRows[rowIndex].leftEnemy = enemyInstance;
            }
            else
            {
                ropeManager.enemyRows[rowIndex].rightEnemy = enemyInstance;
            }
        }
    }

    // ★追加: Fisher-Yatesアルゴリズムによるリストのシャッフル
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}