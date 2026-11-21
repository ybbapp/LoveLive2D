using Kiraio.LoveL2D;

AssertEqual(
    string.Concat((char)67, (char)72, (char)67, (char)67, (char)45,
        (char)49, (char)50, (char)51, (char)52, (char)45,
        (char)53, (char)54, (char)55, (char)56, (char)45,
        (char)57, (char)48, (char)49, (char)50),
    Utils.CreateMacOSLicenseKey(),
    "generating a valid macOS license key"
);

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
    Utils.FindAppLibDirectory(Path.Combine(appBundle, "Contents")),
    "resolving a Contents directory with app/lib"
);
AssertEqual(
    Path.GetFullPath(expectedLib),
    Utils.FindAppLibDirectory(Path.Combine(expectedLib, "nested")),
    "selecting a nested directory"
);
AssertEqual(
    Path.GetFileName(Utils.FindRlmJar(expectedLib)),
    "rlm1603.jar",
    "finding the installed RLM version"
);
AssertEqual(
    null,
    Utils.FindAppLibDirectory(fixture.CreateDirectory("unrelated")),
    "rejecting an unrelated directory"
);
string directLayout = fixture.CreateDirectAppBundle();
AssertEqual(
    Path.GetFullPath(Path.Combine(directLayout, "Contents", "app")),
    Utils.FindAppLibDirectory(directLayout),
    "resolving the Live2D 5.3 direct app layout"
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
        File.WriteAllText(Path.Combine(appLib, "rlm1603.jar"), string.Empty);
        return appBundle;
    }

    public string CreateDirectory(string name)
    {
        string directory = Path.Combine(path, name);
        Directory.CreateDirectory(directory);
        return directory;
    }

    public string CreateDirectAppBundle()
    {
        string appBundle = Path.Combine(path, "Live2D Cubism 5.3.app");
        string app = Path.Combine(appBundle, "Contents", "app");
        Directory.CreateDirectory(app);
        File.WriteAllText(Path.Combine(app, "Live2D_Cubism.jar"), string.Empty);
        File.WriteAllText(Path.Combine(app, "rlm1603.jar"), string.Empty);
        return appBundle;
    }

    public void Dispose() => Directory.Delete(path, true);
}
