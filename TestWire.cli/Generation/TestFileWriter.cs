namespace TestWire.cli.Generation;

public class TestFileWriter
{
    public static void Write(string outputPath, string content)
    {
        var directory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(outputPath, content);
    }
}