using System.IO;
using Spire.Doc;

public static class DocumentConverter
{
    public static string ConvertDocToPdf(string inputPath, string outputPath)
    {
        // Загружаем документ Word
        using (Document document = new Document())
        {
            document.LoadFromFile(inputPath);

            // Сохраняем файл в формате PDF
            document.SaveToFile(outputPath, FileFormat.PDF);
        }

        return outputPath;
    }
}