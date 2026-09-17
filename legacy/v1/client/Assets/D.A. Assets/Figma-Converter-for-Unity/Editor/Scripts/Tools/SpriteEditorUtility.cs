using UnityEditor;
using UnityEngine;
using DA_Assets.Shared.Extensions;

#if U2D_SPRITE_EXISTS
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
#endif

public class SpriteEditorUtility
{
    /// <summary>
    /// com.unity.2d.sprite@1.0.0\Documentation~\DataProvider.md
    /// </summary>
    public static void SetSpriteRects(Sprite sprite, Vector4 border)
    {
#if U2D_SPRITE_EXISTS
        SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
        factory.Init();

        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(sprite);
        if (dataProvider == null)
        {
            Debug.LogError(SharedLocKey.log_sprite_provider_not_found.Localize());
            return;
        }

        dataProvider.InitSpriteEditorDataProvider();
        SpriteRect[] spriteRects = dataProvider.GetSpriteRects();

        foreach (SpriteRect rect in spriteRects)
        {
            if (rect.spriteID == sprite.GetSpriteID())
            {
                rect.border = border;
                Debug.Log(SharedLocKey.log_sprite_border_updated.Localize(rect.border));
            }
        }

        dataProvider.SetSpriteRects(spriteRects);
        dataProvider.Apply();

        TextureImporter textureImporter = dataProvider.targetObject as TextureImporter;
        if (textureImporter != null)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            textureImporter.SetTextureSettings(settings);
            textureImporter.SaveAndReimport();

            Debug.Log(SharedLocKey.log_sprite_reimport_success.Localize());
        }
        else
        {
            Debug.LogError(SharedLocKey.log_sprite_reimport_failed.Localize());
        }
#endif
    }
}
