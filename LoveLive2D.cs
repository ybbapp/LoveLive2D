using System.Reflection;
using NativeFileDialogSharp;
using Spectre.Console;

namespace Kiraio.LoveL2D
{
    public class LoveLive2D
    {
        const string VERSION = "1.0.0";
        const string LIVE2D_CUBISM_MAIN = "Live2D_Cubism.jar";
        static readonly string LIVE2D_CUBISM_PACKAGE = Utils.NormalizePath("com/live2d/cubism");
        const string CEAppDef_CLASS = "g.class";
        const string CEIntegrity_CLASS = "h.class";
        const string MOD_LIB_PATH = "lib";

        static void Main(string[] args)
        {
            string? targetPath = null;
            bool shouldPatch = args.Any(
                argument => string.Equals(argument, "--patch", StringComparison.OrdinalIgnoreCase)
            );
            bool shouldRevoke = args.Any(
                argument => string.Equals(argument, "--revoke", StringComparison.OrdinalIgnoreCase)
            );
            int targetIndex = Array.FindIndex(
                args,
                argument =>
                    string.Equals(argument, "--target", StringComparison.OrdinalIgnoreCase)
            );
            if (targetIndex >= 0 && targetIndex + 1 < args.Length)
                targetPath = args[targetIndex + 1];

            AnsiConsole.Background = Color.Grey11;
            PrintHelp();
            bool oneShot = shouldPatch || shouldRevoke;

            ChooseAction:
            int choiceIndex;
            if (shouldPatch || shouldRevoke)
            {
                choiceIndex = shouldPatch ? 0 : 1;
                shouldPatch = false;
                shouldRevoke = false;
            }
            else
            {
                string[] actionList = { "Patch Pro License", "Revoke License", "Exit" };
                string actionPrompt = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("What would you like to do?")
                        .AddChoices(actionList)
                );
                choiceIndex = Array.FindIndex(actionList, item => item == actionPrompt);
            }

            string? execDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DialogResult? filePicker = null;

            switch (choiceIndex)
            {
                case 0: // Patch
                case 1: // Revoke
                    if (targetPath is not null)
                    {
                        AnsiConsole.MarkupLineInterpolated($"Using [green]{targetPath}[/].");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine(
                            "Selecting [bold orange1]Live2D Cubism[/] application or executable..."
                        );
                        filePicker = Dialog.FileOpen(
                            OperatingSystem.IsMacOS() ? "app" : "exe",
                            Path.GetDirectoryName(Utils.GetLastOpenedFile()) ?? execDirectory
                        );
                    }

                    if (filePicker?.IsCancelled == true)
                        goto ChooseAction;
                    else if (filePicker?.IsError == true)
                    {
                        AnsiConsole.MarkupLine("[red]Something is wrong.[/]");
                        goto ChooseAction;
                    }

                    break;
                case 2: // Exit
                default:
                    return;
            }

            string selectedPath = targetPath ?? filePicker!.Path;
            string? appLibPath = Utils.FindAppLibDirectory(selectedPath);
            if (appLibPath is null)
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"No Live2D Cubism data found near: {selectedPath}"
                );
                return;
            }

            // Save last opened file
            Utils.SaveLastOpenedFile(selectedPath ?? string.Empty);

            string? rlmName = Path.GetFileName(Utils.FindRlmJar(appLibPath));
            if (rlmName is null)
            {
                AnsiConsole.MarkupLine("[red]The installation does not contain a supported RLM JAR.[/]");
                return;
            }

            string rlmVersion = Path.GetFileNameWithoutExtension(rlmName).Replace("rlm", "rlm", StringComparison.Ordinal);
            string rlmPath = Path.Combine(appLibPath, rlmName);
            string oldRlmHash = Utils.GetSHA256(rlmPath);
            string modRlm = Path.Combine(
                execDirectory ?? string.Empty,
                MOD_LIB_PATH,
                rlmVersion,
                rlmName
            );
            if (!File.Exists(modRlm))
                modRlm = Path.Combine(execDirectory ?? string.Empty, MOD_LIB_PATH, rlmName);
            if (!File.Exists(modRlm))
                modRlm = Path.Combine(
                    FindRepositoryRoot(execDirectory),
                    "mod",
                    rlmVersion,
                    rlmName
                );
            if (!File.Exists(modRlm))
            {
                AnsiConsole.MarkupLineInterpolated(
                    $"[red]No matching mod found for {rlmName}: {modRlm}[/]"
                );
                return;
            }

            string rlmBackup = $"{rlmPath}.bak";
            string live2dMain = Path.Combine(appLibPath, LIVE2D_CUBISM_MAIN);
            string live2dMainBackup = $"{live2dMain}.bak";
            string modifiedRlmHash = Utils.GetSHA256(modRlm);
            string ceAppDefClass = Path.Combine(LIVE2D_CUBISM_PACKAGE, CEAppDef_CLASS);
            string ceIntegrityClass = Path.Combine(LIVE2D_CUBISM_PACKAGE, CEIntegrity_CLASS);

            switch (choiceIndex)
            {
                case 0:
                    goto Patch;
                case 1:
                    goto Revoke;
            }

            Patch:
            try
            {
                if (File.Exists(rlmBackup))
                {
                    AnsiConsole.MarkupLine(
                        "[red]An existing backup was found. Revoke it before patching again.[/]"
                    );
                    if (oneShot)
                        return;
                    goto ChooseAction;
                }

                File.Copy(rlmPath, rlmBackup, false); // Backup original rlm
                File.Copy(modRlm, rlmPath, true); // Copy MOD rlm to installation path

                AnsiConsole.MarkupLineInterpolated($"Patching [green]{selectedPath}[/]...");

                if (File.Exists(live2dMain))
                {
                    bool isRlm1603 = string.Equals(
                        rlmName,
                        "rlm1603.jar",
                        StringComparison.OrdinalIgnoreCase
                    );
                    Utils.PatchLive2DMainJar(
                        live2dMain,
                        live2dMainBackup,
                        isRlm1603 ? ceIntegrityClass : ceAppDefClass,
                        Utils.GetSHA256(rlmBackup),
                        Utils.GetSHA256(rlmPath)
                    );
                }

                AnsiConsole.MarkupLine(
                    "Done. If Live2D was signed on macOS, re-sign the application before launching it."
                );
                Console.WriteLine();
                if (oneShot)
                    return;
                goto ChooseAction;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"Failed to patch [red]{live2dMain}[/]!");
                AnsiConsole.WriteException(ex);

                // Return the original files if the patch failed after backups were created.
                if (File.Exists(rlmBackup))
                {
                    File.Copy(rlmBackup, rlmPath, true);
                    File.Delete(rlmBackup);
                }
            }

            Revoke:
            try
            {
                // Should we use SHA-256 to detect if it's the original/modded file?
                if (!File.Exists(rlmBackup))
                {
                    AnsiConsole.MarkupLine("[red]No Pro license applied![/]");
                    goto ChooseAction;
                }

                AnsiConsole.MarkupLineInterpolated($"Revoking license [green]{selectedPath}[/]...");

                string currentRlmHash = Utils.GetSHA256(rlmPath);
                string backupRlmHash = Utils.GetSHA256(rlmBackup);
                if (
                    string.IsNullOrEmpty(backupRlmHash)
                    || string.Equals(
                        currentRlmHash,
                        backupRlmHash,
                        StringComparison.Ordinal
                    )
                )
                {
                    throw new InvalidOperationException(
                        $"The current {rlmName} and its backup are not safe to restore."
                    );
                }

                AnsiConsole.MarkupLine("Getting the backup files...");
                File.Move(rlmBackup, rlmPath, true);

                if (File.Exists(live2dMainBackup))
                {
                    File.Copy(live2dMainBackup, live2dMain, true);
                    File.Delete(live2dMainBackup);
                }

                AnsiConsole.MarkupLine(
                    "Done. If Live2D was signed on macOS, re-sign the application before launching it."
                );
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLineInterpolated($"Failed to revoke license [red]{live2dMain}[/]");
                AnsiConsole.WriteException(ex);
            }

            Console.WriteLine();
            if (oneShot)
                return;
            goto ChooseAction;
        }

        static void PrintHelp()
        {
            AnsiConsole.MarkupLineInterpolated($"[bold pink3]Love Live2D v{VERSION}[/]");
            Console.WriteLine();
            AnsiConsole.MarkupLine(
                "Unlock the full power of [link=https://www.live2d.com/en/]Live2D Cubism[/] for free!"
            );
            AnsiConsole.MarkupLine(
                "For more information, visit: [link]https://github.com/ybbapp/LoveLive2D[/]"
            );
            Console.WriteLine();
            AnsiConsole.MarkupLine("[bold]Supported version[/]: 5.0.00+");
            Console.WriteLine();
        }

        static string FindRepositoryRoot(string? startDirectory)
        {
            foreach (string initial in new[]
                     {
                         startDirectory ?? string.Empty,
                         Directory.GetCurrentDirectory()
                     })
            {
                for (DirectoryInfo? current = new(initial);
                     current is not null;
                     current = current.Parent)
                {
                    if (
                        File.Exists(Path.Combine(current.FullName, "LoveLive2D.csproj"))
                        && Directory.Exists(Path.Combine(current.FullName, "mod"))
                    )
                        return current.FullName;
                }
            }

            return startDirectory ?? Directory.GetCurrentDirectory();
        }
    }
}
