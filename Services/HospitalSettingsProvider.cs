using System;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Services
{
    /// <summary>
    /// Single owner of hospital_settings.json read/write, so both Admin's
    /// settings screen and any patient/doctor-facing page that needs the
    /// hospital's public info (address, hotline, hours) read the exact same
    /// source instead of each keeping a private copy of the file I/O.
    /// </summary>
    public class HospitalSettingsProvider
    {
        private readonly string _settingsFilePath;

        public HospitalSettingsProvider(IWebHostEnvironment env)
        {
            _settingsFilePath = Path.Combine(env.ContentRootPath, "hospital_settings.json");
        }

        public HospitalSettings Load()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new HospitalSettings();
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<HospitalSettings>(json) ?? new HospitalSettings();
            }
            catch
            {
                return new HospitalSettings();
            }
        }

        public void Save(HospitalSettings settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsFilePath, json);
        }
    }
}
