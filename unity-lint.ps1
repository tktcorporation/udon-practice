# Simple Unity C# linter

Write-Host "Running Unity C# Analysis..." -ForegroundColor Green
Write-Host "=========================" -ForegroundColor Green

$files = Get-ChildItem -Path "Assets" -Filter "*.cs" -Recurse -ErrorAction SilentlyContinue

$errors = @()
$warnings = @()
$info = @()

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if (-not $content) { continue }
    
    $lines = Get-Content $file.FullName -ErrorAction SilentlyContinue
    if (-not $lines) { continue }
    
    $relativePath = $file.FullName.Replace("$PWD\", "").Replace("\", "/")
    
    # Check for GetComponent(typeof())
    if ($content -match "GetComponent\s*\(\s*typeof") {
        $warnings += "$relativePath : Use GetComponent<T>() instead of GetComponent(typeof(T))"
    }
    
    # Check for tag comparison
    if ($content -match '\.tag\s*==') {
        $warnings += "$relativePath : Use CompareTag() instead of tag =="
    }
    
    # Check for empty Unity methods
    if ($content -match "void\s+(Start|Update|Awake|LateUpdate|FixedUpdate)\s*\(\s*\)\s*\{\s*\}") {
        $warnings += "$relativePath : Remove empty Unity lifecycle methods"
    }
    
    # Check for public fields without SerializeField
    $lineNum = 0
    foreach ($line in $lines) {
        $lineNum++
        if ($line -match "^\s*public\s+(?!class|interface|enum|struct|void|override|static|const)" -and
            $line -notmatch "\(" -and
            $line -notmatch "\[SerializeField\]" -and
            ($lineNum -eq 1 -or $lines[$lineNum-2] -notmatch "\[SerializeField\]")) {
            $info += "$relativePath : Line $lineNum - Consider using [SerializeField] private instead of public field"
        }
    }
    
    # Check for Time.fixedDeltaTime in Update
    if ($content -match "void\s+Update\s*\(" -and $content -match "Time\.fixedDeltaTime") {
        $warnings += "$relativePath : Use Time.deltaTime in Update(), not Time.fixedDeltaTime"
    }
    
    # Check for using directives order
    $usingLines = @()
    $lineNum = 0
    foreach ($line in $lines) {
        $lineNum++
        if ($line -match "^using\s+") {
            $usingLines += @{Line = $line; Num = $lineNum}
        }
        elseif ($line -match "^namespace" -or $line -match "^public" -or $line -match "^internal") {
            break
        }
    }
    if ($usingLines.Count -gt 1) {
        $sorted = $usingLines.Line | Sort-Object
        $current = $usingLines.Line
        if (Compare-Object $current $sorted) {
            $info += "$relativePath : Using directives should be sorted alphabetically"
        }
    }
    
    # Check for TODO comments
    $lineNum = 0
    foreach ($line in $lines) {
        $lineNum++
        if ($line -match "(TODO|FIXME|HACK)") {
            $info += "$relativePath : Line $lineNum has TODO/FIXME/HACK comment"
        }
    }
}

# Display results
if ($errors.Count -gt 0) {
    Write-Host "`nERRORS ($($errors.Count)):" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "  $error" -ForegroundColor Red
    }
}

if ($warnings.Count -gt 0) {
    Write-Host "`nWARNINGS ($($warnings.Count)):" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "  $warning" -ForegroundColor Yellow
    }
}

if ($info.Count -gt 0) {
    Write-Host "`nINFO ($($info.Count)):" -ForegroundColor Cyan
    foreach ($i in $info) {
        Write-Host "  $i" -ForegroundColor Cyan
    }
}

$totalIssues = $errors.Count + $warnings.Count + $info.Count
Write-Host "`nTotal issues found: $totalIssues" -ForegroundColor White

if ($totalIssues -eq 0) {
    Write-Host "No issues found!" -ForegroundColor Green
    exit 0
} elseif ($errors.Count -gt 0) {
    exit 1
} else {
    exit 0
}