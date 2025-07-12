using UnityEngine;
using UnityEditor;
using TMPro;

/// <summary>
/// VRChatワールドでテキストオブジェクトを簡単に作成するためのエディタ拡張
/// VRChatでは3Dテキストの表示にTextMeshProを使用するのが一般的
/// </summary>
/// <remarks>
/// 使用理由：
/// - VRChatではTextMeshProがパフォーマンスと見た目の面で推奨される
/// - エディタ拡張により、繰り返し作業を効率化できる
/// - VRChat向けの最適な設定を自動的に適用できる
/// 
/// 主な機能：
/// 1. TextMeshProオブジェクトの自動生成
/// 2. VRChat向けの最適な設定の適用
/// 3. 日本語対応フォントの自動設定
/// 4. 影やアウトラインなどの視認性向上オプション
/// </remarks>
public class VRChatTextCreator : EditorWindow
{
    // UI入力フィールド用の変数
    /// <summary>
    /// 作成するテキストの内容
    /// ユーザーが入力したテキストがそのまま3D空間に表示される
    /// </summary>
    private string textContent = "Hello VRChat!";

    /// <summary>
    /// テキストのフォントサイズ（単位：Unity単位）
    /// VRChatでは5～10程度が標準的なサイズ
    /// </summary>
    private float fontSize = 5f;

    /// <summary>
    /// テキストの色
    /// 白色がデフォルトで、暗い背景でも明るい背景でも見やすい
    /// </summary>
    private Color textColor = Color.white;

    /// <summary>
    /// 影を追加するかどうか
    /// 影があることでテキストの視認性が向上する
    /// </summary>
    private bool addShadow = true;

    /// <summary>
    /// アウトライン（輪郭線）を追加するかどうか
    /// 背景と同化しないようにテキストを際立たせる
    /// </summary>
    private bool addOutline = false;

    /// <summary>
    /// テキストの配置方法
    /// 中央揃え、左揃え、右揃えなどが選択できる
    /// </summary>
    private TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;

    /// <summary>
    /// テキストをワールド空間に固定するか、それともUIとして扱うか
    /// VRChatではワールド空間配置が一般的
    /// </summary>
    private bool worldSpace = true;

    /// <summary>
    /// 両面から見えるようにするかどうか
    /// trueの場合、テキストの裏側からも文字が見える
    /// </summary>
    private bool doubleSided = false;

    /// <summary>
    /// エディタウィンドウを開くためのメニュー項目を追加
    /// Unity上部のToolsメニューから呼び出せる
    /// </summary>
    [MenuItem("Tools/VRChat Text Creator")]
    public static void ShowWindow()
    {
        // ウィンドウのインスタンスを作成して表示
        VRChatTextCreator window = GetWindow<VRChatTextCreator>("VRChat Text Creator");
        window.minSize = new Vector2(300, 400);
    }

    /// <summary>
    /// エディタウィンドウのUI描画処理
    /// ユーザーが設定を入力するためのインターフェース
    /// </summary>
    void OnGUI()
    {
        // タイトルとヘルプテキスト
        GUILayout.Label("VRChat Text Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "VRChatワールドで表示可能なTextMeshProオブジェクトを作成します。\n" +
            "日本語を使用する場合は、適切な日本語フォントを設定してください。",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // 基本設定セクション
        GUILayout.Label("基本設定", EditorStyles.boldLabel);

        // テキスト内容の入力フィールド
        // 複数行対応で、改行も可能
        EditorGUILayout.LabelField("表示するテキスト:");
        textContent = EditorGUILayout.TextArea(textContent, GUILayout.Height(60));

        // フォントサイズの設定
        // スライダーで直感的に調整可能
        fontSize = EditorGUILayout.Slider("フォントサイズ", fontSize, 0.1f, 20f);

        // テキストカラーの選択
        textColor = EditorGUILayout.ColorField("テキストカラー", textColor);

        // テキスト配置の選択
        textAlignment = (TextAlignmentOptions)EditorGUILayout.EnumPopup("テキスト配置", textAlignment);

        EditorGUILayout.Space();

        // 視覚効果セクション
        GUILayout.Label("視覚効果", EditorStyles.boldLabel);

        // 各種エフェクトのトグル
        addShadow = EditorGUILayout.Toggle("影を追加", addShadow);
        addOutline = EditorGUILayout.Toggle("アウトラインを追加", addOutline);

        EditorGUILayout.Space();

        // 配置設定セクション
        GUILayout.Label("配置設定", EditorStyles.boldLabel);

        worldSpace = EditorGUILayout.Toggle("ワールド空間に配置", worldSpace);
        doubleSided = EditorGUILayout.Toggle("両面表示", doubleSided);

        EditorGUILayout.Space();

        // 作成ボタン
        // 大きめのボタンで押しやすく
        if (GUILayout.Button("テキストを作成", GUILayout.Height(30)))
        {
            CreateVRChatText();
        }

        EditorGUILayout.Space();

        // 追加のヘルプ情報
        EditorGUILayout.HelpBox(
            "作成されたテキストは選択中のオブジェクトの子として配置されます。\n" +
            "何も選択していない場合は、シーンのルートに配置されます。",
            MessageType.None
        );
    }

    /// <summary>
    /// 実際にTextMeshProオブジェクトを作成する処理
    /// ユーザーが設定した内容に基づいてオブジェクトを生成
    /// </summary>
    private void CreateVRChatText()
    {
        // 新しいGameObjectを作成
        // 名前にはテキストの最初の部分を使用（最大20文字）
        string objectName = "VRChatText_" + (textContent.Length > 20 ? textContent.Substring(0, 20) + "..." : textContent);
        GameObject textObject = new GameObject(objectName);

        // 親オブジェクトの設定
        // 選択中のオブジェクトがあればその子として配置
        if (Selection.activeGameObject != null)
        {
            textObject.transform.SetParent(Selection.activeGameObject.transform);
        }

        // TextMeshProコンポーネントを追加
        TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();

        // 基本設定の適用
        textMesh.text = textContent;
        textMesh.fontSize = fontSize;
        textMesh.color = textColor;
        textMesh.alignment = textAlignment;

        // レンダリング設定
        // VRChatでの視認性を考慮した設定
        textMesh.enableWordWrapping = true;  // 長いテキストの自動折り返し
        textMesh.overflowMode = TextOverflowModes.Overflow;  // オーバーフローを許可

        // フォントアセットの設定
        // TextMeshProのデフォルトフォントを使用
        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont != null)
        {
            textMesh.font = defaultFont;
        }

        // マテリアル設定
        if (textMesh.fontSharedMaterial != null)
        {
            // マテリアルのコピーを作成して個別に編集可能に
            Material mat = new Material(textMesh.fontSharedMaterial);

            // 影の設定
            if (addShadow)
            {
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetFloat("_UnderlayOffsetX", 1f);
                mat.SetFloat("_UnderlayOffsetY", -1f);
                mat.SetFloat("_UnderlayDilate", 0.1f);
                mat.SetFloat("_UnderlaySoftness", 0.1f);
            }

            // アウトラインの設定
            if (addOutline)
            {
                mat.EnableKeyword("OUTLINE_ON");
                mat.SetFloat("_OutlineWidth", 0.2f);
                mat.SetColor("_OutlineColor", Color.black);
            }

            // 両面表示の設定
            if (doubleSided)
            {
                mat.SetFloat("_CullMode", 0);  // カリングを無効化
            }

            textMesh.fontSharedMaterial = mat;
        }

        // RectTransformの設定
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // サイズの自動調整を有効化
            rectTransform.sizeDelta = new Vector2(10, 5);
        }

        // オブジェクトを選択状態にする
        Selection.activeGameObject = textObject;

        // Undoに登録（Ctrl+Zで取り消し可能に）
        Undo.RegisterCreatedObjectUndo(textObject, "Create VRChat Text");

        // 作成完了メッセージ
        Debug.Log($"VRChatテキスト '{objectName}' を作成しました。");
    }
}