using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace ScreenOcrTool
{
    internal sealed class OcrService
    {
        private readonly OcrEngine _engine;

        public string DisplayLanguage { get; private set; }
        public string Warning { get; private set; }
        public bool IsChinese { get; private set; }

        private OcrService(OcrEngine engine, string displayLanguage, bool isChinese, string warning)
        {
            _engine = engine;
            DisplayLanguage = displayLanguage;
            IsChinese = isChinese;
            Warning = warning;
        }

        public static OcrService Create()
        {
            IReadOnlyList<Language> languages;
            try
            {
                languages = OcrEngine.AvailableRecognizerLanguages;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("无法使用 Windows 自带的文字识别组件。", ex);
            }

            if (languages == null || languages.Count == 0)
            {
                throw new InvalidOperationException("系统中没有安装任何 OCR 文字识别语言包。");
            }

            Language chosen = null;

            // 优先使用简体中文，它可以同时识别屏幕上的中英文。
            foreach (Language language in languages)
            {
                if (language.LanguageTag.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase))
                {
                    chosen = language;
                    break;
                }
            }

            // 没有简体中文时，选择与当前系统语言最接近的识别包。
            if (chosen == null)
            {
                string uiLanguage = CultureInfo.CurrentUICulture.Name;
                foreach (Language language in languages)
                {
                    if (language.LanguageTag.Equals(uiLanguage, StringComparison.OrdinalIgnoreCase) ||
                        language.LanguageTag.StartsWith(uiLanguage + "-", StringComparison.OrdinalIgnoreCase))
                    {
                        chosen = language;
                        break;
                    }
                }
            }

            if (chosen == null)
            {
                foreach (Language language in languages)
                {
                    if (language.LanguageTag.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                    {
                        chosen = language;
                        break;
                    }
                }
            }

            if (chosen == null)
            {
                chosen = languages[0];
            }

            OcrEngine engine = OcrEngine.TryCreateFromLanguage(chosen);
            if (engine == null)
            {
                engine = OcrEngine.TryCreateFromUserProfileLanguages();
            }
            if (engine == null)
            {
                throw new InvalidOperationException("无法加载 OCR 识别引擎，请确认系统语言包完整。");
            }

            bool isChinese = chosen.LanguageTag.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            string display = SimplifyLanguageName(chosen);
            string warning = null;
            if (!isChinese)
            {
                warning = "系统没有安装中文识别包，当前只能按 " + display + " 识别。";
            }

            return new OcrService(engine, display, isChinese, warning);
        }

        private static string SimplifyLanguageName(Language language)
        {
            string tag = language.LanguageTag ?? string.Empty;
            if (tag.StartsWith("zh-Hans", StringComparison.OrdinalIgnoreCase))
            {
                return "简体中文";
            }
            if (tag.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase))
            {
                return "繁体中文";
            }
            if (tag.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return "中文";
            }
            return string.IsNullOrWhiteSpace(language.DisplayName) ? tag : language.DisplayName;
        }

        public async Task<string> RecognizeAsync(Bitmap source)
        {
            if (source == null)
            {
                return string.Empty;
            }

            using (Bitmap prepared = PrepareImage(source))
            using (SoftwareBitmap softwareBitmap = ToSoftwareBitmap(prepared))
            {
                OcrResult result;
                try
                {
                    result = await _engine.RecognizeAsync(softwareBitmap);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("文字识别失败，请重试。", ex);
                }

                List<string> lines = new List<string>();
                foreach (OcrLine line in result.Lines)
                {
                    string cleaned = CleanLine(line.Text);
                    if (!string.IsNullOrWhiteSpace(cleaned))
                    {
                        lines.Add(cleaned);
                    }
                }

                return string.Join(Environment.NewLine, lines.ToArray());
            }
        }

        private static Bitmap PrepareImage(Bitmap source)
        {
            int sourceWidth = source.Width;
            int sourceHeight = source.Height;
            if (sourceWidth < 2 || sourceHeight < 2)
            {
                return new Bitmap(source);
            }

            // 屏幕字体通常偏小，放大后再识别会准很多；超大截图则控制尺寸避免太慢。
            double scale = 1.0;
            int longestSide = Math.Max(sourceWidth, sourceHeight);
            if (longestSide <= 1200)
            {
                scale = 3.0;
            }
            else if (longestSide <= 2000)
            {
                scale = 2.0;
            }
            else if (longestSide <= 3200)
            {
                scale = 1.5;
            }

            int maxDimension = 6000;
            if (longestSide * scale > maxDimension)
            {
                scale = (double)maxDimension / longestSide;
            }

            int drawWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale));
            int drawHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale));
            int padding = Math.Max(8, (int)Math.Round(12 * scale));

            Color background = GetEdgeColor(source);
            bool invertForDarkBackground = GetBrightness(background) < 128;

            Bitmap prepared = new Bitmap(drawWidth + padding * 2, drawHeight + padding * 2, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(prepared))
            {
                graphics.Clear(background);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                // 转成灰度再识别，可以避开 ClearType 彩边造成的笔画误判。
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    ColorMatrix grayscale = new ColorMatrix(new float[][]
                    {
                        new float[] { 0.299f, 0.299f, 0.299f, 0f, 0f },
                        new float[] { 0.587f, 0.587f, 0.587f, 0f, 0f },
                        new float[] { 0.114f, 0.114f, 0.114f, 0f, 0f },
                        new float[] { 0f, 0f, 0f, 1f, 0f },
                        new float[] { 0f, 0f, 0f, 0f, 1f }
                    });
                    attributes.SetColorMatrix(grayscale);
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(
                        source,
                        new Rectangle(padding, padding, drawWidth, drawHeight),
                        0,
                        0,
                        sourceWidth,
                        sourceHeight,
                        GraphicsUnit.Pixel,
                        attributes);
                }
            }

            Binarize(prepared, 150, invertForDarkBackground);
            return prepared;
        }

        private static int GetBrightness(Color color)
        {
            return (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
        }

        private static void Binarize(Bitmap bitmap, int threshold, bool invert)
        {
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            try
            {
                byte[] pixels = new byte[data.Stride * bitmap.Height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

                for (int y = 0; y < bitmap.Height; y++)
                {
                    int row = y * data.Stride;
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int index = row + x * 4;
                        bool isWhite = pixels[index] > threshold;
                        if (invert)
                        {
                            isWhite = !isWhite;
                        }

                        byte value = isWhite ? (byte)255 : (byte)0;
                        pixels[index] = value;
                        pixels[index + 1] = value;
                        pixels[index + 2] = value;
                        pixels[index + 3] = 255;
                    }
                }

                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private static Color GetEdgeColor(Bitmap bitmap)
        {
            int stepX = Math.Max(1, bitmap.Width / 40);
            int stepY = Math.Max(1, bitmap.Height / 40);
            long brightness = 0;
            int count = 0;

            for (int y = 0; y < bitmap.Height; y += stepY)
            {
                for (int x = 0; x < bitmap.Width; x += stepX)
                {
                    Color color = bitmap.GetPixel(x, y);
                    brightness += (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
                    count++;
                }
            }

            if (count == 0)
            {
                return Color.White;
            }

            int average = (int)(brightness / count);
            return average < 120 ? Color.FromArgb(24, 24, 24) : Color.White;
        }

        private static SoftwareBitmap ToSoftwareBitmap(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                byte[] pixels = new byte[data.Stride * height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

                if (data.Stride != width * 4)
                {
                    byte[] packed = new byte[width * height * 4];
                    for (int y = 0; y < height; y++)
                    {
                        Buffer.BlockCopy(pixels, y * data.Stride, packed, y * width * 4, width * 4);
                    }
                    pixels = packed;
                }

                return SoftwareBitmap.CreateCopyFromBuffer(
                    WindowsRuntimeBufferExtensions.AsBuffer(pixels),
                    BitmapPixelFormat.Bgra8,
                    width,
                    height,
                    BitmapAlphaMode.Premultiplied);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        private static string CleanLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            text = text.Replace('\u00A0', ' ')
                       .Replace('\u3000', ' ')
                       .Replace('\uFF0E', '.');
            text = Regex.Replace(text, "[\u200B-\u200D\uFEFF]", string.Empty);
            text = Regex.Replace(text, "[ \t]+", " ").Trim();

            StringBuilder builder = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];
                if (current == ' ')
                {
                    int previousIndex = i - 1;
                    while (previousIndex >= 0 && text[previousIndex] == ' ')
                    {
                        previousIndex--;
                    }

                    int nextIndex = i + 1;
                    while (nextIndex < text.Length && text[nextIndex] == ' ')
                    {
                        nextIndex++;
                    }

                    if (previousIndex >= 0 && nextIndex < text.Length &&
                        (IsWideCharacter(text[previousIndex]) || IsWideCharacter(text[nextIndex])))
                    {
                        // Windows 中文识别会在汉字之间插入空格，这里自动去掉。
                        continue;
                    }
                }

                builder.Append(current);
            }

            string result = Regex.Replace(builder.ToString(), " {2,}", " ").Trim();
            return NormalizePunctuation(result);
        }

        private static string NormalizePunctuation(string text)
        {
            // 数字中的全角逗号、间隔点，通常是识别器把半角逗号/小数点认错了。
            text = Regex.Replace(text, "(?<=[0-9])，(?=[0-9])", ",");
            text = Regex.Replace(text, "(?<=[0-9])[·。](?=[0-9])", ".");
            // 字母、数字之间的“一”通常是识别器把连字符认错了。
            text = Regex.Replace(text, "(?<=[A-Za-z0-9])一(?=[A-Za-z0-9])", "-");
            // 金额符号和数字之间不需要空格。
            text = Regex.Replace(text, @"(?<=[¥￥$€£])\s+(?=[0-9])", string.Empty);
            // 数字中间的小数点、千位逗号两侧不应有空格。
            text = Regex.Replace(text, @"(?<=[0-9])\s*([.,])\s*(?=[0-9])", "$1");
            return text;
        }

        private static bool IsWideCharacter(char c)
        {
            if (c >= '\u2E80' && c <= '\u9FFF')
            {
                return true;
            }
            if (c >= '\u3000' && c <= '\u303F')
            {
                return true;
            }
            if (c >= '\uF900' && c <= '\uFAFF')
            {
                return true;
            }
            if (c >= '\uFE30' && c <= '\uFE4F')
            {
                return true;
            }
            if (c >= '\uFF00' && c <= '\uFFEF')
            {
                return true;
            }
            if (c == '\u2018' || c == '\u2019' || c == '\u201C' || c == '\u201D' ||
                c == '\u2026' || c == '\u2014' || c == '\u00B7')
            {
                return true;
            }
            return false;
        }
    }
}
