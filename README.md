# udon-practice

Unity 2022.3.22f1とVRChat SDK 3.8.2を使用したVRChatワールド開発プロジェクトです。

## 初回セットアップ

### 前提条件
1. **Unity Hub**: Unity Hubを通じてUnity 2022.3.22f1をインストール
2. **VRChat Creator Companion (VCC)**: VRChat SDKパッケージの管理用
3. **mise**: 開発ツールと依存関係の管理用

### セットアップ手順
```bash
# 1. miseをインストール（未インストールの場合）
curl https://mise.jdx.dev/install.sh | sh

# 2. 開発ツールをインストール
mise install

# 3. Unity HubでUnity 2022.3.22f1を使用してプロジェクトを開く

# 4. VRChat SDKはVPMを通じて自動的に解決される
```

## 開発について

詳細な開発ガイドラインとプロジェクト構造については、[CLAUDE.md](./CLAUDE.md)を参照してください。