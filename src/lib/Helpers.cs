using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MgcPrxyDrftr.models;
using Newtonsoft.Json;
using Spire.Pdf;
using Spire.Pdf.Graphics;

namespace MgcPrxyDrftr.lib
{
    public static class Helpers
    {
        public static bool Write(string text, int posX, int posY)
        {
            (var left, var top) = Console.GetCursorPosition();
            Console.SetCursorPosition(posX, posY);
            Console.Write(text);
            Console.SetCursorPosition(left, top);

            return true;
        }

        public static void CheckDirectory(string path)
        {
            DirectoryInfo dir = new(path);
            if (!dir.Exists) { dir.Create(); }
        }

        public static DeckRoot ReadSingleDeck(string file)
        {
            return JsonConvert.DeserializeObject<DeckRoot>(File.ReadAllText(file));
        }

        public static void CheckNanDeck(string fullPath)
        {
            FileInfo nandeck = new(fullPath);
            if (!nandeck.Exists)
            {
                Console.WriteLine("NanDECK not found. Please enter path to nandeck.exe manually!");
                Console.Write("> ");
                fullPath = Console.ReadLine();
                ConfigurationManager.AppSettings["NanDeckFullPath"] = fullPath;
            }
        }

        /// <summary>
        /// Generic method to download the given file and validate sha256 hash
        /// </summary>
        /// <param name="downloadFileUri"></param>
        /// <param name="validationFileUri"></param>
        /// <param name="targetDirectory"></param>
        /// <returns></returns>
        public static async Task<bool> DownloadAndValidateFile(string downloadFileUri, string validationFileUri, string targetDirectory)
        {
            HttpClient httpClient = new();
            Uri downloadUri = new(downloadFileUri);
            Uri checksumUri = new(validationFileUri);

            await DownloadFile(httpClient, downloadFileUri, @$"{targetDirectory}\{downloadUri.Segments[^1]}");
            await DownloadFile(httpClient, validationFileUri, @$"{targetDirectory}\{checksumUri.Segments[^1]}");

            return ValidateFiles($@"{targetDirectory}\{downloadUri.Segments[^1]}", @$"{targetDirectory}\{checksumUri.Segments[^1]}");
        }

        private static async Task<bool> DownloadFile(HttpClient httpClient, string uri, string targetFile)
        {
            await using var stream = await httpClient.GetStreamAsync(uri);
            await using var fileStream = new FileStream(targetFile, FileMode.CreateNew);
            await stream.CopyToAsync(fileStream);
            return true;
        }

        public static async Task DownloadSetFile(string setCode, string fullJsonPath, string setFolder)
        {
            HttpClient httpClient = new();
            var currentFileText = string.Empty;

            // download content file
            await DownloadFile(httpClient, $"https://mtgjson.com/api/v5/{setCode}.json", @$"{fullJsonPath}\{setCode.ToUpper()}.json").ConfigureAwait(false);

            // download checksum file
            await DownloadFile(httpClient, $"https://mtgjson.com/api/v5/{setCode}.json.sha256", @$"{fullJsonPath}\{setCode.ToUpper()}.json.sha256").ConfigureAwait(false);

            // validate checksum
            var isValid = ValidateFiles(@$"{fullJsonPath}\{setCode.ToUpper()}.json", @$"{fullJsonPath}\{setCode.ToUpper()}.json.sha256");

            if (!isValid) return;
            var downloadedFileText = await File.ReadAllTextAsync(@$"{fullJsonPath}\{setCode.ToUpper()}.json");
            if (File.Exists(@$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json"))
            {
                currentFileText = await File.ReadAllTextAsync(@$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json");
            }

            // TODO: check for new eums
                
            var downloadSet = JsonConvert.DeserializeObject<SetRoot>(downloadedFileText);
            var currentSet = JsonConvert.DeserializeObject<SetRoot>(currentFileText);

            if (currentSet == null)
            {
                // move
                File.Move(@$"{fullJsonPath}\{setCode.ToUpper()}.json", @$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json");
            }
            else if (downloadSet.Meta.Date > currentSet.Meta.Date && !downloadSet.Meta.Version.Equals(currentSet.Meta.Version))
            {
                // delete
                File.Delete(@$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json");

                // then move
                File.Move(@$"{fullJsonPath}\{setCode.ToUpper()}.json", @$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json");
            }

            File.Delete(@$"{fullJsonPath}\{setCode.ToUpper()}.json");
            File.Delete(@$"{fullJsonPath}\{setFolder}\{setCode.ToUpper()}.json.bak");
            File.Delete(@$"{fullJsonPath}\{setCode.ToUpper()}.json.sha256");
        }

        private static bool ValidateFiles(string contentFile, string checksumFile)
        {
            var downloadedFileChecksum = CalculateChecksum(contentFile);
            var serverChecksum = ReadChecksum(checksumFile);

            return (downloadedFileChecksum.Equals(serverChecksum));
        }

        private static string ReadChecksum(string file) { return File.ReadAllText(file); }

        private static string CalculateChecksum(string file)
        {
            var waitCounter = 0;
            var finished = false;
            var checksum = string.Empty;
            FileInfo fileInfo = new(file);
            
            using var sha256 = SHA256.Create();

            do
            {
                try
                {
                    using var fileStream = fileInfo.Open(FileMode.Open);
                    var hashValue = sha256.ComputeHash(fileStream);

                    checksum = hashValue.Aggregate(checksum, (current, t) => current + $"{t:X2}");

                    finished = true;
                }
                catch (Exception)
                {
                    //Console.WriteLine(ex.Message);
                }

                if (++waitCounter == 10) finished = true;

            } while (finished == false);

            return checksum.ToLower();
        }

        public static bool CreatePdfDocument(Guid boosterGuid, string imageFolder)
        {

            // TODO: check folder

            var pdfDocument = new PdfDocument();
            var cardCounter = 0;

            float x = 0;
            float y = 0;

            var unitConvertor = new PdfUnitConvertor();
            var cardWidth = unitConvertor.ConvertUnits(6.2f, PdfGraphicsUnit.Centimeter, PdfGraphicsUnit.Point);
            var cardHeight = unitConvertor.ConvertUnits(8.7f, PdfGraphicsUnit.Centimeter, PdfGraphicsUnit.Point);

            var marginLeftRight = (PdfPageSize.A4.Width - cardWidth * 3) / 2;
            var marginTopBottom = (PdfPageSize.A4.Height - cardHeight * 3) / 2;

            // add first page
            var page = pdfDocument.Pages.Add(PdfPageSize.A4, new PdfMargins(marginLeftRight, marginTopBottom));

            // get card count
            var maxCards = Directory.GetFiles(@$"{imageFolder}\{boosterGuid}\", "*.png").Length;

            foreach (var file in Directory.GetFiles(@$"{imageFolder}\{boosterGuid}\", "*.png"))
            {
                cardCounter++;

                var image = Image.FromFile(file);
                var pdfImage = PdfImage.FromImage(image);
                
                // put image on page
                page.Canvas.DrawImage(pdfImage, x, y, cardWidth, cardHeight);

                if (cardCounter % 3 > 0) { x += cardWidth; } else { x = 0; }

                if (cardCounter is 3 or 6 or 12 or 15) { y += cardHeight; }

                // add new page if current page has nine cards
                if (cardCounter % 9 != 0) continue;
                page = pdfDocument.Pages.Add(PdfPageSize.A4, new PdfMargins(marginLeftRight, marginTopBottom));
                x = 0; y = 0;
            }

            pdfDocument.SaveToFile(@$"{imageFolder}\{boosterGuid}\{boosterGuid}.pdf");

            return true;
        }
    }
}
