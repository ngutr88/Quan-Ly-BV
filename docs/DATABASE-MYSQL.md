# MySQL local va Render

## Chay local trong Visual Studio

1. Cai MySQL Server 8.0 va tao/ghi nho mat khau tai khoan dung cho ung dung.
2. Sua `appsettings.json` (hoac bien moi truong) va thay `CHANGE_ME` trong `ConnectionStrings:DefaultConnection`.
3. Khoi dong lai ung dung. Khi dung provider MySQL, ung dung se tao database/schema theo model neu database chua co.

Khong commit mat khau that vao Git. Co the dat chuoi ket noi qua bien moi truong:

```text
Database__Provider=MySql
ConnectionStrings__DefaultConnection=Server=127.0.0.1;Port=3306;Database=QuanLyBenhVien;User ID=root;Password=<mat-khau>;AllowPublicKeyRetrieval=True;SslMode=Preferred;
```

## Render

`render.yaml` dang de SQLite cho ban demo. File SQLite trong web service Free la tam thoi; restart, spin-down hoac redeploy co the lam mat du lieu.

De dung MySQL production, tao mot MySQL managed o ben ngoai Render (hoac mot MySQL service co persistent disk), sau do dat hai bien moi truong trong Render Dashboard:

```text
Database__Provider=MySql
ConnectionStrings__DefaultConnection=Server=<host>;Port=3306;Database=<db>;User ID=<user>;Password=<secret>;SslMode=Required;
```

Database MySQL nam ngoai container nen khong bi mat khi deploy lai web service. Du lieu SQLite hien co trong `hms.db` cung khong tu dong chuyen sang MySQL; can export/import mot lan neu muon giu du lieu cu.

