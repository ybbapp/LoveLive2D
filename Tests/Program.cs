using Kiraio.LoveL2D;

using TemporaryDirectory fixture = new();
string appBundle = fixture.CreateAppBundle();
string expectedLib = Path.Combine(appBundle, "Contents", "app", "lib");

AssertEqual(
    Path.GetFullPath(expectedLib),
    Utils.FindAppLibDirectory(appBundle),
    "selecting an .app bundle"
);
AssertEqual(
    Path.GetFullPath(expectedLib),
    Utils.FindAppLibDirectory(Path.Combine(expectedLib, "nested")),
    "selecting a nested directory"
);
AssertEqual(
    null,
    Utils.FindAppLibDirectory(fixture.CreateDirectory("unrelated")),
    "rejecting an unrelated directory"
);

string jar = Path.Combine(expectedLib, "Live2D_Cubism.jar");
using (FileStream stream = File.Create(jar))
using (System.IO.Compression.ZipArchive archive = new(stream, System.IO.Compression.ZipArchiveMode.Create))
{
    foreach (string entryName in new[]
             {
                 "META-INF/MANIFEST.MF",
                 "META-INF/TEST.SF",
                 "META-INF/TEST.RSA",
                 "META-INF/NOTICE.txt"
             })
        archive.CreateEntry(entryName);
}

string signatures = string.Join(
    ",",
    Utils.GetJarSignatureEntries(jar).OrderBy(name => name, StringComparer.Ordinal)
);
AssertEqual(
    "META-INF/MANIFEST.MF,META-INF/TEST.RSA,META-INF/TEST.SF",
    signatures,
    "signature entry discovery"
);

Console.WriteLine("All macOS path and JAR signature tests passed.");

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"{name}: expected '{expected}', got '{actual}'");
}

sealed class TemporaryDirectory : IDisposable
{
    readonly string path = Path.Combine(Path.GetTempPath(), $"LoveLive2D.Tests.{Guid.NewGuid():N}");

    public TemporaryDirectory() => Directory.CreateDirectory(path);

    public string CreateAppBundle()
    {
        string appBundle = Path.Combine(path, "Live2D Cubism Editor.app");
        string appLib = Path.Combine(appBundle, "Contents", "app", "lib");
        Directory.CreateDirectory(Path.Combine(appLib, "nested"));
        File.WriteAllText(Path.Combine(appLib, "Live2D_Cubism.jar"), string.Empty);
        File.WriteAllText(Path.Combine(appLib, "rlm1501.jar"), string.Empty);
        return appBundle;
    }

    public string CreateDirectory(string name)
    {
        string directory = Path.Combine(path, name);
        Directory.CreateDirectory(directory);
        return directory;
    }

    public void Dispose() => Directory.Delete(path, true);
}
