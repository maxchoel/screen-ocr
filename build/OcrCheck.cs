using System;
using System.Drawing;
using ScreenOcrTool;

internal static class OcrCheck
{
    private static void Main(string[] args)
    {
        OcrService service = OcrService.Create();
        Console.WriteLine("engine=" + service.DisplayLanguage);
        foreach (string path in args)
        {
            using (Bitmap bitmap = new Bitmap(path))
            {
                string text = service.RecognizeAsync(bitmap).GetAwaiter().GetResult();
                Console.WriteLine("######## " + System.IO.Path.GetFileName(path));
                Console.WriteLine(text);
            }
        }
    }
}