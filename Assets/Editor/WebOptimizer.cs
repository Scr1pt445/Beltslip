using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Применяет настройки релизной WebGL-сборки (Gzip + Decompression Fallback для itch.io)
// и собирает архив Builds/Beltslip_WebGL.zip с index.html в корне.
public static class WebOptimizer
{
    const int WebWidth = 960;
    const int WebHeight = 540;
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string BuildDir = "Builds/WebGL";
    const string ZipPath = "Builds/Beltslip_WebGL.zip";

    [InitializeOnLoadMethod]
    static void EnsureItchWebSettings()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        bool changed = false;

        if (PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Gzip)
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            changed = true;
        }

        if (!PlayerSettings.WebGL.decompressionFallback)
        {
            PlayerSettings.WebGL.decompressionFallback = true;
            changed = true;
        }

        if (PlayerSettings.defaultWebScreenWidth != WebWidth)
        {
            PlayerSettings.defaultWebScreenWidth = WebWidth;
            changed = true;
        }

        if (PlayerSettings.defaultWebScreenHeight != WebHeight)
        {
            PlayerSettings.defaultWebScreenHeight = WebHeight;
            changed = true;
        }

        if (changed)
            AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Apply Web Release Settings")]
    public static void Optimize()
    {
        var target = NamedBuildTarget.WebGL;
        PlayerSettings.SetIl2CppCodeGeneration(target, Il2CppCodeGeneration.OptimizeSize);
        PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.High);
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.stripUnusedMeshComponents = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;
        PlayerSettings.defaultWebScreenWidth = WebWidth;
        PlayerSettings.defaultWebScreenHeight = WebHeight;
        UnityEditor.WebGL.UserBuildSettings.codeOptimization =
            UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;

        AssetDatabase.SaveAssets();

        Debug.Log(
            "Web release settings applied and saved:\n"
            + $"  il2cppCodeGeneration = {PlayerSettings.GetIl2CppCodeGeneration(target)}\n"
            + $"  managedStrippingLevel = {PlayerSettings.GetManagedStrippingLevel(target)}\n"
            + $"  stripEngineCode = {PlayerSettings.stripEngineCode}\n"
            + $"  stripUnusedMeshComponents = {PlayerSettings.stripUnusedMeshComponents}\n"
            + $"  dataCaching = {PlayerSettings.WebGL.dataCaching}\n"
            + $"  compressionFormat = {PlayerSettings.WebGL.compressionFormat}\n"
            + $"  decompressionFallback = {PlayerSettings.WebGL.decompressionFallback}\n"
            + $"  exceptionSupport = {PlayerSettings.WebGL.exceptionSupport}\n"
            + $"  debugSymbolMode = {PlayerSettings.WebGL.debugSymbolMode}\n"
            + $"  resolution = {PlayerSettings.defaultWebScreenWidth}x{PlayerSettings.defaultWebScreenHeight}\n"
            + $"  codeOptimization = {UnityEditor.WebGL.UserBuildSettings.codeOptimization}");
    }

    [MenuItem("Tools/Build WebGL + Pack itch.io ZIP")]
    public static void BuildWebGLAndZip()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("Не удалось переключить платформу сборки на WebGL.");
                return;
            }
        }

        Optimize();

        Directory.CreateDirectory(BuildDir);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = BuildDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Сборка WebGL завершилась со статусом: {report.summary.result} (ошибок: {report.summary.totalErrors}).");
            return;
        }

        if (File.Exists(ZipPath))
            File.Delete(ZipPath);

        ZipFile.CreateFromDirectory(BuildDir, ZipPath, System.IO.Compression.CompressionLevel.Optimal, false);
        Debug.Log($"Сборка WebGL и архив для itch.io готовы: {Path.GetFullPath(ZipPath)} ({report.summary.totalSize / (1024f * 1024f):F2} MB)");
    }
}
