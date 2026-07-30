-- Phuc hoi CSDL SQL Server tu file .bak da tao boi backup-sqlserver.sql.
-- Dat ung dung ve che do bao tri / dung ket noi truoc khi chay script nay,
-- vi RESTORE DATABASE can quyen truy cap doc quyen (khong con session nao dang mo).
--
-- Cach dung (sqlcmd hoac SSMS), sau khi sua 2 bien duoi day:
--   sqlcmd -S <ServerName> -d master -i scripts\restore-sqlserver.sql

DECLARE @DbName      NVARCHAR(128) = N'QuanLyBenhVien';
DECLARE @BackupFile  NVARCHAR(4000) = N'D:\Backups\QuanLyBenhVien\QuanLyBenhVien_20260730_114358.bak';
DECLARE @Sql         NVARCHAR(MAX);

-- Ngat moi ket noi hien co toi database truoc khi restore.
SET @Sql = N'ALTER DATABASE ' + QUOTENAME(@DbName) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
EXEC sp_executesql @Sql;

SET @Sql = N'RESTORE DATABASE ' + QUOTENAME(@DbName) + N'
FROM DISK = ''' + @BackupFile + N'''
WITH REPLACE, RECOVERY, STATS = 10;';
EXEC sp_executesql @Sql;

SET @Sql = N'ALTER DATABASE ' + QUOTENAME(@DbName) + N' SET MULTI_USER;';
EXEC sp_executesql @Sql;

PRINT N'Da phuc hoi CSDL ' + @DbName + N' tu: ' + @BackupFile;
