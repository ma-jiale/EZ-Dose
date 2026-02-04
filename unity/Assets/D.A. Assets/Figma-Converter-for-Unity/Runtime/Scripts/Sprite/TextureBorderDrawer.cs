using UnityEngine;

namespace DA_Assets.FCU
{
    public class TextureBorderDrawer
    {
        public static Texture2D CreateTextureWithBorder(int width, int height, int leftWidth, int rightWidth, int topWidth, int bottomWidth, Color borderColor)
        {
            Texture2D texture = new Texture2D(width, height);

            Color transparent = new Color(0, 0, 0, 0);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            if (leftWidth > 0)
                DrawLeftBorder(texture, leftWidth, borderColor);
            if (rightWidth > 0)
                DrawRightBorder(texture, rightWidth, borderColor);
            if (topWidth > 0)
                DrawTopBorder(texture, topWidth, borderColor);
            if (bottomWidth > 0)
                DrawBottomBorder(texture, bottomWidth, borderColor);

            texture.Apply();
            return texture;
        }

        private static void DrawLeftBorder(Texture2D texture, int borderWidth, Color borderColor)
        {
            for (int x = 0; x < borderWidth; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    texture.SetPixel(x, y, borderColor);
                }
            }
        }

        private static void DrawRightBorder(Texture2D texture, int borderWidth, Color borderColor)
        {
            for (int x = texture.width - borderWidth; x < texture.width; x++)
            {
                for (int y = 0; y < texture.height; y++)
                {
                    texture.SetPixel(x, y, borderColor);
                }
            }
        }

        private static void DrawTopBorder(Texture2D texture, int borderWidth, Color borderColor)
        {
            for (int y = texture.height - borderWidth; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, borderColor);
                }
            }
        }

        private static void DrawBottomBorder(Texture2D texture, int borderWidth, Color borderColor)
        {
            for (int y = 0; y < borderWidth; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, borderColor);
                }
            }
        }
    }
}

