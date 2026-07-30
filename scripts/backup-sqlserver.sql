-- Sao luu CSDL SQL Server truoc khi chay migration/thay doi cau truc.
-- Doi <ServerName> khong can thiet (chay tren dung server dich), nhung PHAI
-- doi <BackupPath> thanh duong dan hop le tren may chu SQL Server (khong phai
-- may client), vi luong BACKUP DATABASE chay o phia server.
--
-- Cach dung (sqlcmd hoac SSMS):
--   sqlcmd -S <ServerName> -d master -i scripts\backup-sqlserver.sql

DECLARE @DbName        NVARCHAR(128) = N'QuanLyBenhVien';
DECLARE @BackupPath     NVARCHAR(4000) = N'D:\Backups\QuanLyBenhVien\';
DECLARE @Timestamp      NVARCHAR(20) = FORMAT(SYSDATETIME(), 'yyyyMMdd_HHmmss');
DECLARE @BackupFile     NVARCHAR(4000) = @BackupPath + @DbName + N'_' + @Timestamp + N'.bak';
DECLARE @Sql            NVARCHAR(MAX);

SET @Sql = N'BACKUP DATABASE ' + QUOTENAME(@DbName) + N'
TO DISK = ''' + @BackupFile + N'''
WITH INIT, CHECKSUM, COMPRESSION,
     NAME = ''' + @DbName + N' - Full Backup truoc migration'',
     STATS = 10;';

PRINT @Sql;
EXEC sp_executesql @Sql;

-- Xac minh file backup hop le truoc khi tiep tuc migration.
DECLARE @VerifySql NVARCHAR(MAX) = N'RESTORE VERIFYONLY FROM DISK = ''' + @BackupFile + N''';';
EXEC sp_executesql @VerifySql;

PRINT N'Da sao luu va xac minh: ' + @BackupFile;
