using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NexaCastVideo;

// IFileWrapper provides an interface for file operations.
public interface IFileWrapper
{
    bool Exists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string contents);
}

// FileWrapper implements file operations defined in IFileWrapper.
public class FileWrapper : IFileWrapper
{
    public bool Exists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
}

// ConfigManager depends on the abstraction IFileWrapper.
public class ConfigManager
{
    private static readonly string ConfigDirectory = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\SECURE"));
    private static readonly string ConfigFileName = "config.json";
    public static readonly string ConfigPath = Path.Combine(ConfigDirectory, ConfigFileName);
    private static readonly object LockObject = new object();
    private static JObject Configurations;
    private static IFileWrapper FileWrapperInstance;

    public static string GetAPIKey(string apiName)
    {
        try
        {
            return GetAppSetting(apiName);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error retrieving API key for '{apiName}': {ex.Message}");
            return null;
        }
    }


    static ConfigManager()
    {
        FileWrapperInstance = new FileWrapper();
        LoadConfigurations();
    }

    public static void LoadConfigurations()
    {
        try
        {
            if (!FileWrapperInstance.Exists(ConfigPath))
            {
                throw new FileNotFoundException($"Configuration file not found at {ConfigPath}");
            }

            string json = FileWrapperInstance.ReadAllText(ConfigPath);
            Configurations = JObject.Parse(json);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error loading configurations: {ex.Message}");
            Configurations = new JObject();
        }
    }

    // GetAppSetting retrieves a specific configuration value.

    public static string GetAppSetting(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Logger.LogError("Key is null or whitespace.");
            return null;
        }

        lock (LockObject)
        {
            try
            {
                return Configurations[key]?.ToString();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error retrieving app setting for key '{key}': {ex.Message}");
                return null;
            }
        }
    }

    // SetAppSetting method can be extended to support different types of settings.
    public static void SetAppSetting(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Logger.LogError("Key is null or whitespace.");
            return;
        }

        lock (LockObject)
        {
            try
            {
                Configurations[key] = value;
                FileWrapperInstance.WriteAllText(ConfigPath, Configurations.ToString());
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error saving configurations: {ex.Message}");
            }
        }
    }
}
