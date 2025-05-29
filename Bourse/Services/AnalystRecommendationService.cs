using Microsoft.Playwright;
using System.Drawing;
using System;
using System.Text.RegularExpressions;
using Tesseract;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class AnalystRecommendationService
{
    private const string Url = "https://finance.yahoo.com/quote/";
    private const string ImagePath = "canvas.png";

    public async Task<IDictionary<string, int>> FetchRecommendationsAsync(string symbol)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        await page.GotoAsync(Url + symbol + "/analysis/");
        await page.WaitForTimeoutAsync(1000); // Laisse le temps au canvas de charger

        var canvasLocator = page.Locator("section[data-testid='analyst-recommendations-card'] canvas");

        // Vérifie s'il existe au moins un canvas dans la section ciblée
        if (await canvasLocator.CountAsync() == 0)
        {
            // Aucun canvas trouvé, retour "sûr"
            return new Dictionary<string, int>
            {
                ["Strong Buy"] = 0,
                ["Buy"] = 0,
                ["Hold"] = 0,
                ["Sell"] = 0,
                ["Strong Sell"] = 0
            };
        }

        //// Scroll au cas où le canvas n’est pas visible
        //await canvasLocator.ScrollIntoViewIfNeededAsync();

        // Extraire l'image base64 du canvas
        string base64 = await page.EvalOnSelectorAsync<string>(
            "section[data-testid='analyst-recommendations-card'] canvas",
            "canvas => canvas.toDataURL('image/png').split(',')[1]"
        );

        // Convertir en image
        byte[] imageBytes = Convert.FromBase64String(base64);
        await File.WriteAllBytesAsync(ImagePath, imageBytes);

        // OCR avec Tesseract
        //return PerformOcr(ImagePath);
        return AnalyzeBarFromCanvas(ImagePath);
    }

    private Dictionary<string, int> PerformOcr(string imagePath)
    {
        var resultDict = new Dictionary<string, int>();
        using var ocrEngine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
        using var img = Pix.LoadFromFile(imagePath);
        using var result = ocrEngine.Process(img);

        var text = result.GetText();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        var regex = new Regex(@"(?<label>Strong Buy|Buy|Hold|Sell|Strong Sell)\D*(?<count>\d+)", RegexOptions.IgnoreCase);
        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (match.Success)
            {
                string label = match.Groups["label"].Value.Trim();
                int count = int.Parse(match.Groups["count"].Value);
                resultDict[label] = count;
            }
        }

        return resultDict;
    }

    public Dictionary<string, int> ExtractRecommendations(string imagePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("This method is supported only on Windows.");

        var recommendations = new Dictionary<string, int>();
        var categories = new[] { "Strong Buy", "Buy", "Hold", "Sell", "Strong Sell" };

        using var image = new Bitmap(imagePath);

        // Recadrage : la barre droite (May) — ajusté à ton image spécifique (93px largeur totale)
        int barWidth = 25;
        int barHeight = image.Height;
        int barX = image.Width - barWidth; // partie droite
        var lastBar = new Bitmap(barWidth, barHeight);

        using (Graphics g = Graphics.FromImage(lastBar))
        {
            g.DrawImage(image, new Rectangle(0, 0, barWidth, barHeight), new Rectangle(barX, 0, barWidth, barHeight), GraphicsUnit.Pixel);
        }

        // Découper la barre en 5 zones verticales
        int segmentHeight = barHeight / 5;

        using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
        engine.SetVariable("tessedit_char_whitelist", "0123456789");

        for (int i = 0; i < 5; i++)
        {
            Rectangle segmentRect = new Rectangle(0, i * segmentHeight, barWidth, segmentHeight);
            using var segment = lastBar.Clone(segmentRect, lastBar.PixelFormat);
            Bitmap processed = PreprocessForOcr(segment);

            using var pix = BitmapToPix(processed);
            processed.Save($"segment_{i}.png");

            using var page = engine.Process(pix);
            string text = page.GetText().Trim();

            if (int.TryParse(text, out int value))
            {
                recommendations[categories[i]] = value;
            }
            else
            {
                recommendations[categories[i]] = 0;
            }
        }

        return recommendations;
    }

    private Pix BitmapToPix(Bitmap bitmap)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                stream.Position = 0;
                return Pix.LoadFromMemory(stream.ToArray());
            }
        }
        else
        {
            throw new PlatformNotSupportedException("Image saving is only supported on Windows.");
        }
    }

    private Bitmap PreprocessForOcr(Bitmap original)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("This method is supported only on Windows.");

        // Agrandir l'image x2
        int scale = 2;
        var resized = new Bitmap(original, new Size(original.Width * scale, original.Height * scale));

        // Convertir en niveaux de gris
        for (int y = 0; y < resized.Height; y++)
        {
            for (int x = 0; x < resized.Width; x++)
            {
                Color originalColor = resized.GetPixel(x, y);
                int grayScale = (int)((originalColor.R * 0.3) + (originalColor.G * 0.59) + (originalColor.B * 0.11));
                Color grayColor = Color.FromArgb(grayScale, grayScale, grayScale);
                resized.SetPixel(x, y, grayColor);
            }
        }

        return resized;
    }
    public Dictionary<string, int> AnalyzeBarFromCanvas(string imagePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("This method is supported only on Windows.");

        using var bitmap = new Bitmap(imagePath);

        // Exemple : coordonnées à adapter selon l'image
        Rectangle lastBarRect = new Rectangle(50, 0, 25, bitmap.Height);
        using var lastBar = bitmap.Clone(lastBarRect, bitmap.PixelFormat);
        lastBar.Save($"segment.png");

        return AnalyzeBarByColor(lastBar);
    }

    private Dictionary<string, int> AnalyzeBarByColor(Bitmap barSegment, int totalVotes = 100)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("This method is supported only on Windows.");

        var counts = new Dictionary<string, int>
        {
            { "Strong Buy", 0 },
            { "Buy", 0 },
            { "Hold", 0 },
            { "Sell", 0 },
            { "Strong Sell", 0 }
        };

        for (int y = 0; y < barSegment.Height; y++)
        {
            for (int x = 0; x < barSegment.Width; x++)
            {
                var pixel = barSegment.GetPixel(x, y);

                if (IsCloseToColor(pixel, Color.FromArgb(3, 123, 102))) // Adapter avec eyedropper
                    counts["Strong Buy"]++;
                else if (IsCloseToColor(pixel, Color.FromArgb(129, 169, 73)))
                    counts["Buy"]++;
                else if (IsCloseToColor(pixel, Color.FromArgb(255, 215, 71)))
                    counts["Hold"]++;
                else if (IsCloseToColor(pixel, Color.FromArgb(234, 112, 52)))
                    counts["Sell"]++;
                else if (IsCloseToColor(pixel, Color.FromArgb(214, 10, 34)))
                    counts["Strong Sell"]++;
            }
        }

        // Convert pixel count to integer values based on percentage
        int totalPixels = counts.Values.Sum();
        if (totalPixels == 0) return counts;

        return counts.ToDictionary(kvp => kvp.Key,
            kvp => (int)Math.Round((double)kvp.Value / totalPixels * totalVotes));
    }

    private bool IsCloseToColor(Color a, Color b, int tolerance = 30)
    {
        return Math.Abs(a.R - b.R) < tolerance &&
               Math.Abs(a.G - b.G) < tolerance &&
               Math.Abs(a.B - b.B) < tolerance;
    }


}
