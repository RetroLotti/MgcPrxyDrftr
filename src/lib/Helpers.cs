using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProxyDraftor.lib
{
    public static class Helpers
    {
        public static bool Write(string text, int posX, int posY)
        {
            (int left, int top) = Console.GetCursorPosition();
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

        public static models.Deck ReadSingleDeck(string file)
        {
            return JsonConvert.DeserializeObject<models.Deck>(File.ReadAllText(file));
        }

        public static void CheckNanDeck(string fullPath)
        {
            FileInfo nandeck = new(fullPath);
            if (!nandeck.Exists)
            {
                Console.WriteLine("NanDECK wurde nicht gefunden. Bitte geben die den Ort manuell an!");
                Console.Write("> ");
                fullPath = Console.ReadLine();
                ConfigurationManager.AppSettings["NanDeckFullPath"] = fullPath;
            }
        }

        //static bool Test()
        //{
        //    //DirectoryInfo setDirectory = new(@$"{BaseDirectory}\{JsonDirectory}\{JsonSetDirectory}\");
        //    //if (setDirectory.Exists)
        //    //{
        //    //    var files = setDirectory.GetFiles("*.json");
        //    //    foreach (var file in files)
        //    //    {
        //    //        var txt = File.ReadAllText(file.FullName);
        //    //        dynamic set = JObject.Parse(txt);

        //    //        foreach (var item in set.data.booster)
        //    //        {
        //    //            foreach (var boosters in item.Value)
        //    //            {
        //    //                foreach (var content in boosters.Value)
        //    //                {
        //    //                    //((JProperty)content.contents.First).Value
        //    //                    foreach (var sheet in ((JObject)content.contents))
        //    //                    {
        //    //                        var s = Type.GetType("ProxyDraftor.models.Sheets").GetProperties().Where(x => x.Name.ToLower().Equals(sheet.Key.ToLower())).FirstOrDefault();
        //    //                        if (s == null)
        //    //                        {
        //    //                            Console.WriteLine($"Sheet {sheet} nicht gefunden!");
        //    //                        }
        //    //                    }
        //    //                }
        //    //            }
        //    //        }

        //    //    }
        //    //}
        //    return true;
        //}
    }
}
