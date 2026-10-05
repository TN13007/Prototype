using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

public static class PrototypeContentLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public static T Load<T>(string path) where T : class
    {
        if (!FileAccess.FileExists(path))
        {
            GD.PushError($"Content file was not found: {path}");
            return null;
        }

        try
        {
            string json = FileAccess.GetFileAsString(path);
            T content = JsonSerializer.Deserialize<T>(json, JsonOptions);

            if (content == null)
                GD.PushError($"Content file is empty or invalid: {path}");

            return content;
        }
        catch (JsonException exception)
        {
            GD.PushError($"Could not parse {path}: {exception.Message}");
            return null;
        }
    }
}
