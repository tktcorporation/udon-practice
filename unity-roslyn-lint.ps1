# Unity Roslyn Analyzers Lint Script
# This script runs Roslyn Analyzers on Unity C# files

Write-Host "Unity C# Roslyn Analyzers" -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green

# First, check if Unity generated project files exist
$mainProject = "Assembly-CSharp.csproj"
if (-not (Test-Path $mainProject)) {
    Write-Host "Error: $mainProject not found. Please open the project in Unity to generate project files." -ForegroundColor Red
    Write-Host "In Unity: Edit > Preferences > External Tools > Regenerate project files" -ForegroundColor Yellow
    exit 1
}

# Create a custom MSBuild project that includes analyzers
$analyzerProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <NoWarn>CS0649;CS0169;IDE0051;IDE0044;CS8019</NoWarn>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>AllEnabledByDefault</AnalysisMode>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Unity.Analyzers" Version="1.18.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <Compile Include="Assets\**\*.cs" Exclude="Assets\Packages\**;Assets\TextMesh Pro\**;Assets\Editor\**" />
  </ItemGroup>

  <ItemGroup>
    <AdditionalFiles Include=".editorconfig" />
    <GlobalAnalyzerConfigFiles Include="GlobalAnalyzerConfig" />
  </ItemGroup>

  <!-- Unity and VRChat references -->
  <ItemGroup>
    <Reference Include="UnityEngine">
      <HintPath>Library\ScriptAssemblies\UnityEngine.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UnityEngine.CoreModule">
      <HintPath>Library\ScriptAssemblies\UnityEngine.CoreModule.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="VRC.SDKBase">
      <HintPath>Library\ScriptAssemblies\VRC.SDKBase.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="VRC.Udon">
      <HintPath>Library\ScriptAssemblies\VRC.Udon.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="UdonSharp.Runtime">
      <HintPath>Library\ScriptAssemblies\UdonSharp.Runtime.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
"@

$tempProjectPath = "TempRoslynAnalysis.csproj"
$analyzerProject | Out-File -FilePath $tempProjectPath -Encoding UTF8

try {
    # Restore packages
    Write-Host "`nRestoring analyzer packages..." -ForegroundColor Yellow
    $restoreOutput = & dotnet restore $tempProjectPath --verbosity minimal 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to restore packages:" -ForegroundColor Red
        Write-Host $restoreOutput
        exit 1
    }

    # Run build with analyzers
    Write-Host "Running Roslyn Analyzers..." -ForegroundColor Yellow
    Write-Host ""
    
    $buildOutput = & dotnet build $tempProjectPath `
        --no-restore `
        --verbosity normal `
        /p:RunAnalyzers=true `
        /p:RunAnalyzersDuringBuild=true `
        /p:ReportAnalyzer=true `
        /p:WarningLevel=4 `
        /p:TreatWarningsAsErrors=false 2>&1
    
    # Parse output
    $issues = @()
    
    foreach ($line in $buildOutput) {
        # Match warning/error pattern
        if ($line -match "(.+\.cs)\((\d+),(\d+)\):\s*(warning|error)\s+(\w+):\s*(.+)") {
            $filePath = $matches[1]
            $lineNum = $matches[2]
            $column = $matches[3]
            $severity = $matches[4]
            $code = $matches[5]
            $message = $matches[6].Trim()
            
            # Skip certain warnings
            if ($code -in @("CS8019", "CS0105", "CS0436")) {
                continue
            }
            
            $issues += [PSCustomObject]@{
                File = $filePath -replace [regex]::Escape($PWD + "\"), ""
                Line = $lineNum
                Severity = $severity
                Code = $code
                Message = $message
                Category = switch -Regex ($code) {
                    "^UNT" { "Unity" }
                    "^SA" { "StyleCop" }
                    "^S\d" { "SonarAnalyzer" }
                    "^CS" { "C# Compiler" }
                    "^CA" { "Code Analysis" }
                    "^IDE" { "IDE Suggestions" }
                    default { "Other" }
                }
            }
        }
    }
    
    # Group and display results
    $errors = $issues | Where-Object { $_.Severity -eq "error" }
    $warnings = $issues | Where-Object { $_.Severity -eq "warning" }
    
    if ($errors.Count -gt 0) {
        Write-Host "ERRORS ($($errors.Count)):" -ForegroundColor Red
        $errors | Group-Object Category | ForEach-Object {
            Write-Host "`n  $($_.Name) ($($_.Count)):" -ForegroundColor Red
            $_.Group | ForEach-Object {
                Write-Host "    $($_.File):$($_.Line) - $($_.Message) [$($_.Code)]" -ForegroundColor Red
            }
        }
    }
    
    if ($warnings.Count -gt 0) {
        Write-Host "`nWARNINGS ($($warnings.Count)):" -ForegroundColor Yellow
        $warnings | Group-Object Category | ForEach-Object {
            Write-Host "`n  $($_.Name) ($($_.Count)):" -ForegroundColor Yellow
            $_.Group | Select-Object -First 5 | ForEach-Object {
                Write-Host "    $($_.File):$($_.Line) - $($_.Message) [$($_.Code)]" -ForegroundColor Yellow
            }
            if ($_.Count -gt 5) {
                Write-Host "    ... and $($_.Count - 5) more" -ForegroundColor Gray
            }
        }
    }
    
    Write-Host "`nSummary:" -ForegroundColor Cyan
    Write-Host "  Total issues: $($issues.Count)"
    Write-Host "  Errors: $($errors.Count)" -ForegroundColor $(if ($errors.Count -gt 0) { "Red" } else { "Green" })
    Write-Host "  Warnings: $($warnings.Count)" -ForegroundColor $(if ($warnings.Count -gt 0) { "Yellow" } else { "Green" })
    
    # Category breakdown
    $issues | Group-Object Category | Sort-Object Count -Descending | ForEach-Object {
        Write-Host "  $($_.Name): $($_.Count)" -ForegroundColor Gray
    }
    
    if ($issues.Count -eq 0) {
        Write-Host "`nNo issues found! Code quality looks good." -ForegroundColor Green
    }
    
    # Save detailed report
    $reportPath = "roslyn-analysis-detailed.txt"
    $issues | Format-Table -AutoSize | Out-String | Out-File -FilePath $reportPath -Encoding UTF8
    if ($issues.Count -gt 0) {
        Write-Host "`nDetailed report saved to: $reportPath" -ForegroundColor Gray
    }
    
    if ($errors.Count -gt 0) {
        exit 1
    }
    exit 0
}
finally {
    # Clean up
    if (Test-Path $tempProjectPath) {
        Remove-Item $tempProjectPath -Force
    }
}