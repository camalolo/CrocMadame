using System;
using System.IO;
using System.Text.Json;

namespace CrocMadame;

public class Settings
{
    public string ReceiveRelay { get; set; } = "";
    public string SendRelay { get; set; } = "";

    private static string SettingsFilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".crocmadame");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            }
        }
        catch (Exception)
        {
            // If loading fails, return default settings
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception)
        {
            // Silently fail if saving fails
        }
    }
}