using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

/// <summary>
/// プレイヤー検出機能を持つUdonSharpスクリプト
/// プレイヤーとの距離を検出し、距離に応じて色を変更したり、プレイヤー名を表示します
/// </summary>
/// <remarks>
/// 【このスクリプトの目的】
/// VRChatワールドでプレイヤーの接近を検出し、インタラクティブな反応を実装する方法を学習します。
/// プレイヤーAPIの使い方、距離計算、リアルタイム更新の基本を理解できます。
/// 
/// 【動作の仕組み】
/// 1. Update()メソッドで毎フレーム実行される
/// 2. 最も近いプレイヤーを検出
/// 3. 距離に応じて色を変更（近い：赤、遠い：青）
/// 4. プレイヤー名をテキストで表示
/// 
/// 【VRChat特有の考慮事項】
/// - プレイヤーAPIはVRChat固有のAPI
/// - ローカルプレイヤー（自分）と他のプレイヤーを区別可能
/// - パフォーマンスのため、毎フレームの処理は軽量に保つ
/// </remarks>
public class PlayerDetector : UdonSharpBehaviour
{
    /// <summary>
    /// プレイヤー検出範囲（メートル）
    /// この距離以内のプレイヤーを検出対象とする
    /// </summary>
    [Header("検出設定")]
    [SerializeField] private float detectionRange = 10f;

    /// <summary>
    /// 色変化の最小距離（メートル）
    /// この距離で最も赤くなる
    /// </summary>
    [SerializeField] private float minDistance = 1f;

    /// <summary>
    /// 色変化の最大距離（メートル）
    /// この距離で最も青くなる
    /// </summary>
    [SerializeField] private float maxDistance = 5f;

    /// <summary>
    /// プレイヤー名を表示するテキストUI（オプション）
    /// 設定されていない場合は名前表示をスキップ
    /// </summary>
    [Header("UI設定")]
    [SerializeField] private Text playerNameText;

    /// <summary>
    /// デバッグログを表示するかどうか
    /// 開発中はtrueにして動作を確認
    /// </summary>
    [Header("デバッグ")]
    [SerializeField] private bool showDebugInfo = true;

    // ========== プライベート変数 ==========
    
    /// <summary>
    /// このオブジェクトのRenderer（色変更用）
    /// </summary>
    private Renderer objectRenderer;

    /// <summary>
    /// 元のマテリアルの色（リセット用）
    /// </summary>
    private Color originalColor;

    /// <summary>
    /// 現在最も近いプレイヤーへの距離
    /// </summary>
    private float currentDistance = float.MaxValue;

    /// <summary>
    /// 初期化処理
    /// Rendererの取得と元の色の保存を行う
    /// </summary>
    private void Start()
    {
        // Rendererコンポーネントを取得
        // なぜ必要か：オブジェクトの色を変更するため
        objectRenderer = GetComponent<Renderer>();
        
        if (objectRenderer != null)
        {
            // 元の色を保存
            // なぜ必要か：プレイヤーが離れた時に元の色に戻すため
            originalColor = objectRenderer.material.color;
        }
        else
        {
            Debug.LogError("[PlayerDetector] Rendererが見つかりません。色の変更ができません。");
        }

        // テキストUIの初期化
        if (playerNameText != null)
        {
            playerNameText.text = "プレイヤーを検出中...";
        }
    }

    /// <summary>
    /// 毎フレーム実行される更新処理
    /// プレイヤーの検出と状態更新を行う
    /// </summary>
    private void Update()
    {
        // 最も近いプレイヤーを検出
        VRCPlayerApi nearestPlayer = FindNearestPlayer();
        
        if (nearestPlayer != null)
        {
            // プレイヤーが見つかった場合の処理
            ProcessNearestPlayer(nearestPlayer);
        }
        else
        {
            // プレイヤーが見つからない場合の処理
            ResetToDefault();
        }
    }

    /// <summary>
    /// 最も近いプレイヤーを検出する
    /// </summary>
    /// <returns>最も近いプレイヤーのAPI。見つからない場合はnull</returns>
    private VRCPlayerApi FindNearestPlayer()
    {
        VRCPlayerApi nearestPlayer = null;
        float nearestDistance = detectionRange;

        // ========== 全プレイヤーをチェック ==========
        // VRChatのGetPlayers()は現在ワールドにいる全プレイヤーのリストを返す
        VRCPlayerApi[] players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);

        foreach (VRCPlayerApi player in players)
        {
            // プレイヤーが有効かチェック
            // なぜ必要か：プレイヤーが退出中などの場合、無効になることがある
            if (!Utilities.IsValid(player))
            {
                continue;
            }

            // プレイヤーとの距離を計算
            float distance = Vector3.Distance(
                transform.position,
                player.GetPosition()
            );

            // より近いプレイヤーを記録
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlayer = player;
                currentDistance = distance;
            }
        }

        return nearestPlayer;
    }

    /// <summary>
    /// 最も近いプレイヤーに対する処理
    /// 色の変更と名前の表示を行う
    /// </summary>
    /// <param name="player">処理対象のプレイヤー</param>
    private void ProcessNearestPlayer(VRCPlayerApi player)
    {
        // ========== 色の変更 ==========
        if (objectRenderer != null)
        {
            // 距離を0-1の範囲に正規化
            // Mathf.InverseLerpは値を指定範囲内での割合に変換する
            float normalizedDistance = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);
            
            // 色を補間（近い：赤、遠い：青）
            // Color.Lerpは2つの色を指定の割合で混ぜる
            Color targetColor = Color.Lerp(Color.red, Color.blue, normalizedDistance);
            
            // 色を適用
            objectRenderer.material.color = targetColor;
        }

        // ========== プレイヤー名の表示 ==========
        if (playerNameText != null)
        {
            string playerInfo = $"プレイヤー: {player.displayName}\n距離: {currentDistance:F1}m";
            
            // ローカルプレイヤー（自分）の場合は特別な表示
            if (player.isLocal)
            {
                playerInfo += " (あなた)";
            }
            
            playerNameText.text = playerInfo;
        }

        // ========== デバッグ情報 ==========
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerDetector] 最寄りプレイヤー: {player.displayName}, 距離: {currentDistance:F1}m");
        }
    }

    /// <summary>
    /// プレイヤーが検出範囲内にいない時の処理
    /// 色とテキストを初期状態に戻す
    /// </summary>
    private void ResetToDefault()
    {
        // 色を元に戻す
        if (objectRenderer != null)
        {
            objectRenderer.material.color = originalColor;
        }

        // テキストをリセット
        if (playerNameText != null)
        {
            playerNameText.text = "プレイヤーが検出範囲内にいません";
        }

        currentDistance = float.MaxValue;
    }

    /// <summary>
    /// インスペクターで値が変更された時の処理
    /// 範囲の妥当性をチェック
    /// </summary>
    private void OnValidate()
    {
        // 最小距離は0以上
        minDistance = Mathf.Max(0f, minDistance);
        
        // 最大距離は最小距離より大きく
        maxDistance = Mathf.Max(minDistance + 0.1f, maxDistance);
        
        // 検出範囲は最大距離以上
        detectionRange = Mathf.Max(maxDistance, detectionRange);
    }
}