using System.Text.Json;

namespace keyboard_switcher;
public class Program
{
    private const string ConfigFilePath = "keybaordConfig.json";

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new KeyboardSwitcher(LoadConfig()));
    }

    public static Dictionary<string, KeyboardConfig> LoadConfig()
    {
        if (!File.Exists(ConfigFilePath))
            return new Dictionary<string, KeyboardConfig>();

        var json = File.ReadAllText(ConfigFilePath);
        return JsonSerializer.Deserialize<Dictionary<string, KeyboardConfig>>(json) ?? new Dictionary<string, KeyboardConfig>();
    }

    public static void SaveConfig(Dictionary<string, KeyboardConfig> config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFilePath, json);
    }
}