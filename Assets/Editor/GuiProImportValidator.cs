using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class GuiProImportValidator
{
    private const string PackageMarker = "GUI Pro - Fantasy RPG";

    [MenuItem("Tools/Farm Merger/Validate GUI Pro Import")]
    public static void ValidateImport()
    {
        var packageRoots = FindPackageRoots();
        ValidateExpectedFolders(packageRoots);
        ValidateNineSliceBorders(packageRoots);
    }

    private static List<string> FindPackageRoots()
    {
        var roots = AssetDatabase.GetSubFolders("Assets")
            .Where(path => path.Contains(PackageMarker))
            .ToList();

        if (roots.Count == 0)
        {
            Debug.LogWarning($"GUI Pro import validation: package root containing '{PackageMarker}' was not found under Assets/.");
        }

        return roots;
    }

    private static void ValidateExpectedFolders(List<string> packageRoots)
    {
        var expectedSuffixes = new[] { "/UI", "/Fonts", "/Icons" };
        var missingFolders = new List<string>();

        foreach (var suffix in expectedSuffixes)
        {
            var hasFolder = packageRoots.Any(root => AssetDatabase.IsValidFolder(root + suffix));
            if (!hasFolder)
            {
                missingFolders.Add($"...{suffix}");
            }
        }

        if (missingFolders.Count > 0)
        {
            Debug.LogWarning("GUI Pro import validation: missing expected folders:\n - " + string.Join("\n - ", missingFolders));
            return;
        }

        Debug.Log("GUI Pro import validation: expected UI, Fonts and Icons folders found.");
    }

    private static void ValidateNineSliceBorders(List<string> packageRoots)
    {
        if (packageRoots.Count == 0)
        {
            return;
        }

        var missingBorders = new List<string>();
        var guids = AssetDatabase.FindAssets("t:Texture2D", packageRoots.ToArray());

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (sprites.Length == 0)
            {
                continue;
            }

            if (!IsNineSliceCandidate(path, sprites))
            {
                continue;
            }

            var hasBorder = sprites.Any(sprite => sprite.border.sqrMagnitude > 0f);
            if (!hasBorder)
            {
                missingBorders.Add(path);
            }
        }

        if (missingBorders.Count == 0)
        {
            Debug.Log("GUI Pro import validation: all candidate panel/button sprites contain 9-slice borders.");
            return;
        }

        Debug.LogWarning(
            "GUI Pro import validation: candidate panel/button sprites without 9-slice border:\n - " +
            string.Join("\n - ", missingBorders)
        );
    }

    private static bool IsNineSliceCandidate(string texturePath, Sprite[] sprites)
    {
        var markers = new[] { "panel", "button", "window", "frame", "box" };
        var pathLower = texturePath.ToLowerInvariant();

        if (markers.Any(marker => pathLower.Contains(marker)))
        {
            return true;
        }

        return sprites.Any(sprite =>
        {
            var nameLower = sprite.name.ToLowerInvariant();
            return markers.Any(marker => nameLower.Contains(marker));
        });
    }
}
