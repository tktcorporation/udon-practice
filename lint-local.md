# Unity C# Lint/Static Analysis (ローカル用)

# 1. .NETツールのローカルインストール
mise exec -- dotnet tool restore

# 2. コードフォーマットチェック
mise exec -- dotnet format --verify-no-changes --severity warn

# 3. 静的解析（Roslynator）
mise exec -- dotnet tool run roslynator analyze . --msbuild-path $(mise exec -- which msbuild) --verbosity minimal

# 4. 型チェック
mise exec -- dotnet build --no-restore --nologo --warnaserror

# 事前に一度だけ
# mise exec -- dotnet new tool-manifest --if-not-exists
# mise exec -- dotnet tool install dotnet-format
# mise exec -- dotnet tool install roslynator.dotnet.cli
