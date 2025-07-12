# CLAUDE.md

このファイルは、このリポジトリでコードを扱う際のClaude Code (claude.ai/code)へのガイダンスを提供します。

## プロジェクト概要

Unity 2022.3.22f1とVRChat SDK 3.8.2を使用したVRChatワールド開発プロジェクトです。VRChat互換の動作をスクリプト化するためにUdonSharpを使用しています。

## 環境構築

### ツール設定

#### mise (.mise.toml)
プロジェクトは一貫した開発環境のために[mise](https://mise.jdx.dev/)を使用します：
- **.NET SDK 6.0.424**: Unity 2022.3 C#開発に必要
- **Node.js 22.17.0 LTS**: ツールとスクリプト用
- **openupm-cli**: OpenUPMパッケージ管理用

#### パッケージ管理

1. **Unity Package Manager (UPM)**
   - コアUnityパッケージは`/Packages/manifest.json`で定義
   - サードパーティパッケージ用にOpenUPMレジストリを設定
   - 現在OpenUPMからNuGetForUnityを含む

2. **VRChat Package Manager (VPM)**
   - VRChat SDKパッケージは`/Packages/vpm-manifest.json`で管理
   - com.vrchat.worlds 3.8.2と依存関係を含む
   - VPMパッケージの更新にはVRChat Creator Companionを使用

3. **NuGet for Unity**
   - `/Assets/NuGet.config`で設定
   - パッケージは`/Assets/Packages/`にインストール
   - 現在NuGetパッケージは未使用（空のpackages.config）

## 主要コマンド

### Unity開発
- **Unityで開く**: Unity Hubを使用してUnity 2022.3.22f1でプロジェクトを開く
- **Play Modeテスト**: VRChat ClientSimでUnityのPlayモードを使用してローカルテスト
- **VRChatワールドビルド**: File → Build Settings → Build（VRChat SDKパネルの設定が必要）

### コード品質
- **Lintチェック**: 変更をコミットする前に`mise lint`を実行
- **タスク完了**: 開発タスクを完了する際は必ず`mise lint`が成功することを確認

### Git操作
- リポジトリにはUnity/VRChat開発用の包括的な`.gitignore`がある
- Unityメタファイルは追跡される（Unityプロジェクトに必要）

## アーキテクチャと構造

### コアディレクトリ
- `/Assets/` - すべてのプロジェクトアセットとスクリプト
- `/Assets/Scenes/` - Unityシーン（メインシーン: VRCDefaultWorldScene.unity）
- `/Assets/UdonSharp/UtilityScripts/` - UdonSharp動作スクリプト
- `/Assets/SerializedUdonPrograms/` - コンパイル済みUdonプログラム（自動生成）

### 主要スクリプトタイプ
- **PlayerModSetter.cs**: プレイヤー移動の変更（ジャンプ、速度、重力）
- **InteractToggle.cs**: インタラクティブオブジェクトの切り替え
- **GlobalToggleObject.cs**: ネットワーク同期オブジェクト状態
- **MasterToggleObject.cs**: マスター専用コントロール

## VRChat固有の考慮事項

1. **ネットワーキング**: ネットワーク動作には適切な`BehaviourSyncMode`を使用
2. **Player API**: `VRCPlayerApi`を通じてプレイヤーデータにアクセス
3. **インタラクション**: プレイヤーインタラクションには`Interact()`メソッドを使用
4. **オーナーシップ**: ネットワークオブジェクトのオーナーシップは`Networking.SetOwner()`で処理

## Context7 MCP - ドキュメント参照機能

Claude CodeはContext7 MCP (Model Context Protocol)を通じて、VRChatの最新ドキュメントやAPIリファレンスにアクセスできます。これにより、以下のような情報を取得できます：

- **VRChat公式ドキュメント**: 最新のAPIリファレンス、UdonSharpガイド、ネットワーキング仕様など
- **Unity公式ドキュメント**: Unity 2022.3の最新機能、ベストプラクティス
- **その他の技術ドキュメント**: 関連するライブラリやツールの最新情報

### 使用方法
開発中に以下のような場面で積極的に活用してください：
- 新しいAPIや機能について調べる必要がある時
- エラーメッセージや警告の解決方法を探す時
- ベストプラクティスや推奨される実装パターンを確認する時
- LLMの学習データに含まれていない最新の情報が必要な時

この機能により、常に最新かつ正確な情報に基づいた開発が可能になります。

## コードドキュメント化ルール

このプロジェクトはUnity C#初心者向けに作成されているため、すべてのコードには詳細なドキュメントが必要です。以下のルールに従ってください：

### 必須ドキュメント項目

1. **コードの意図と目的**
   - なぜそのコードを書いたのか
   - 何を実現しようとしているのか
   - どのような問題を解決するのか

2. **動作の詳細説明**
   - コードによって何が起こるのか
   - 実行順序とその理由
   - 予期される結果と副作用

3. **関数・メソッドの説明**
   - 関数の目的と役割
   - パラメータの詳細な説明
   - 戻り値の意味と使用方法
   - 呼び出しタイミングと条件

4. **クラスの設計意図**
   - クラスの存在理由と責務
   - 他のクラスとの関係性
   - 使用場面とライフサイクル

5. **全体構造の説明**
   - システム全体における位置づけ
   - 他のコンポーネントとの連携方法
   - データフローと処理の流れ

### ドキュメント記述例

```csharp
/// <summary>
/// プレイヤーのジャンプ機能を制御するクラス
/// VRChat内でプレイヤーがインタラクトボタンを押すとジャンプできるようにする
/// </summary>
/// <remarks>
/// 使用理由：
/// - VRChatではデフォルトのジャンプがないため、カスタム実装が必要
/// - インタラクトボタンを使うことで、VRコントローラーでも操作可能
/// 
/// 動作の流れ：
/// 1. プレイヤーがオブジェクトに近づく
/// 2. インタラクトボタンを押す
/// 3. Interact()メソッドが呼ばれる
/// 4. プレイヤーの上方向に力を加える
/// 5. 結果：プレイヤーがジャンプする
/// </remarks>
public class JumpPad : UdonSharpBehaviour
{
    /// <summary>
    /// ジャンプの強さ（単位：Unity物理エンジンの力）
    /// 大きいほど高くジャンプする（推奨値：5-15）
    /// </summary>
    [SerializeField] private float jumpPower = 10f;
    
    /// <summary>
    /// プレイヤーがインタラクトした時に呼ばれる
    /// VRChatの仕様により、このメソッド名は固定
    /// </summary>
    public override void Interact()
    {
        // インタラクトしたプレイヤーを取得
        // なぜ必要か：ジャンプさせる対象を特定するため
        VRCPlayerApi player = Networking.LocalPlayer;
        
        // プレイヤーに上向きの速度を設定
        // Vector3.upは(0,1,0)を表し、真上方向
        // jumpPowerを掛けることで、ジャンプの高さを調整
        player.SetVelocity(Vector3.up * jumpPower);
    }
}
```

### 重要な注意事項

- **初心者向けを意識**：専門用語は避けるか、使う場合は必ず説明を追加
- **なぜ？を説明**：「なぜその方法を選んだのか」を明確に記述
- **具体例を提供**：抽象的な説明より、具体的な使用例を示す
- **エラー処理も説明**：エラーが起きる可能性とその対処法も記載

## 開発メモ

- **重要**: タスクを完了としてマークする前に必ず`mise lint`を実行し、成功することを確認
- マルチプレイヤー機能は必ずClientSimでテスト
- VRではパフォーマンスが重要 - ドローコールとポリゴン数を最適化
- パフォーマンス分析にはUnity Profilerを使用
- ワールドコンテンツにはVRChatのコミュニティガイドラインに従う