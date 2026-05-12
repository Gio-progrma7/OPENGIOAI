# ============================================================
#  migrar_core.ps1 — Crea OPENGIOAI.Core desde OPENGIOAI
#  Ejecutar desde la raíz del repo: .\migrar_core.ps1
# ============================================================

$repoRaiz = Split-Path -Parent $MyInvocation.MyCommand.Path
$src      = Join-Path $repoRaiz "OPENGIOAI"
$dst      = Join-Path $repoRaiz "OPENGIOAI.Core"

Write-Host "`n📦 Creando OPENGIOAI.Core en: $dst" -ForegroundColor Cyan

# ── 1. Carpetas que van íntegras al Core ────────────────────
$carpetasCore = @(
    "Agentes",
    "Comandos",
    "Data",
    "Entidades",
    "Herramientas",
    "ServiciosAI",
    "ServiciosSlack",
    "ServiciosTelegram",
    "ServiciosTTS",
    "Skills",
    "Promts"
)

foreach ($c in $carpetasCore) {
    $origen  = Join-Path $src $c
    $destino = Join-Path $dst $c
    if (Test-Path $origen) {
        Copy-Item -Path $origen -Destination $destino -Recurse -Force
        Write-Host "  ✓ $c" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ No existe: $c" -ForegroundColor Yellow
    }
}

# ── 2. Utilerias: todo excepto los archivos WinForms-only ───
$srcUtil  = Join-Path $src "Utilerias"
$dstUtil  = Join-Path $dst "Utilerias"
New-Item -ItemType Directory -Path $dstUtil -Force | Out-Null

# Estos dos dependen de WinForms/GDI+ — se quedan en OPENGIOAI
$excluirUtil = @(
    "ChatStreamingThrottleService.cs",   # usa System.Windows.Forms.Timer + BurbujaChat
    "GeneradorIcono.cs"                  # usa System.Drawing (GDI+), llamado desde Program.cs
)

Get-ChildItem -Path $srcUtil -File | Where-Object { $_.Name -notin $excluirUtil } | ForEach-Object {
    Copy-Item $_.FullName -Destination $dstUtil -Force
    Write-Host "  ✓ Utilerias\$($_.Name)" -ForegroundColor Green
}

Write-Host "`n✅ Archivos copiados." -ForegroundColor Cyan

# ── 3. Agregar el nuevo proyecto a la solución ──────────────
Write-Host "`n🔗 Agregando OPENGIOAI.Core a la solución..." -ForegroundColor Cyan

$slnx = Join-Path $repoRaiz "OPENGIOAI.slnx"
if (Test-Path $slnx) {
    # slnx es XML — agregar el proyecto manualmente si dotnet sln no lo soporta aún
    [xml]$xml = Get-Content $slnx
    $ns = "http://schemas.microsoft.com/developer/msbuild/2003"
    
    # Intentar con dotnet sln primero (soporta .slnx desde .NET 9 preview)
    $resultado = dotnet sln "$slnx" add "OPENGIOAI.Core\OPENGIOAI.Core.csproj" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Proyecto agregado a la solución (.slnx)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ dotnet sln no pudo modificar el .slnx. Agrega el proyecto manualmente en Visual Studio:" -ForegroundColor Yellow
        Write-Host "    Clic derecho en solución → Agregar → Proyecto existente → OPENGIOAI.Core\OPENGIOAI.Core.csproj" -ForegroundColor Yellow
    }
} else {
    # Buscar .sln clásico
    $sln = Get-ChildItem $repoRaiz -Filter "*.sln" | Select-Object -First 1
    if ($sln) {
        dotnet sln $sln.FullName add "OPENGIOAI.Core\OPENGIOAI.Core.csproj"
        Write-Host "  ✓ Proyecto agregado a $($sln.Name)" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ No se encontró archivo de solución. Agrega el proyecto manualmente." -ForegroundColor Yellow
    }
}

# ── 4. Restaurar y verificar compilación ───────────────────
Write-Host "`n🔧 Restaurando paquetes..." -ForegroundColor Cyan
dotnet restore "OPENGIOAI.Core\OPENGIOAI.Core.csproj"

Write-Host "`n🏗  Compilando OPENGIOAI.Core..." -ForegroundColor Cyan
dotnet build "OPENGIOAI.Core\OPENGIOAI.Core.csproj" --no-restore

Write-Host "`n🏗  Compilando OPENGIOAI (WinForms)..." -ForegroundColor Cyan
dotnet build "OPENGIOAI\OPENGIOAI.csproj" --no-restore

Write-Host "`n🎉 Migración completada." -ForegroundColor Green
Write-Host "   Siguientes pasos recomendados:" -ForegroundColor Gray
Write-Host "   1. Abrir Visual Studio y verificar que ambos proyectos aparecen en la solución." -ForegroundColor Gray
Write-Host "   2. Ejecutar los tests: dotnet run --project OPENGIOAI\Tests\ComandosTests" -ForegroundColor Gray
Write-Host "   3. Correr la app: dotnet run --project OPENGIOAI`n" -ForegroundColor Gray
