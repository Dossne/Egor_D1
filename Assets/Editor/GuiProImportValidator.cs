using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GuiProImportValidator
{
    [MenuItem("Tools/Farm Merger/Validate GUI Pro Import")]
    public static void ValidateImport()
    {
        var missingFolders = new List<string>();
        var expectedFolderMarkers = new[]
        {
            "Assets/GUI Pro - Fantasy RPG/UI",
            "Assets/GUI Pro - Fantasy RPG/Fonts",
            "Assets/GUI Pro - Fantasy RPG/Icons"
        };

        foreach (var folder in expectedFolderMarkers)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                missingFolders.Add(folder);
            }
        }

        if (missingFolders.Count > 0)
        {
            Debug.LogWarning("GUI Pro import validation: missing expected folders:\n - " + string.Join("\n - ", missingFolders));
        }
        else
        {
            Debug.Log("GUI Pro import validation: expected UI, Fonts and Icons folders found.");
        }

        ValidateNineSliceBorders();
    }

    private static void ValidateNineSliceBorders()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/GUI Pro - Fantasy RPG" });
        var missingBorders = new List<string>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
            {
                continue;
            }

            var spriteImportMode = importer.spriteImportMode;
            if (spriteImportMode == SpriteImportMode.None)
            {
                continue;
            }

            var hasAnyBorder = false;

            if (spriteImportMode == SpriteImportMode.Single)
            {
                hasAnyBorder = importer.spriteBorder.sqrMagnitude > 0f;
            }
            else
            {
                var spritesheet = importer.spritesheet;
                foreach (var meta in spritesheet)
                {
                    if (meta.border.sqrMagnitude > 0f)
                    {
                        hasAnyBorder = true;
                        break;
                    }
                }
            }

            if (!hasAnyBorder)
            {
                missingBorders.Add(path);
            }
        }

        if (missingBorders.Count == 0)
        {
            Debug.Log("GUI Pro import validation: all sprite textures contain at least one 9-slice border.");
            return;
        }

        Debug.LogWarning(
            "GUI Pro import validation: the following sprite textures have no 9-slice border set:\n - " +
            string.Join("\n - ", missingBorders)
        );
    }
}
