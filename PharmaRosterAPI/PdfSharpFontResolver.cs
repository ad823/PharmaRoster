using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.IO;

namespace PharmaRosterAPI
{
    /// <summary>
    /// PDFSharp 自訂字型解析器
    /// 說明：
    /// 1. 優先使用專案內 /fonts 目錄的字型檔
    /// 2. 若找不到，再 fallback 到 Windows 字型路徑
    /// 3. 預設字型為「標楷體」
    /// 4. 支援 Noto Sans TC Regular / Bold
    /// </summary>
    public class CustomFontResolver : IFontResolver
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        // FaceName（給 PDFSharp 用）
        private const string FaceKaiu = "Kaiu-Regular";
        private const string FaceNotoRegular = "NotoSansTC-Regular";
        private const string FaceNotoBold = "NotoSansTC-Bold";

        /// <summary>
        /// 預設字型名稱
        /// </summary>
        public string DefaultFontName => "標楷體";

        /// <summary>
        /// faceName -> 實體字型檔路徑
        /// </summary>
        private readonly Dictionary<string, string> _faceToPath;

        public CustomFontResolver()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectFontDir = Path.Combine(baseDir, "fonts");

            _faceToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // 預設：標楷體
                { FaceKaiu, ResolveFontPath(projectFontDir, "kaiu.ttf", @"C:\Windows\Fonts\kaiu.ttf") },

                // Noto Sans TC
                { FaceNotoRegular, ResolveFontPath(projectFontDir, "NotoSansTC-Regular.ttf", @"C:\Windows\Fonts\NotoSansTC-Regular.ttf") },
                { FaceNotoBold, ResolveFontPath(projectFontDir, "NotoSansTC-Bold.ttf", @"C:\Windows\Fonts\NotoSansTC-Bold.ttf") }
            };
        }

        /// <summary>
        /// 確保全域 FontResolver 只初始化一次
        /// </summary>
        public static void EnsurePdfSharpFontResolver()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                if (GlobalFontSettings.FontResolver == null)
                {
                    GlobalFontSettings.FontResolver = new CustomFontResolver();
                }

                _initialized = true;
            }
        }

        /// <summary>
        /// faceName -> 讀取字型 bytes
        /// </summary>
        public byte[] GetFont(string faceName)
        {
            if (_faceToPath.TryGetValue(faceName, out string path) && File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }

            Console.WriteLine($"⚠ 找不到字型 face: {faceName}，改用預設字型 {DefaultFontName}");

            if (_faceToPath.TryGetValue(FaceKaiu, out string defaultPath) && File.Exists(defaultPath))
            {
                return File.ReadAllBytes(defaultPath);
            }

            throw new FileNotFoundException($"找不到預設 PDF 字型檔：{DefaultFontName}");
        }

        /// <summary>
        /// familyName + style -> 對應 faceName
        /// </summary>
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            string normalized = (familyName ?? "").Trim();

            // 標楷體 / DFKai-SB
            if (normalized.Equals("標楷體", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("DFKai-SB", StringComparison.OrdinalIgnoreCase))
            {
                return new FontResolverInfo(FaceKaiu);
            }

            // Noto Sans TC
            if (normalized.Equals("Noto Sans TC", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("NotoSansTC", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("Noto Sans TC Regular", StringComparison.OrdinalIgnoreCase))
            {
                if (isBold)
                {
                    return new FontResolverInfo(FaceNotoBold);
                }

                return new FontResolverInfo(FaceNotoRegular);
            }

            // fallback 預設字型：標楷體
            return new FontResolverInfo(FaceKaiu);
        }

        /// <summary>
        /// 優先取專案內字型，若不存在則 fallback 到指定系統路徑
        /// </summary>
        private string ResolveFontPath(string projectFontDir, string projectFontFileName, string fallbackSystemPath)
        {
            string projectPath = Path.Combine(projectFontDir, projectFontFileName);
            if (File.Exists(projectPath))
            {
                return projectPath;
            }

            return fallbackSystemPath;
        }
    }
}