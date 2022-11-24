using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity_Maki_Space.Scripts.Managers
{
    public class TextureManager : Manager
    {
        private ZipArchive archive;

        private readonly Dictionary<string, Texture2D> loadedTextures = new();
        private readonly Dictionary<string, Sprite> loadedSprites = new();

        public override Task Init()
        {
            // var resourcePackGuids = AssetDatabase.FindAssets("", new[] {"Assets/Resources/Resource Packs"});
            // if (resourcePackGuids.Length == 0) throw new Exception("No resource packs found");
            //
            // var resourcePackPath = AssetDatabase.GUIDToAssetPath(resourcePackGuids[0])
            //     .Replace("Assets/Resources/", "")
            //     .Replace(".bytes", "");

            var resourcePackBytes = Resources.Load<TextAsset>("Resource Packs/Dandelion+X+1.19b").bytes;
            var resourcePackStream = new MemoryStream(resourcePackBytes);

            archive = new ZipArchive(resourcePackStream, ZipArchiveMode.Read);

            return Task.CompletedTask;
        }

        public Texture2D GetTexture(string texturePath)
        {
            if (loadedTextures.ContainsKey(texturePath)) return loadedTextures[texturePath];

            var entry = archive.Entries.First(entry => entry.FullName == texturePath);
            var stream = entry.Open();
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            stream.Close();

            var texture = new Texture2D(1, 1);
            texture.LoadImage(memoryStream.ToArray());
            texture.filterMode = FilterMode.Point;

            loadedTextures[texturePath] = texture;

            return texture;
        }

        // public Texture2D GetCroppedTexture(string texturePath, RectInt rect)
        // {
        //     var texturePathWithCrop = $"{texturePath} {rect.width}x{rect.height}+{rect.x}+{rect.y}";
        //     if (loadedTextures.ContainsKey(texturePathWithCrop)) return loadedTextures[texturePathWithCrop];
        //     
        //     var uncroppedTexture = GetTexture(texturePath);
        //     var pixels = uncroppedTexture.GetPixels(rect.x, rect.y, rect.width, rect.height);
        //
        //     var texture = new Texture2D(rect.width, rect.height);
        //     texture.SetPixels(pixels);
        //     texture.filterMode = FilterMode.Point;
        //
        //     loadedTextures[texturePathWithCrop] = texture;
        //
        //     return texture;
        // }

        public Sprite GetCroppedSprite(string texturePath, Rect rect)
        {
            var texturePathWithCrop = $"{texturePath} {rect.width}x{rect.height}+{rect.x}+{rect.y}";
            if (loadedSprites.ContainsKey(texturePathWithCrop)) return loadedSprites[texturePathWithCrop];

            var sprite = Sprite.Create(GetTexture(texturePath), rect, new Vector2(0.5f, 0.5f));

            loadedSprites[texturePathWithCrop] = sprite;

            return sprite;
        }
    }
}