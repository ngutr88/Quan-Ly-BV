# Sao luu file SQLite (hms.db) truoc khi chay migration hoac bat ky thay doi
# cau truc CSDL nao. Vi SQLite luu toan bo CSDL trong MOT file duy nhat, sao
# luu chi don gian la copy file (kem file -wal/-shm neu dang o che do WAL).
#
# Cach dung:
#   .\scripts\backup-sqlite.ps1
#   .\scripts\backup-sqlite.ps1 -DbPath "D:\QuanLyBenhVien\hms.db" -BackupDir "D:\Backups"

param(
    [string]$DbPath = (Join-Path $PSScriptRoot "..\hms.db"),
    [string]$BackupDir = (Join-Path $PSScriptRoot "..\backups")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $DbPath)) {
    throw "Khong tim thay file CSDL: $DbPath"
}

if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$destFile = Join-Path $BackupDir "hms_$timestamp.db"

Copy-Item -Path $DbPath -Destination $destFile -Force

# SQLite o che do WAL (Write-Ahead Log) co the con du lieu chua flush vao file
# chinh trong -wal/-shm. Sao luu ca hai neu ton tai de dam bao khong mat du
# lieu ghi gan nhat.
foreach ($suffix in @("-wal", "-shm")) {
    $sidecar = "$DbPath$suffix"
    if (Test-Path $sidecar) {
        Copy-Item -Path $sidecar -Destination "$destFile$suffix" -Force
    }
}

Write-Host "Da sao luu CSDL vao: $destFile"
Write-Host "Kiem tra file: (Get-Item '$destFile').Length -eq (Get-Item '$DbPath').Length"
