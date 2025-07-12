using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// VRChatTextCreatorで作成したTextMeshProオブジェクトを制御するスクリプト
/// プレイヤーとのインタラクションに応じてテキストを動的に変更
/// </summary>
/// <remarks>
/// 【このスクリプトの目的】
/// VRChatTextCreatorで作成したテキストをゲーム内で動的に制御する方法を提供します。
/// プレイヤーのアクションに応じてテキストの内容、色、サイズなどを変更できます。
/// 
/// 【主な機能】
/// 1. インタラクトによるテキスト変更
/// 2. プレイヤー情報の表示
/// 3. 時間経過によるアニメーション
/// 4. 複数のテキストモード切り替え
/// 
/// 【使い方】
/// 1. VRChatTextCreatorでテキストオブジェクトを作成
/// 2. このスクリプトを同じGameObjectに追加
/// 3. インスペクターで設定を調整
/// </remarks>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class VRChatTextController : UdonSharpBehaviour
{
    /// <summary>
    /// 制御対象のTextMeshProコンポーネント
    /// 同じGameObjectに自動的にアタッチされている想定
    /// </summary>
    private TextMeshPro textMesh;
    
    /// <summary>
    /// テキストの表示モード
    /// </summary>
    public enum TextMode
    {
        Welcome,        // ウェルカムメッセージ
        PlayerInfo,     // プレイヤー情報表示
        WorldInfo,      // ワールド情報表示
        Custom         // カスタムメッセージ
    }
    
    /// <summary>
    /// 現在の表示モード
    /// </summary>
    [Header("表示設定")]
    [SerializeField] private TextMode currentMode = TextMode.Welcome;
    
    /// <summary>
    /// カスタムメッセージ（CustomMode時に使用）
    /// </summary>
    [SerializeField] private string customMessage = "カスタムメッセージ";
    
    /// <summary>
    /// テキストアニメーションを有効にするか
    /// </summary>
    [Header("アニメーション設定")]
    [SerializeField] private bool enableAnimation = true;
    
    /// <summary>
    /// アニメーション速度
    /// </summary>
    [SerializeField] private float animationSpeed = 1f;
    
    /// <summary>
    /// 色のグラデーション設定
    /// </summary>
    [SerializeField] private Gradient colorGradient;
    
    /// <summary>
    /// インタラクトでモードを切り替えるか
    /// </summary>
    [Header("インタラクション設定")]
    [SerializeField] private bool allowModeSwitch = true;
    
    /// <summary>
    /// ネットワーク同期する現在のモード
    /// </summary>
    [UdonSynced]
    private int syncedMode = 0;
    
    /// <summary>
    /// アニメーション用のタイマー
    /// </summary>
    private float animationTimer = 0f;
    
    /// <summary>
    /// 初期化処理
    /// </summary>
    void Start()
    {
        // TextMeshProコンポーネントを取得
        textMesh = GetComponent<TextMeshPro>();
        
        if (textMesh == null)
        {
            Debug.LogError("[VRChatTextController] TextMeshProコンポーネントが見つかりません");
            return;
        }
        
        // カラーグラデーションが設定されていない場合はデフォルトを作成
        if (colorGradient == null || colorGradient.colorKeys.Length == 0)
        {
            colorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[3];
            colorKeys[0] = new GradientColorKey(Color.red, 0.0f);
            colorKeys[1] = new GradientColorKey(Color.yellow, 0.5f);
            colorKeys[2] = new GradientColorKey(Color.green, 1.0f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
            alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);
            
            colorGradient.SetKeys(colorKeys, alphaKeys);
        }
        
        // 初期テキストを更新
        UpdateText();
    }
    
    /// <summary>
    /// 毎フレーム実行される更新処理
    /// </summary>
    void Update()
    {
        if (textMesh == null) return;
        
        // アニメーション処理
        if (enableAnimation)
        {
            animationTimer += Time.deltaTime * animationSpeed;
            if (animationTimer > 1f) animationTimer -= 1f;
            
            // 色をグラデーションで変更
            textMesh.color = colorGradient.Evaluate(animationTimer);
            
            // サイズを波のように変化させる
            float sizeMultiplier = 1f + Mathf.Sin(animationTimer * Mathf.PI * 2f) * 0.1f;
            textMesh.fontSize = textMesh.fontSize * sizeMultiplier;
        }
        
        // 定期的にテキストを更新（プレイヤー情報などが変わる可能性があるため）
        if (currentMode == TextMode.PlayerInfo || currentMode == TextMode.WorldInfo)
        {
            UpdateText();
        }
    }
    
    /// <summary>
    /// インタラクト時の処理
    /// モードを切り替える
    /// </summary>
    public override void Interact()
    {
        if (!allowModeSwitch) return;
        
        // 次のモードに切り替え
        int nextMode = ((int)currentMode + 1) % System.Enum.GetValues(typeof(TextMode)).Length;
        
        // オーナーシップを取得してから同期変数を更新
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        
        syncedMode = nextMode;
        currentMode = (TextMode)nextMode;
        
        // 同期をリクエスト
        RequestSerialization();
        
        // テキストを更新
        UpdateText();
        
        // フィードバック
        Debug.Log($"[VRChatTextController] モードを {currentMode} に切り替えました");
    }
    
    /// <summary>
    /// ネットワーク同期時の処理
    /// </summary>
    public override void OnDeserialization()
    {
        currentMode = (TextMode)syncedMode;
        UpdateText();
    }
    
    /// <summary>
    /// 現在のモードに応じてテキストを更新
    /// </summary>
    private void UpdateText()
    {
        if (textMesh == null) return;
        
        switch (currentMode)
        {
            case TextMode.Welcome:
                ShowWelcomeMessage();
                break;
                
            case TextMode.PlayerInfo:
                ShowPlayerInfo();
                break;
                
            case TextMode.WorldInfo:
                ShowWorldInfo();
                break;
                
            case TextMode.Custom:
                textMesh.text = customMessage;
                break;
        }
    }
    
    /// <summary>
    /// ウェルカムメッセージを表示
    /// </summary>
    private void ShowWelcomeMessage()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (Utilities.IsValid(localPlayer))
        {
            textMesh.text = $"ようこそ、{localPlayer.displayName}さん！\nこのワールドをお楽しみください！";
        }
        else
        {
            textMesh.text = "VRChatワールドへようこそ！";
        }
    }
    
    /// <summary>
    /// プレイヤー情報を表示
    /// </summary>
    private void ShowPlayerInfo()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (!Utilities.IsValid(localPlayer))
        {
            textMesh.text = "プレイヤー情報を取得中...";
            return;
        }
        
        string info = $"プレイヤー名: {localPlayer.displayName}\n";
        info += $"プレイヤーID: {localPlayer.playerId}\n";
        info += $"マスター: {(localPlayer.isMaster ? "はい" : "いいえ")}\n";
        info += $"VR使用: {(localPlayer.IsUserInVR() ? "はい" : "いいえ")}";
        
        textMesh.text = info;
    }
    
    /// <summary>
    /// ワールド情報を表示
    /// </summary>
    private void ShowWorldInfo()
    {
        int playerCount = VRCPlayerApi.GetPlayerCount();
        string masterName = "不明";
        
        // マスターを探す
        VRCPlayerApi[] players = new VRCPlayerApi[playerCount];
        VRCPlayerApi.GetPlayers(players);
        
        foreach (var player in players)
        {
            if (Utilities.IsValid(player) && player.isMaster)
            {
                masterName = player.displayName;
                break;
            }
        }
        
        string info = $"ワールド情報\n";
        info += $"プレイヤー数: {playerCount}人\n";
        info += $"インスタンスマスター: {masterName}\n";
        info += $"現在時刻: {System.DateTime.Now:HH:mm:ss}";
        
        textMesh.text = info;
    }
    
    /// <summary>
    /// 外部からテキストを設定するメソッド
    /// 他のスクリプトから呼び出し可能
    /// </summary>
    /// <param name="newText">設定するテキスト</param>
    public void SetCustomText(string newText)
    {
        customMessage = newText;
        currentMode = TextMode.Custom;
        UpdateText();
    }
    
    /// <summary>
    /// 外部からモードを設定するメソッド
    /// </summary>
    /// <param name="mode">設定するモード</param>
    public void SetMode(TextMode mode)
    {
        currentMode = mode;
        syncedMode = (int)mode;
        UpdateText();
        
        if (Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }
}