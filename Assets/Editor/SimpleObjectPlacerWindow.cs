namespace MyEditor
{
    using UdonSharpEditor;
    using UnityEditor;
    using UnityEngine;
    using VRC.Udon;
    using VRC.Udon.Common;

    /// <summary>
    /// Unity エディタ拡張：インタラクティブオブジェクト配置ツール
    /// ワンクリックで原点に様々な機能を持つキューブを配置するための開発支援ツール.
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
        [MenuItem("Tools/Place Interactive Cube At Origin")]
        public static void PlaceInteractiveCubeAtOrigin()
        {
            // 基本のキューブを作成
            GameObject newObject = CreateBasicCube("InteractiveCube");

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

            // ========== エディタ機能との統合 ==========
            FinalizeObject(newObject, "Place Interactive Cube");

            // コンソールウィンドウに詳細メッセージを表示
            // 開発者に操作が成功したことと、インタラクト機能があることを通知
            Debug.Log("インタラクト可能なキューブを原点に配置しました。" +
                      "プレイヤーがインタラクトすると色が赤に変わります。");
        }

        /// <summary>
        /// Unity エディタのメニューバーに「Place Player Detector Cube」を追加し、
        /// クリック時に原点にプレイヤー検出機能を持つキューブを配置する.
        /// </summary>
        /// <remarks>
        /// 【実行の流れ】
        /// 1. ユーザーがメニューバーの「Tools」→「Place Player Detector Cube」をクリック
        /// 2. このメソッドが自動的に呼び出される
        /// 3. キューブが原点に生成される
        /// 4. PlayerDetector (UdonSharp) スクリプトがアタッチされる
        /// 5. 生成されたキューブが選択状態になる
        ///
        /// 【プレイヤー検出機能】
        /// 生成されたキューブは VRChat 内でプレイヤーを検出し、
        /// プレイヤーとの距離に応じて色が変化します（近い：赤、遠い：青）.
        /// </remarks>
        [MenuItem("Tools/Place Player Detector Cube")]
        public static void PlacePlayerDetectorCube()
        {
            // 基本のキューブを作成
            GameObject newObject = CreateBasicCube("PlayerDetectorCube");

            // ========== PlayerDetector の設定 ==========
            // PlayerDetector スクリプトをアタッチ
            // このスクリプトにより、プレイヤーの接近を検出できるようになる
            
            // UdonBehaviourを追加
            var udonBehaviour = newObject.AddComponent<VRC.Udon.UdonBehaviour>();
            
            // PlayerDetectorのUdonSharpProgramAssetを検索して設定
            // なぜ必要か：UdonSharpはC#をUdonにコンパイルするため、
            // ProgramAssetを通じてコンパイル済みコードを参照する必要がある
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:UdonSharpProgramAsset PlayerDetector");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                var programAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UdonSharp.UdonSharpProgramAsset>(path);
                if (programAsset != null)
                {
                    udonBehaviour.AssignProgramAndVariables(programAsset.SerializedProgramAsset, new VRC.Udon.Common.UdonVariableTable());
                    udonBehaviour.programSource = programAsset;
                }
                else
                {
                    Debug.LogWarning("PlayerDetectorのProgramAssetが見つかりません。");
                }
            }
            else
            {
                Debug.LogWarning("PlayerDetectorのUdonSharpProgramAssetが見つかりません。Assets/Scripts/PlayerDetector.assetを確認してください。");
            }

            // ========== エディタ機能との統合 ==========
            FinalizeObject(newObject, "Place Player Detector Cube");

            // コンソールウィンドウに詳細メッセージを表示
            Debug.Log("プレイヤー検出キューブを原点に配置しました。" +
                      "プレイヤーが近づくと距離に応じて色が変化します。");
        }

        /// <summary>
        /// 基本的なキューブを生成し、共通の設定を行う
        /// </summary>
        /// <param name="objectName">生成するオブジェクトの名前</param>
        /// <returns>生成されたGameObject</returns>
        private static GameObject CreateBasicCube(string objectName)
        {
            // ========== キューブの生成 ==========
            GameObject newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newObject.name = objectName;

            // ========== Transform の設定 ==========
            newObject.transform.position = Vector3.zero;
            newObject.transform.rotation = Quaternion.identity;
            newObject.transform.localScale = Vector3.one;

            // ========== コライダーの設定 ==========
            BoxCollider collider = newObject.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = false;
            }

            return newObject;
        }

        /// <summary>
        /// オブジェクトの生成を完了し、エディタ機能と統合する
        /// </summary>
        /// <param name="obj">処理対象のGameObject</param>
        /// <param name="undoName">Undo履歴に表示される操作名</param>
        private static void FinalizeObject(GameObject obj, string undoName)
        {
            // Undo（元に戻す）機能への対応
            Undo.RegisterCreatedObjectUndo(obj, undoName);

            // 生成したオブジェクトを選択状態にする
            Selection.activeGameObject = obj;

            // オブジェクトを「変更済み」としてマーク
            EditorUtility.SetDirty(obj);
        }
    }
}
