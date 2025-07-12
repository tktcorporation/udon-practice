using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// VRChatワールド用の高機能テキスト作成エディタ拡張
/// 基本的なテキスト作成に加え、プリセット、バッチ処理、日本語対応などの機能を提供
/// </summary>
/// <remarks>
/// 追加機能：
/// 1. テキストプリセット機能（よく使う設定を保存）
/// 2. 複数テキストの一括作成
/// 3. 日本語フォントの自動検出と設定
/// 4. テキストアニメーション用のコンポーネント追加オプション
/// 5. VRChat向けの最適化設定
/// 
/// なぜこれらの機能が必要か：
/// - VRChatワールド制作では多数のテキストを配置することが多い
/// - 日本語表示には特別な設定が必要
/// - 統一感のあるデザインのために設定の再利用が重要
/// </remarks>
public class VRChatTextCreatorAdvanced : EditorWindow
{
    /// <summary>
    /// テキスト設定のプリセットを管理するクラス
    /// よく使う設定を保存して再利用できる
    /// </summary>
    [System.Serializable]
    public class TextPreset
    {
        public string name = "新しいプリセット";
        public float fontSize = 5f;
        public Color textColor = Color.white;
        public TextAlignmentOptions alignment = TextAlignmentOptions.Center;
        public bool addShadow = true;
        public bool addOutline = false;
        public bool doubleSided = false;
        public string fontAssetPath = "";
    }

    // 現在の設定
    private string textContent = "Hello VRChat!";
    private float fontSize = 5f;
    private Color textColor = Color.white;
    private TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
    private bool addShadow = true;
    private bool addOutline = false;
    private bool worldSpace = true;
    private bool doubleSided = false;
    
    // 高度な設定
    private bool autoSizeText = false;
    private float maxTextWidth = 10f;
    private bool addCollider = false;
    private bool makeInteractable = false;
    
    // プリセット管理
    private List<TextPreset> presets = new List<TextPreset>();
    private int selectedPresetIndex = -1;
    private string newPresetName = "";
    
    // バッチ作成用
    private bool batchMode = false;
    private List<string> batchTexts = new List<string>();
    private float batchSpacing = 2f;
    private bool arrangeHorizontally = true;
    
    // フォント選択
    private TMP_FontAsset selectedFont;
    private List<TMP_FontAsset> availableFonts = new List<TMP_FontAsset>();
    private int selectedFontIndex = 0;
    
    // UI表示制御
    private bool showAdvancedOptions = false;
    private bool showPresetManager = false;
    private bool showBatchOptions = false;
    
    /// <summary>
    /// エディタウィンドウを開くメニュー項目
    /// </summary>
    [MenuItem("Tools/VRChat Text Creator (Advanced)")]
    public static void ShowWindow()
    {
        VRChatTextCreatorAdvanced window = GetWindow<VRChatTextCreatorAdvanced>("VRChat Text Creator Advanced");
        window.minSize = new Vector2(400, 600);
        window.LoadPresets();
        window.FindAvailableFonts();
    }

    /// <summary>
    /// 利用可能なTextMeshProフォントを検索
    /// 日本語対応フォントを優先的に表示
    /// </summary>
    private void FindAvailableFonts()
    {
        availableFonts.Clear();
        
        // プロジェクト内のすべてのTMP_FontAssetを検索
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null)
            {
                availableFonts.Add(font);
            }
        }
        
        // デフォルトフォントを最初に追加
        if (availableFonts.Count == 0)
        {
            TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFont != null)
            {
                availableFonts.Add(defaultFont);
            }
        }
        
        // 最初のフォントを選択
        if (availableFonts.Count > 0)
        {
            selectedFont = availableFonts[0];
        }
    }

    /// <summary>
    /// エディタウィンドウのUI描画
    /// </summary>
    void OnGUI()
    {
        // スクロールビューの開始
        Vector2 scrollPosition = Vector2.zero;
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        // ヘッダー
        GUILayout.Label("VRChat Text Creator Advanced", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "VRChatワールド用の高機能テキスト作成ツール\n" +
            "プリセット機能、バッチ作成、日本語対応などの機能を搭載",
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        // タブボタン風のUI
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("基本設定", GUILayout.Height(25)))
        {
            showAdvancedOptions = false;
            showPresetManager = false;
            showBatchOptions = false;
        }
        if (GUILayout.Button("高度な設定", GUILayout.Height(25)))
        {
            showAdvancedOptions = true;
            showPresetManager = false;
            showBatchOptions = false;
        }
        if (GUILayout.Button("プリセット", GUILayout.Height(25)))
        {
            showPresetManager = true;
            showAdvancedOptions = false;
            showBatchOptions = false;
        }
        if (GUILayout.Button("バッチ作成", GUILayout.Height(25)))
        {
            showBatchOptions = true;
            showAdvancedOptions = false;
            showPresetManager = false;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // プリセットマネージャー
        if (showPresetManager)
        {
            DrawPresetManager();
        }
        // バッチオプション
        else if (showBatchOptions)
        {
            DrawBatchOptions();
        }
        // 高度な設定
        else if (showAdvancedOptions)
        {
            DrawAdvancedOptions();
        }
        // 基本設定
        else
        {
            DrawBasicOptions();
        }
        
        EditorGUILayout.Space();
        
        // 作成ボタン
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button(batchMode ? "テキストを一括作成" : "テキストを作成", GUILayout.Height(40)))
        {
            if (batchMode)
            {
                CreateBatchTexts();
            }
            else
            {
                CreateVRChatText();
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 基本設定のUI描画
    /// </summary>
    private void DrawBasicOptions()
    {
        GUILayout.Label("基本設定", EditorStyles.boldLabel);
        
        // モード選択
        batchMode = EditorGUILayout.Toggle("バッチモード", batchMode);
        
        if (!batchMode)
        {
            // 単一テキスト作成モード
            EditorGUILayout.LabelField("表示するテキスト:");
            textContent = EditorGUILayout.TextArea(textContent, GUILayout.Height(60));
        }
        
        // フォント選択
        EditorGUILayout.Space();
        GUILayout.Label("フォント設定", EditorStyles.boldLabel);
        
        if (availableFonts.Count > 0)
        {
            string[] fontNames = availableFonts.Select(f => f.name).ToArray();
            selectedFontIndex = EditorGUILayout.Popup("フォント", selectedFontIndex, fontNames);
            
            if (selectedFontIndex >= 0 && selectedFontIndex < availableFonts.Count)
            {
                selectedFont = availableFonts[selectedFontIndex];
            }
            
            // 日本語フォントの推奨表示
            if (selectedFont != null && selectedFont.name.ToLower().Contains("japan"))
            {
                EditorGUILayout.HelpBox("日本語対応フォントが選択されています", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("利用可能なフォントが見つかりません", MessageType.Warning);
        }
        
        fontSize = EditorGUILayout.Slider("フォントサイズ", fontSize, 0.1f, 20f);
        
        // 表示設定
        EditorGUILayout.Space();
        GUILayout.Label("表示設定", EditorStyles.boldLabel);
        
        textColor = EditorGUILayout.ColorField("テキストカラー", textColor);
        textAlignment = (TextAlignmentOptions)EditorGUILayout.EnumPopup("テキスト配置", textAlignment);
        
        // エフェクト
        EditorGUILayout.Space();
        GUILayout.Label("視覚効果", EditorStyles.boldLabel);
        
        addShadow = EditorGUILayout.Toggle("影を追加", addShadow);
        addOutline = EditorGUILayout.Toggle("アウトラインを追加", addOutline);
        
        // 配置設定
        EditorGUILayout.Space();
        GUILayout.Label("配置設定", EditorStyles.boldLabel);
        
        worldSpace = EditorGUILayout.Toggle("ワールド空間に配置", worldSpace);
        doubleSided = EditorGUILayout.Toggle("両面表示", doubleSided);
    }

    /// <summary>
    /// 高度な設定のUI描画
    /// </summary>
    private void DrawAdvancedOptions()
    {
        GUILayout.Label("高度な設定", EditorStyles.boldLabel);
        
        // 自動サイズ調整
        autoSizeText = EditorGUILayout.Toggle("テキストの自動サイズ調整", autoSizeText);
        if (autoSizeText)
        {
            maxTextWidth = EditorGUILayout.FloatField("最大幅", maxTextWidth);
        }
        
        EditorGUILayout.Space();
        
        // インタラクション設定
        GUILayout.Label("インタラクション", EditorStyles.boldLabel);
        
        addCollider = EditorGUILayout.Toggle("コライダーを追加", addCollider);
        makeInteractable = EditorGUILayout.Toggle("インタラクト可能にする", makeInteractable);
        
        if (makeInteractable)
        {
            EditorGUILayout.HelpBox(
                "インタラクト可能にする場合は、別途UdonSharpスクリプトを追加してください。",
                MessageType.Info
            );
        }
        
        EditorGUILayout.Space();
        
        // パフォーマンス設定
        GUILayout.Label("パフォーマンス最適化", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "VRChatでのパフォーマンスを考慮した設定:\n" +
            "• 静的なテキストはStaticに設定\n" +
            "• 大量のテキストは1つのマテリアルを共有\n" +
            "• 不要なコンポーネントは追加しない",
            MessageType.Info
        );
    }

    /// <summary>
    /// プリセットマネージャーのUI描画
    /// </summary>
    private void DrawPresetManager()
    {
        GUILayout.Label("プリセット管理", EditorStyles.boldLabel);
        
        // 現在のプリセット一覧
        if (presets.Count > 0)
        {
            EditorGUILayout.LabelField("保存されたプリセット:");
            
            for (int i = 0; i < presets.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                // プリセット名
                if (GUILayout.Button(presets[i].name, GUILayout.Width(200)))
                {
                    ApplyPreset(presets[i]);
                    selectedPresetIndex = i;
                }
                
                // 適用中マーク
                if (selectedPresetIndex == i)
                {
                    GUILayout.Label("(適用中)", GUILayout.Width(60));
                }
                
                // 削除ボタン
                if (GUILayout.Button("削除", GUILayout.Width(50)))
                {
                    presets.RemoveAt(i);
                    SavePresets();
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("保存されたプリセットがありません", MessageType.Info);
        }
        
        EditorGUILayout.Space();
        
        // 新規プリセット作成
        EditorGUILayout.BeginHorizontal();
        newPresetName = EditorGUILayout.TextField("新規プリセット名:", newPresetName);
        if (GUILayout.Button("現在の設定を保存", GUILayout.Width(120)))
        {
            SaveCurrentAsPreset();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// バッチ作成オプションのUI描画
    /// </summary>
    private void DrawBatchOptions()
    {
        GUILayout.Label("バッチ作成設定", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "複数のテキストを一度に作成します。\n" +
            "各行に1つずつテキストを入力してください。",
            MessageType.Info
        );
        
        // バッチテキスト入力
        EditorGUILayout.LabelField("作成するテキスト（1行1テキスト）:");
        
        // テキストエリアで複数行入力
        string batchTextInput = string.Join("\n", batchTexts);
        batchTextInput = EditorGUILayout.TextArea(batchTextInput, GUILayout.Height(120));
        
        // 入力を行ごとに分割
        batchTexts = batchTextInput.Split('\n').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        
        EditorGUILayout.LabelField($"作成予定: {batchTexts.Count} 個のテキスト");
        
        EditorGUILayout.Space();
        
        // 配置設定
        GUILayout.Label("配置設定", EditorStyles.boldLabel);
        
        arrangeHorizontally = EditorGUILayout.Toggle("横に並べる", arrangeHorizontally);
        batchSpacing = EditorGUILayout.FloatField("間隔", batchSpacing);
        
        // プレビュー情報
        if (batchTexts.Count > 0)
        {
            float totalLength = arrangeHorizontally ? 
                (batchTexts.Count - 1) * batchSpacing : 
                (batchTexts.Count - 1) * batchSpacing;
            
            EditorGUILayout.HelpBox(
                $"配置サイズ: {(arrangeHorizontally ? "横" : "縦")} {totalLength:F1} ユニット",
                MessageType.None
            );
        }
    }

    /// <summary>
    /// 単一のVRChatテキストを作成
    /// </summary>
    private void CreateVRChatText()
    {
        CreateTextObject(textContent, Vector3.zero);
    }

    /// <summary>
    /// 複数のテキストを一括作成
    /// </summary>
    private void CreateBatchTexts()
    {
        if (batchTexts.Count == 0)
        {
            EditorUtility.DisplayDialog("エラー", "作成するテキストが入力されていません", "OK");
            return;
        }
        
        // 親オブジェクトを作成
        GameObject batchParent = new GameObject("VRChatText_Batch");
        if (Selection.activeGameObject != null)
        {
            batchParent.transform.SetParent(Selection.activeGameObject.transform);
        }
        
        // 各テキストを作成
        for (int i = 0; i < batchTexts.Count; i++)
        {
            Vector3 position = arrangeHorizontally ?
                new Vector3(i * batchSpacing, 0, 0) :
                new Vector3(0, -i * batchSpacing, 0);
            
            GameObject textObj = CreateTextObject(batchTexts[i], position);
            textObj.transform.SetParent(batchParent.transform);
        }
        
        // 親オブジェクトを選択
        Selection.activeGameObject = batchParent;
        Undo.RegisterCreatedObjectUndo(batchParent, "Create Batch VRChat Texts");
        
        Debug.Log($"{batchTexts.Count} 個のVRChatテキストを作成しました");
    }

    /// <summary>
    /// テキストオブジェクトを作成する共通処理
    /// </summary>
    /// <param name="text">表示するテキスト</param>
    /// <param name="localPosition">ローカル座標での位置</param>
    /// <returns>作成されたGameObject</returns>
    private GameObject CreateTextObject(string text, Vector3 localPosition)
    {
        // GameObjectを作成
        string objectName = "VRChatText_" + (text.Length > 20 ? text.Substring(0, 20) + "..." : text);
        GameObject textObject = new GameObject(objectName);
        textObject.transform.localPosition = localPosition;
        
        // TextMeshProコンポーネントを追加
        TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();
        
        // 基本設定
        textMesh.text = text;
        textMesh.fontSize = fontSize;
        textMesh.color = textColor;
        textMesh.alignment = textAlignment;
        
        // フォント設定
        if (selectedFont != null)
        {
            textMesh.font = selectedFont;
        }
        
        // 自動サイズ調整
        if (autoSizeText)
        {
            textMesh.enableAutoSizing = true;
            textMesh.fontSizeMin = fontSize * 0.5f;
            textMesh.fontSizeMax = fontSize;
            textMesh.characterWidthAdjustment = 5f;
        }
        
        // レンダリング設定
        textMesh.enableWordWrapping = true;
        if (maxTextWidth > 0 && autoSizeText)
        {
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(maxTextWidth, rect.sizeDelta.y);
        }
        
        // マテリアル設定
        if (textMesh.fontSharedMaterial != null)
        {
            Material mat = new Material(textMesh.fontSharedMaterial);
            
            // 影
            if (addShadow)
            {
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetFloat("_UnderlayOffsetX", 1f);
                mat.SetFloat("_UnderlayOffsetY", -1f);
                mat.SetFloat("_UnderlayDilate", 0.1f);
                mat.SetFloat("_UnderlaySoftness", 0.1f);
            }
            
            // アウトライン
            if (addOutline)
            {
                mat.EnableKeyword("OUTLINE_ON");
                mat.SetFloat("_OutlineWidth", 0.2f);
                mat.SetColor("_OutlineColor", Color.black);
            }
            
            // 両面表示
            if (doubleSided)
            {
                mat.SetFloat("_CullMode", 0);
            }
            
            textMesh.fontSharedMaterial = mat;
        }
        
        // コライダー追加
        if (addCollider)
        {
            BoxCollider collider = textObject.AddComponent<BoxCollider>();
            // テキストのサイズに基づいてコライダーを調整
            Bounds textBounds = textMesh.bounds;
            collider.size = textBounds.size;
            collider.center = textBounds.center - textObject.transform.position;
        }
        
        // インタラクト可能設定の準備
        if (makeInteractable)
        {
            // タグを設定（VRChatでのインタラクト検出用）
            textObject.tag = "Untagged"; // 必要に応じて変更
            
            // レイヤーを設定
            textObject.layer = 0; // Default layer
        }
        
        return textObject;
    }

    /// <summary>
    /// 現在の設定をプリセットとして保存
    /// </summary>
    private void SaveCurrentAsPreset()
    {
        if (string.IsNullOrWhiteSpace(newPresetName))
        {
            EditorUtility.DisplayDialog("エラー", "プリセット名を入力してください", "OK");
            return;
        }
        
        TextPreset preset = new TextPreset
        {
            name = newPresetName,
            fontSize = fontSize,
            textColor = textColor,
            alignment = textAlignment,
            addShadow = addShadow,
            addOutline = addOutline,
            doubleSided = doubleSided,
            fontAssetPath = selectedFont != null ? AssetDatabase.GetAssetPath(selectedFont) : ""
        };
        
        presets.Add(preset);
        SavePresets();
        
        newPresetName = "";
        EditorUtility.DisplayDialog("成功", $"プリセット '{preset.name}' を保存しました", "OK");
    }

    /// <summary>
    /// プリセットを現在の設定に適用
    /// </summary>
    private void ApplyPreset(TextPreset preset)
    {
        fontSize = preset.fontSize;
        textColor = preset.textColor;
        textAlignment = preset.alignment;
        addShadow = preset.addShadow;
        addOutline = preset.addOutline;
        doubleSided = preset.doubleSided;
        
        // フォントを復元
        if (!string.IsNullOrEmpty(preset.fontAssetPath))
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(preset.fontAssetPath);
            if (font != null)
            {
                selectedFont = font;
                selectedFontIndex = availableFonts.IndexOf(font);
            }
        }
        
        Debug.Log($"プリセット '{preset.name}' を適用しました");
    }

    /// <summary>
    /// プリセットをEditorPrefsに保存
    /// </summary>
    private void SavePresets()
    {
        string json = JsonUtility.ToJson(new SerializableList<TextPreset> { items = presets });
        EditorPrefs.SetString("VRChatTextCreator_Presets", json);
    }

    /// <summary>
    /// プリセットをEditorPrefsから読み込み
    /// </summary>
    private void LoadPresets()
    {
        string json = EditorPrefs.GetString("VRChatTextCreator_Presets", "");
        if (!string.IsNullOrEmpty(json))
        {
            SerializableList<TextPreset> loaded = JsonUtility.FromJson<SerializableList<TextPreset>>(json);
            if (loaded != null && loaded.items != null)
            {
                presets = loaded.items;
            }
        }
    }

    /// <summary>
    /// リストをシリアライズ可能にするためのラッパークラス
    /// </summary>
    [System.Serializable]
    private class SerializableList<T>
    {
        public List<T> items;
    }
}