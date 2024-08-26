namespace LIME.Shared.Configuration;

public class DotEnv
{
    public static void Load(string path)
    {
        path = Path.Combine(path, ".env");

        var lines = File.ReadAllLines(path);
        EnumerateAndSetVariables(lines);
    }

    public static async Task LoadAsync(string path)
    {
        path = Path.Combine(path, ".env");

        var lines = await File.ReadAllLinesAsync(path);
        EnumerateAndSetVariables(lines);
    }

    private static void EnumerateAndSetVariables(string[] vars)
    {
        foreach (var line in vars)
        {
            var sections = line.Split('=', 2);
            if (sections.Length < 2)
            {
                continue;
            }

            Environment.SetEnvironmentVariable(sections[0], sections[1], EnvironmentVariableTarget.Process);
        }
    }
}
