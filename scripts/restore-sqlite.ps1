# Phuc hoi file SQLite (hms.db) tu mot ban sao luu da tao boi backup-sqlite.ps1.
# Se tu sao luu file hms.db HIEN TAI truoc khi ghi de, phong truong hop can quay lai.
#
# Cach dung:
#   .\scripts\restore-sqlite.ps1 -BackupFile "D:\QuanLyBenhVien\backups\hms_20260730_114358.db"

param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile,

    [string]$DbPath = (Join-Path $PSScriptRoot "..\hms.db")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackupFile)) {
    throw "Khong tim thay file sao luu: $BackupFile"
}

if (Test-Path $DbPath) {
    $safety = "$DbPath.before-restore_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
    Copy-Item -Path $DbPath -Destination $safety -Force
    Write-Host "Da sao luu CSDL hien tai vao: $safety (phong truong hop can hoan tac restore)"
}

Copy-Item -Path $BackupFile -Destination $DbPath -Force

foreach ($suffix in @("-wal", "-shm")) {
    $sidecarBackup = "$BackupFile$suffix"
    $sidecarDest = "$DbPath$suffix"
    if (Test-Path $sidecarBackup) {
        Copy-Item -Path $sidecarBackup -Destination $sidecarDest -Force
    } elseif (Test-Path $sidecarDest) {
        # Ban sao luu khong co -wal/-shm (thuong la khi backup luc CSDL dang dong);
        # xoa file -wal/-shm cu de tranh SQLite doc nham du lieu WAL khong khop.
        Remove-Item $sidecarDest -Force
    }
}

Write-Host "Da phuc hoi CSDL tu: $BackupFile"
Write-Host "Dung lai ung dung (neu dang chay) truoc khi restore, va khoi dong lai sau khi restore xong."
