using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Enters Play Mode once when the local codex-enter-play.flag file exists.
/// </summary>
[InitializeOnLoad]
public static class CodexEnterPlayOnce
{
    private const string FlagPath = "codex-enter-play.flag";

    static CodexEnterPlayOnce()
    {
        if (!File.Exists(FlagPath)) return;

        File.Delete(FlagPath);
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Debug.Log("[Codex] Entering Play Mode");
            EditorApplication.isPlaying = true;
        };
    }
}
