using System.Collections.Generic;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Models.ViewModels;

/// <summary>Backs Areas/Doctor/Views/Profile/Index.cshtml.</summary>
public class DoctorProfileViewModel
{
    public Doctor Doctor { get; set; } = null!;

    public ProfileChangeRequest? PendingRequest { get; set; }

    public ProfileChangeFields? PendingFields { get; set; }

    public List<Department> Departments { get; set; } = new();

    public bool TwoFactorEnabled { get; set; }

    public bool TwoFactorForced { get; set; }

    public List<LoginSession> ActiveSessions { get; set; } = new();

    public string? CurrentSessionToken { get; set; }

    public List<AuditLog> LoginHistory { get; set; } = new();
}
