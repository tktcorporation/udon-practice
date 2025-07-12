namespace MyEditor
{
    using UdonSharpEditor;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Unity エディタ拡張：インタラクティブオブジェクト配置ツール
    /// ワンクリックで原点にインタラクト可能なキューブを配置するための開発支援ツール.
    /// </summary>
    /// <remarks>
    /// 【このツールを作った理由】
    /// VRChatワールド開発では、テスト用のオブジェクトを素早く配置する必要が頻繁にあります.
    /// 毎回手動でキューブを作成し、位置をリセットするのは面倒なので、
    /// ワンクリックで原点に配置できるツールを作成しました.
    ///
    /// 【Unity エディタ拡張とは】
    /// Unity エディタ拡張は、Unity エディタ自体に機能を追加する仕組みです.
    /// ゲーム実行時ではなく、開発中のエディタ上でのみ動作します.
    /// そのため、ビルドされたVRChatワールドには含まれません.
    ///
    /// 【EditorWindow を継承する理由】
    /// EditorWindow クラスを継承することで、Unity のメニューバーに
    /// 独自のメニュー項目を追加できます.これにより、開発効率が向上します.
    /// </remarks>
    public class SimpleObjectPlacerWindow : EditorWindow
    {
        /// <summary>
        /// Unity エディタのメニューバーに「Place Cube At Origin」を追加し、
        /// クリック時に原点にインタラクト可能なキューブを配置する.
        /// </summary>
        /// <remarks>
        /// 【MenuItem 属性の役割】
        /// [MenuItem("Tools/Place Cube At Origin")] により、
        /// Unity エディタの上部メニューバーに「Tools」メニューを作成し、
        /// その中に「Place Cube At Origin」という項目を追加します.
        ///
        /// 【static メソッドである理由】
        /// MenuItem 属性を使用するメソッドは static である必要があります.
        /// これは、インスタンスを作成せずにメニューから直接呼び出すためです.
        ///
        /// 【実行の流れ】
        /// 1. ユーザーがメニューバーの「Tools」→「Place Cube At Origin」をクリック
        /// 2. このメソッドが自動的に呼び出される
        /// 3. キューブが原点に生成される
        /// 4. InteractiveCube (UdonSharp) スクリプトがアタッチされる
        /// 5. 生成されたキューブが選択状態になる
        ///
        /// 【インタラクト機能】
        /// 生成されたキューブは VRChat 内でインタラクト可能になり、
        /// プレイヤーがインタラクトすると色が赤に変わります.
        /// </remarks>
        [MenuItem("Tools/Place Cube At Origin")]
        public static void PlaceCubeAtOrigin()
        {
            // ========== キューブの生成 ==========
            // GameObject.CreatePrimitive は Unity が提供する基本形状を生成するメソッド
            // PrimitiveType.Cube を指定することで、1x1x1 サイズのキューブを作成
            // なぜキューブを選んだか：最も基本的な形状で、衝突判定のテストなどに便利なため
            GameObject newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // 生成したオブジェクトに分かりやすい名前を設定
            // デフォルトでは "Cube" という名前になるが、
            // このツールで配置したことが分かるように "PlacedCube" に変更
            newObject.name = "PlacedCube";

            // ========== Transform の設定 ==========
            // Transform は Unity のすべてのゲームオブジェクトが持つコンポーネントで、
            // 位置（position）、回転（rotation）、大きさ（scale）を管理します

            // 位置を原点（0, 0, 0）に設定
            // Vector3.zero は new Vector3(0, 0, 0) の省略形
            // VRChat ワールドでは原点が重要な基準点となるため、ここに配置
            newObject.transform.position = Vector3.zero;

            // 回転をリセット（回転なし）
            // Quaternion.identity は「回転なし」を表す特別な値
            // 初心者向け説明：オブジェクトがまっすぐ正面を向いている状態
            newObject.transform.rotation = Quaternion.identity;

            // 大きさを標準サイズ（1, 1, 1）に設定
            // Vector3.one は new Vector3(1, 1, 1) の省略形
            // これにより、キューブは 1メートル × 1メートル × 1メートル のサイズになる
            newObject.transform.localScale = Vector3.one;

            // ========== UdonSharp の設定 ==========
            // UdonSharp は VRChat で使用されるスクリプティングシステムで、
            // C# のコードを Udon（VRChat の実行環境）用にコンパイルします

            // InteractiveCube スクリプトをアタッチ
            // このスクリプトにより、プレイヤーがキューブにインタラクトできるようになる
            // AddUdonSharpComponent は UdonSharp 専用のメソッドで、
            // 通常の AddComponent とは異なり、UdonBehaviour も自動的に設定される
            newObject.AddUdonSharpComponent<InteractiveCube>();

            // なぜ UdonSharp を使うのか：
            // 1. VRChat ではセキュリティ上の理由から、通常の C# スクリプトは実行できない
            // 2. UdonSharp により、使い慣れた C# 構文で VRChat 用のスクリプトが書ける
            // 3. インタラクト機能などの VRChat 特有の機能が簡単に実装できる

            // ========== コライダーの設定 ==========
            // インタラクトを可能にするため、コライダーが必要
            // CreatePrimitive で作成したキューブには既に BoxCollider が付いているが、
            // 念のため確認して設定を調整
            BoxCollider collider = newObject.GetComponent<BoxCollider>();
            if (collider != null)
            {
                // コライダーのトリガー設定
                // isTrigger = false にすることで、物理的な衝突も検知できる
                collider.isTrigger = false;
            }

            // ========== エディタ機能との統合 ==========

            // Undo（元に戻す）機能への対応
            // Ctrl+Z でこの操作を取り消せるようにする
            // "Place Cube At Origin" は Undo 履歴に表示される操作名
            Undo.RegisterCreatedObjectUndo(newObject, "Place Cube At Origin");

            // 生成したオブジェクトを選択状態にする
            // これにより、インスペクターウィンドウに詳細が表示され、
            // すぐに編集を開始できる
            Selection.activeGameObject = newObject;

            // オブジェクトを「変更済み」としてマーク
            // これにより、シーンの保存時に確実に保存される
            // Unity はこのマークがないと、変更を検知できない場合がある
            EditorUtility.SetDirty(newObject);

            // コンソールウィンドウに詳細メッセージを表示
            // 開発者に操作が成功したことと、インタラクト機能があることを通知
            Debug.Log("インタラクト可能なキューブを原点に配置しました。" +
                      "プレイヤーがインタラクトすると色が赤に変わります。");
        }
    }
}
