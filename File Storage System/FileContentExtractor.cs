// FileContentExtractor.cs
using System;
using System.IO;
using System.Text;
using PdfiumViewer;

namespace FileFlow
{
    public static class FileContentExtractor
    {
        // Set a reasonable file size limit (e.g., 100 MB) to avoid memory issues.
        private const long MaxFileSizeInBytes = 100 * 1024 * 1024;

        public static string GetTextContent(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists || fileInfo.Length == 0 || fileInfo.Length > MaxFileSizeInBytes)
                {
                    return string.Empty;
                }

                var extension = fileInfo.Extension.ToLowerInvariant();
                switch (extension)
                {
                    case ".txt":
                    case ".csv":
                    case ".log":
                    case ".prg":
                    case ".p-2":
                    case ".files":
                    case "": // Added support for files with no extension
                        return ReadTextFile(filePath);

                    case ".pdf":
                        return GetPdfText(filePath);

                    default:
                        return string.Empty;
                }
            }
            // Catch specific, common exceptions to make the process more robust.
            catch (IOException) { return string.Empty; } // File is likely in use or unreadable
            catch (UnauthorizedAccessException) { return string.Empty; } // No permission to read
            catch (Exception) { return string.Empty; } // Catch-all for any other unexpected errors
        }

        private static string ReadTextFile(string filePath)
        {
            var sb = new StringBuilder();
            // Read the file line-by-line to be more memory-efficient.
            using (var reader = new StreamReader(filePath, Encoding.UTF8, true))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }

        private static string GetPdfText(string filePath)
        {
            var sb = new StringBuilder();
            try
            {
                using (var document = PdfDocument.Load(filePath))
                {
                    for (int i = 0; i < document.PageCount; i++)
                    {
                        sb.Append(document.GetPdfText(i));
                        sb.Append(" "); // Add space between pages
                    }
                }
            }
            catch (Exception)
            {
                // Ignore errors from corrupt or protected PDFs
                return string.Empty;
            }
            return sb.ToString();
        }
    }
}