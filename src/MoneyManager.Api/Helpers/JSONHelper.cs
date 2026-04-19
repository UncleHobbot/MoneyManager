using System.Text.Json;

namespace MoneyManager.Api.Helpers;

/// <summary>
/// Provides helper methods for JSON serialization and deserialization operations.
/// </summary>
/// <remarks>
/// Uses System.Text.Json for efficient JSON processing.
/// Extension methods on string for clean API: <c>filename.WriteJSON(obj)</c> and <c>filename.ReadJSON&lt;T&gt;()</c>.
/// All operations are asynchronous to prevent blocking the calling thread.
/// Automatically handles file opening and closing with <c>using</c> statements.
/// </remarks>
public static class JSONHelper
{
    /// <summary>
    /// Serializes an object to JSON and writes it to a file.
    /// </summary>
    /// <typeparam name="T">
    /// The type of object to serialize. Can be any reference or value type.
    /// </typeparam>
    /// <param name="filename">
    /// The path and filename of the file to write.
    /// </param>
    /// <param name="obj">
    /// The object to serialize to JSON format.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous write operation.
    /// </returns>
    /// <remarks>
    /// Creates a new file if it doesn't exist, or overwrites if it does.
    /// Uses indented JSON format for human readability.
    /// File is automatically closed after writing via <c>using</c> statement.
    /// Uses <see cref="JsonSerializer.SerializeAsync{TValue}(System.IO.Stream, TValue, JsonSerializerOptions, System.Threading.CancellationToken)"/>.
    /// </remarks>
    /// <exception cref="System.IO.IOException">
    /// Thrown when the file path is invalid or file cannot be created.
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the application doesn't have permission to write to the specified file.
    /// </exception>
    public static async Task WriteJSON<T>(this string filename, T obj)
    {
        await using var createStream = File.Create(filename);
        await JsonSerializer.SerializeAsync(createStream, obj, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Reads and deserializes JSON data from a file into an object of specified type.
    /// </summary>
    /// <typeparam name="T">
    /// The type of object to deserialize to. Must have a parameterless constructor for deserialization.
    /// </typeparam>
    /// <param name="filename">
    /// The path and filename of the file to read.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous read operation. The task result is the deserialized object of type T,
    /// or <c>null</c> if the file doesn't exist.
    /// </returns>
    /// <remarks>
    /// Returns <c>default(T)</c> (null for reference types, default for value types) if the file doesn't exist.
    /// Doesn't throw an exception for missing files; returns null instead.
    /// Uses <see cref="JsonSerializer.DeserializeAsync{TValue}(System.IO.Stream, JsonSerializerOptions, System.Threading.CancellationToken)"/>.
    /// File is automatically closed after reading via <c>using</c> statement.
    /// </remarks>
    /// <exception cref="System.IO.IOException">
    /// Thrown when the file exists but cannot be read.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the file contains invalid JSON that cannot be deserialized to type T.
    /// </exception>
    public static async Task<T?> ReadJSON<T>(this string filename)
    {
        if (File.Exists(filename))
        {
            await using var openStream = File.OpenRead(filename);
            return await JsonSerializer.DeserializeAsync<T>(openStream);
        }
        return default;
    }
}
