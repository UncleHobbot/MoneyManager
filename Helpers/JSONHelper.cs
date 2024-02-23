namespace MoneyManager.Helpers;

public static class JSONHelper
{
    public static async Task WriteJSON<T>(this string filename, T obj)
    {
        await using var createStream = File.Create(filename);
        await JsonSerializer.SerializeAsync(createStream, obj, new JsonSerializerOptions { WriteIndented = true });
    }

    public static async Task<T> ReadJSON<T>(this string filename)
    {
        if (File.Exists(filename))
        {
            await using var openStream = File.OpenRead(filename);
            return await JsonSerializer.DeserializeAsync<T>(openStream);
        }
        return default;
    }
}