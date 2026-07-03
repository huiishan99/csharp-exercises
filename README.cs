using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class MediaVolumeConfigLoader
{
    [Serializable]
    private class MediaVolumeConfigRoot
    {
        public MediaVolumeConfig media_volume;
        public MediaVolumeConfig mediaVolume;
        public MediaVolumeConfig MediaVolume;
    }

    private const string DefaultStreamingAssetsPath =
        "MediaVolume/media_volume_config.json";

    public static MediaVolumeConfig Load(string path)
    {
        string fullPath = ResolveFullPath(path);

        if (!File.Exists(fullPath))
        {
            Debug.LogWarning(
                "[MediaVolume] Config file not found: "
                + fullPath
                + ". Use default config."
            );

            return MediaVolumeConfig.CreateDefault();
        }

        try
        {
            string json = File.ReadAllText(fullPath, Encoding.UTF8);
            MediaVolumeConfig config = ParseConfig(json);

            if (config == null)
            {
                Debug.LogWarning(
                    "[MediaVolume] Failed to parse config. Use default config. path="
                    + fullPath
                );

                return MediaVolumeConfig.CreateDefault();
            }

            config.Normalize();

            Debug.Log(
                "[MediaVolume] Config loaded: "
                + fullPath
                + " min="
                + config.min.ToString("0.###")
                + " max="
                + config.max.ToString("0.###")
                + " default="
                + config.@default.ToString("0.###")
                + " step="
                + config.step.ToString("0.###")
            );

            return config;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[MediaVolume] Config load error: "
                + exception.Message
                + " path="
                + fullPath
            );

            return MediaVolumeConfig.CreateDefault();
        }
    }

    private static MediaVolumeConfig ParseConfig(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        MediaVolumeConfigRoot root = JsonUtility.FromJson<MediaVolumeConfigRoot>(json);

        if (root != null)
        {
            if (root.media_volume != null)
            {
                return root.media_volume;
            }

            if (root.mediaVolume != null)
            {
                return root.mediaVolume;
            }

            if (root.MediaVolume != null)
            {
                return root.MediaVolume;
            }
        }

        MediaVolumeConfig directConfig = JsonUtility.FromJson<MediaVolumeConfig>(json);

        if (directConfig == null)
        {
            return null;
        }

        return directConfig;
    }

    private static string ResolveFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Path.Combine(
                Application.streamingAssetsPath,
                DefaultStreamingAssetsPath
            );
        }

        string trimmedPath = path.Trim();

        if (trimmedPath.StartsWith("\"") && trimmedPath.EndsWith("\""))
        {
            trimmedPath = trimmedPath.Substring(1, trimmedPath.Length - 2);
        }

        // %USERPROFILE% などのWindows環境変数を展開する。
        string expandedPath = Environment.ExpandEnvironmentVariables(trimmedPath);

        // C:\... のような絶対Pathならそのまま使う。
        if (Path.IsPathRooted(expandedPath))
        {
            return expandedPath;
        }

        // 相対Pathの場合はStreamingAssets配下として扱う。
        return Path.Combine(
            Application.streamingAssetsPath,
            expandedPath
        );
    }
}
