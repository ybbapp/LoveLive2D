<div align="center">
  <h1>Love 💖 Live2D</h1>
  <img src="./uwoh.gif" />
  <p>Make <a href="https://www.live2d.com/en/" title="Visit Live2D official website">Live2D Cubism</a> Pro ✨</p>
  <br/>
  <p>You want to learn Live2D Cubism with all its features, but the trial time is limited and you can't buy a Pro license due to cost constraints.<br/>Don't worry! This tool is for you. Unlock the full power of Live2D Cubism!</p>
</div>

## Supported Versions

- Live2D Cubism v5.0.00 and v5.3.x

## Prerequisites

- [Live2D Cubism](https://www.live2d.com/en/cubism/download/editor/ "Live2D Cubism")
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0 ".NET 8.0 SDK")
- Java 17 JDK, used to build the bundled RLM compatibility JARs

## How To Use

- Install Live2D Cubism as usual.
- Download Love Live2D at [Releases](https://github.com/kiraio-moe/LoveLive2D/ "Releases").
- On Windows, run `LoveLive2D.exe` (_Run as Administrator if necessary_).
- On macOS, run `LoveLive2D` and select the Live2D Cubism `.app` bundle.
- Follow the instructions.

### macOS one-click installation

Run:

```sh
curl -fsSL https://raw.githubusercontent.com/ybbapp/LoveLive2D/main/install-macos.sh | bash
```

The release bundle includes the matching .NET runtime. The installer detects
Intel or Apple Silicon, installs into
`~/Applications/LoveLive2D`, and creates `~/Applications/LoveLive2D/LoveLive2D.command`.
Double-click that `.command` file to start the tool.

When building from a cloned source tree, install the
[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), then run:

```sh
chmod +x install-macos.sh
./install-macos.sh
```

You can choose another install location with `LOVELIVE2D_HOME`:

```sh
LOVELIVE2D_HOME="$HOME/tools/LoveLive2D" ./install-macos.sh
```

Set `LOVELIVE2D_RELEASE_URL` to test a specific self-contained package:

```sh
LOVELIVE2D_RELEASE_URL="https://github.com/ybbapp/LoveLive2D/releases/download/v1.0.0/LoveLive2D-v1.0.0-net8.0-osx-arm64-self-contained.zip" ./install-macos.sh
```

The installer downloads the self-contained GitHub release asset. If that download
fails and the script is being run from a cloned source tree, it builds and publishes
the selected macOS architecture locally.

For non-interactive testing or automation:

```sh
LoveLive2D --patch --target "/Applications/Live2D Cubism 5.0/Live2D Cubism Editor 5.0.app"
LoveLive2D --revoke --target "/Applications/Live2D Cubism 5.0/Live2D Cubism Editor 5.0.app"
```

## macOS

Both Intel (`osx-x64`) and Apple Silicon (`osx-arm64`) builds are supported. The
published application is framework-dependent, so install the matching .NET runtime.
Live2D 5.0 uses `rlm1501.jar`; Live2D 5.3 uses `rlm1603.jar` and also checks a
recorded SHA-256. The tool patches the matching RLM JAR and the expected hash.

Changing a JAR inside a signed `.app` invalidates its signature. If the application
does not launch after patching or revoking, re-sign the bundle on your Mac. The tool
backs up `Live2D_Cubism.jar` and the RLM JAR beside their originals and provides a
revoke action to restore them.

## Disclaimer

If you have more money or want to seriously use Live2D Cubism for commercial purposes, it is recommended to buy the genuine license so that the developer can continue developing Live2D Cubism.

This tool is provided for educational and evaluational purposes only. Modifying software without proper authorization may violate terms of service and can lead to legal consequences. Use this tool responsibly and at your own risk.
