# Unity C# 静的解析ガイド

このプロジェクトでは Unity 向けにカスタマイズされた静的解析ツールを使用しています。

## 使用方法

### miseタスクランナーを使用（推奨）

#### 基本的な静的解析（高速）
```bash
mise run lint
```

#### Roslyn Analyzersを使用した詳細解析
```bash
mise run lint:roslyn
# または
mise run lint:full
```

### 手動実行
```powershell
# 基本的な解析
powershell -ExecutionPolicy Bypass -File unity-lint.ps1

# Roslyn Analyzers
powershell -ExecutionPolicy Bypass -File unity-roslyn-lint.ps1
```

## 検出される問題

### ⚠️ WARNINGS (修正推奨)
- `GetComponent(typeof(T))` → `GetComponent<T>()` を使用
- `.tag == "TagName"` → `CompareTag("TagName")` を使用
- 空のUnityライフサイクルメソッド（Start, Update等）
- Update内での `Time.fixedDeltaTime` 使用

### ℹ️ INFO (参考情報)
- publicフィールド → `[SerializeField] private` の使用を推奨
- using文の並び順
- TODO/FIXME/HACKコメント

## 使用している静的解析ツール

### 1. unity-lint.ps1（基本的な解析）
- Unity特有のパターンを検出
- 高速に実行可能
- カスタムルールベース

### 2. unity-roslyn-lint.ps1（Roslyn Analyzers）
- **Microsoft.Unity.Analyzers** - Unity専用の静的解析
- **StyleCop.Analyzers** - コードスタイルの一貫性
- **SonarAnalyzer.CSharp** - バグとコード品質

## 設定ファイル

### .editorconfig
C#のコードスタイルルールを定義。Unity特有の警告を適切に抑制：
- IDE0051: 未使用のprivateメンバー（SerializeField用）
- CS0649: 未割り当てフィールド（Unity Inspector用）
- IDE0044: readonlyフィールド（SerializeField不可）

### Directory.Build.props
Roslyn Analyzersの設定（IDE向け）

### .ruleset
個別の解析ルールの重要度設定

### GlobalAnalyzerConfig
グローバルなアナライザー設定

## Unity特有の考慮事項

1. **publicフィールド**: Unityではよく使われるが、`[SerializeField] private`が推奨
2. **空のメソッド**: Unityのライフサイクルメソッドは削除してパフォーマンス向上
3. **tag比較**: 文字列比較より`CompareTag()`の方が高速

## CI/CD統合

GitHub Actionsの設定（`.github/workflows/code-analysis.yml`）でプッシュ時に自動実行されます。