#!/bin/bash
set -euo pipefail

VERSION="1.0.0"
# Build Love Live2D
echo "Building Love Live2D..."
# dotnet build -c Release
# echo "Build process completed for all target OS architectures."
# Define the target operating systems and architectures
TARGET_OS_ARCHITECTURES=("win-x64" "win-arm64" "osx-x64" "osx-arm64")
TARGET_FRAMEWORKS=("net6.0" "net7.0" "net8.0")

# Build the project for each target OS architecture
for os_arch in "${TARGET_OS_ARCHITECTURES[@]}"
do
    for framework in "${TARGET_FRAMEWORKS[@]}"
    do
        echo "Building for $os_arch architecture..."
        dotnet publish -c Release -f "$framework" -r "$os_arch" --no-self-contained

        # Check if build was successful
        if [ $? -eq 0 ]; then
            echo "Build for $os_arch completed successfully."
        else
            echo "Build for $os_arch failed."
            exit 1  # Exit the script with an error code
        fi
    done
done

echo "Build process completed for all target OS architectures."

MANIFEST="src/rlm1501/resources/META-INF/MANIFEST.MF"
MOD_VERSIONS=("rlm1501" "rlm1603")
for mod_version in "${MOD_VERSIONS[@]}"
do
    if [[ "$mod_version" == "rlm1501" ]]; then
        SOURCE_DIR="src/rlm1501/sources/com/reprisesoftware/rlm"
    else
        SOURCE_DIR="src/rlm1603/com/reprisesoftware/rlm"
    fi

    OUTPUT_DIR="bin/Release/${mod_version}/classes"
    OUTPUT_JAR="mod/${mod_version}/${mod_version}.jar"
    mkdir -p "${OUTPUT_DIR}"
    rm -rf "${OUTPUT_DIR}"/*

    echo "Building ${mod_version}..."
    javac --release 17 -d "${OUTPUT_DIR}" "${SOURCE_DIR}"/*.java -Xlint:unchecked
    jar cfm "${OUTPUT_JAR}.new" "${MANIFEST}" -C "${OUTPUT_DIR}" .
    mv "${OUTPUT_JAR}.new" "${OUTPUT_JAR}"
done

mkdir -p lib
cp mod/rlm1501/rlm1501.jar lib/rlm1501.jar
cp mod/rlm1603/rlm1603.jar lib/rlm1603.jar

# Create "lib" directory to store modified "rlm1501.jar"
echo "Distributing rlm1501.jar for every architecture..."
for framework in "${TARGET_FRAMEWORKS[@]}"
do
    for os_arch in "${TARGET_OS_ARCHITECTURES[@]}"
    do
        # Copy version-specific mod JARs to every architecture
        MOD_PATH="bin/Release/${framework}/${os_arch}/publish/lib"
        mkdir -p "${MOD_PATH}"
        cp mod/rlm1501/rlm1501.jar "${MOD_PATH}/"
        cp mod/rlm1603/rlm1603.jar "${MOD_PATH}/"
    done

    for os_arch in "${TARGET_OS_ARCHITECTURES[@]}"
    do
        # Make a zip file for every architecture to be distributed
        ZIP_OUTPUT="LoveLive2D-v${VERSION}-${framework}-${os_arch}.zip"
        cd "bin/Release/${framework}/${os_arch}/publish/"
        zip -r "../../../${ZIP_OUTPUT}" *
        cd "../../../../../" # build.sh directory
    done
done

if [[ "$(uname -s)" == "Darwin" ]]; then
    echo
    echo "Generated macOS artifacts:"
    find bin/Release -maxdepth 1 -name 'LoveLive2D-*-osx-*.zip' -print | sort
    echo
    echo "The artifacts are framework-dependent. Keep the matching .NET runtime installed."
    echo "If Live2D is code-signed, re-sign its .app bundle after patching it."
fi
