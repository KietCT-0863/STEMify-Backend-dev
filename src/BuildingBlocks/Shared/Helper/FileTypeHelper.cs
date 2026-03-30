using System.IO.Compression;
using System.Text;

namespace Shared.Helper
{
    public static class FileTypeHelper
    {
        public static bool IsImage(byte[]? bytes)
        {
            if (bytes == null || bytes.Length < 4) return false;

            // JPEG/JPG
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return true;

            // PNG
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return true;

            // GIF
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                return true;

            // BMP
            if (bytes[0] == 0x42 && bytes[1] == 0x4D)
                return true;

            // TIFF
            if ((bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00) ||
                (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A))
                return true;

            // WEBP (RIFF....WEBP)
            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                return true;

            // SVG (text-based) or SVGZ (gzipped SVG)
            if (IsSvg(bytes) || IsGzippedSvg(bytes))
                return true;

            return false;
        }

        private static bool IsSvg(byte[] bytes)
        {
            // Read a small sample and look for '<svg' or '<?xml' followed by '<svg'
            int length = Math.Min(bytes.Length, 2048);
            string sample;
            try
            {
                sample = Encoding.UTF8.GetString(bytes, 0, length).TrimStart().ToLowerInvariant();
            }
            catch
            {
                return false;
            }

            if (sample.StartsWith("<svg"))
                return true;

            if (sample.StartsWith("<?xml") && sample.Contains("<svg"))
                return true;

            // Some files may have an XML prolog then whitespace/newlines before svg tag
            if (sample.Contains("<svg"))
                return true;

            return false;
        }

        private static bool IsGzippedSvg(byte[] bytes)
        {
            // GZIP header 1F 8B
            if (bytes.Length < 2 || bytes[0] != 0x1F || bytes[1] != 0x8B)
                return false;

            try
            {
                using var ms = new MemoryStream(bytes);
                using var gz = new GZipStream(ms, CompressionMode.Decompress);
                using var outMs = new MemoryStream();
                // Read only a limited amount to avoid large allocations
                gz.CopyTo(outMs);
                var decompressed = outMs.ToArray();
                return IsSvg(decompressed);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsVideo(byte[]? bytes)
        {
            if (bytes == null || bytes.Length < 12) return false;

            // MP4: "ftyp" ở offset 4
            if (bytes[4] == 0x66 && bytes[5] == 0x74 && bytes[6] == 0x79 && bytes[7] == 0x70)
                return true;

            // WebM / MKV: EBML header 1A 45 DF A3
            if (bytes[0] == 0x1A && bytes[1] == 0x45 && bytes[2] == 0xDF && bytes[3] == 0xA3)
                return true;

            // AVI: "RIFF"...."AVI "
            if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x41 && bytes[9] == 0x56 && bytes[10] == 0x49 && bytes[11] == 0x20)
                return true;

            // MOV: giống MP4 nhưng brand khác ("moov" atom, offset khác)
            if (bytes.Length > 8 &&
                bytes[4] == 0x6D && bytes[5] == 0x6F && bytes[6] == 0x6F && bytes[7] == 0x76)
                return true;

            return false;
        }

        public static bool IsDocument(byte[]? bytes)
        {
            if (bytes == null || bytes.Length < 4) return false;

            // PDF: %PDF
            if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                return true;

            // DOC (OLE Compound file): D0 CF 11 E0
            if (bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0)
                return true;

            // DOCX / XLSX / PPTX (ZIP): PK\x03\x04
            if (bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04)
                return true;

            return false;
        }

        public static bool IsCsv(byte[]? bytes)
        {
            if (bytes == null || bytes.Length < 2)
                return false;

            // CSV is plain text → must be readable
            // Check if first 2 KB are mostly ASCII
            int length = Math.Min(bytes.Length, 2048);
            int asciiCount = 0;

            for (int i = 0; i < length; i++)
            {
                byte b = bytes[i];

                // Allow ASCII printable chars & whitespace
                if (b == 9 || b == 10 || b == 13 || (b >= 32 && b <= 126))
                    asciiCount++;
            }

            // if >95% ASCII → it's plain text (csv, txt, json, etc.)
            double ratio = asciiCount / (double)length;
            if (ratio < 0.95)
                return false;

            // Check for CSV separators
            string sample = Encoding.UTF8.GetString(bytes.Take(length).ToArray());
            if (sample.Contains(",") || sample.Contains(";"))
                return true;

            return false;
        }

        public static string GetDocumentExtension(byte[]? bytes)
        {
            if (bytes == null || bytes.Length < 4)
                return ".bin";

            // PDF
            if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
                return ".pdf";

            // DOC (OLE)
            if (bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0)
                return ".doc"; // could also be .xls or .ppt, but .doc is most common

            // DOCX / XLSX / PPTX (ZIP) - need to check content
            if (bytes[0] == 0x50 && bytes[1] == 0x4B && bytes[2] == 0x03 && bytes[3] == 0x04)
            {
                // Try to detect Office file type by checking ZIP content
                try
                {
                    using var ms = new MemoryStream(bytes);
                    using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
                    
                    // Check for specific Office file markers
                    var entries = archive.Entries.Select(e => e.FullName.ToLowerInvariant()).ToList();
                    
                    // PPTX contains ppt/ folder
                    if (entries.Any(e => e.Contains("ppt/") || e.Contains("ppt\\") || e.Contains("presentation.xml")))
                        return ".pptx";
                    
                    // XLSX contains xl/ folder
                    if (entries.Any(e => e.Contains("xl/") || e.Contains("xl\\") || e.Contains("workbook.xml")))
                        return ".xlsx";
                    
                    // DOCX contains word/ folder
                    if (entries.Any(e => e.Contains("word/") || e.Contains("word\\") || e.Contains("document.xml")))
                        return ".docx";
                }
                catch
                {
                    // If ZIP parsing fails, default to .docx
                }
                
                return ".docx"; // default for Office files
            }

            return ".bin";
        }
    }
}
