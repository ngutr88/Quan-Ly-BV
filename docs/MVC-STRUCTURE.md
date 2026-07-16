# Cấu trúc ASP.NET Core MVC

```text
QuanLyBenhVien/
├── Areas/                 # MVC theo vai trò Admin, Doctor, Patient
│   └── <Role>/
│       ├── Controllers/
│       └── Views/
├── Controllers/           # Controller dùng chung (Home, Auth)
├── Models/
│   ├── Entities/          # Entity/domain model được Entity Framework ánh xạ
│   └── ViewModels/        # Model chuyên dùng cho View
├── Views/                 # View root và partial/layout dùng chung
├── Data/                  # DbContext và dữ liệu khởi tạo
├── Migrations/            # Entity Framework migrations
├── Helpers/               # Hàm hỗ trợ dùng chung
└── wwwroot/               # Static assets
```

Namespace của entity vẫn là `QuanLyBenhVien.Models` để giữ tương thích với code và migrations hiện có. ViewModel dùng namespace `QuanLyBenhVien.Models.ViewModels`.

View phải nằm cạnh controller tương ứng: controller trong `Areas/<Role>/Controllers` sử dụng view trong `Areas/<Role>/Views/<Controller>`; controller root sử dụng `Views/<Controller>`.
