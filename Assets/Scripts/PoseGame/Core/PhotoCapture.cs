using System;
using System.IO;
using UnityEngine;

namespace PoseGame
{
    // Saves a "決め" screenshot (whole game view, including the skeleton overlay and UI) to
    // disk whenever a pose is locked in, like a photo-booth flash at the decisive moment.
    public static class PhotoCapture
    {
        public static string SaveDirectory => Path.Combine(Application.persistentDataPath, "PoseMatchPhotos");

        public static Texture2D CaptureScreen()
        {
            return ScreenCapture.CaptureScreenshotAsTexture();
        }

        public static string Save(Texture2D texture, string fileNameWithoutExt)
        {
            Directory.CreateDirectory(SaveDirectory);
            var safeName = fileNameWithoutExt + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            var path = Path.Combine(SaveDirectory, safeName);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            return path;
        }
    }
}
