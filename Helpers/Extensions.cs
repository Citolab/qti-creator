using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Citolab.QTI.Package.Creator.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Citolab.QTI.Package.Creator.Helpers
{
    public static class Extensions
    {
        private static readonly Regex StripHtmlRegex = new Regex("<.*?>", RegexOptions.Compiled);

        public static IServiceCollection AddQtiCreator(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            services.AddSingleton<QtiPackageCreator>();
            return services;
        }

        public static string TruncateAndPlainText(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            value = value.StripHtml();
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
        public static Dictionary<string, byte[]> GetImages(this HtmlDocument doc, string itemcode, Func<string, IRetrievedFile> resourceHandler)
        {
            return GetImages(doc, itemcode, resourceHandler,
                (imageNode, name, image) => imageNode.Attributes["src"].Value = $"../img/{name}");
        }

        public static T Clone<T>(this T toClone) where T : class
        {
            var tmp = JsonConvert.SerializeObject(toClone);
            return JsonConvert.DeserializeObject<T>(tmp);
        }

        public static Dictionary<string, byte[]> GetImages(this HtmlDocument doc, string itemcode, 
            Func<string, IRetrievedFile> resourceHandler,
            Action<HtmlNode, string, byte[]> fixMethod)
        {
            var images = new Dictionary<string, byte[]>();
            var nodeCollection = doc?.DocumentNode.SelectNodes("//*[name()=\"img\"]");
            if (nodeCollection == null) return null;
            foreach (var imageNode in nodeCollection)
            {
                if (imageNode.Attributes?["alt"] == null) imageNode.Attributes?.Append(doc.CreateAttribute("alt"));
                var containsBase64Image = imageNode.Attributes?["src"]?.Value.Contains("base64");
                var image = new byte[0];
                var name = string.Empty;
                if (containsBase64Image.HasValue && containsBase64Image.Value)
                {
                    var base64Splitted = imageNode.Attributes["src"].Value.Split(Convert.ToChar(","));
                    if (base64Splitted.Length <= 1) continue;
                    var filename = imageNode.Attributes?["alt"]?.Value;
                    name = !string.IsNullOrEmpty(filename)
                        ? $"IMG_{itemcode.ReplaceIllegalFilenameChars()}-{Path.GetFileNameWithoutExtension(filename).ReplaceIllegalFilenameChars()}.{GetExtension(base64Splitted[0])}"
                        : $"IMG_{itemcode.ReplaceIllegalFilenameChars()}-{images.Count + 1}.{GetExtension(base64Splitted[0])}";
                    image = Convert.FromBase64String(base64Splitted[1]);
                }
                else if (imageNode.Attributes?["src"] != null)
                {
                    var src = imageNode.Attributes?["src"]?.Value;
                    var img = resourceHandler.Invoke(src);
                    name = $"{itemcode.ReplaceIllegalFilenameChars()}-{img.Name}";
                    image = img.Bytes;
                }
                var alreadyAdded = false;
                if (images.Values.Any(i => i.Length == image.Length))
                {
                    var imageAlreadyAdded = images.First(i => i.Value != null && i.Value.SequenceEqual(image));
                    if (imageAlreadyAdded.Value != null)
                    {
                        name = imageAlreadyAdded.Key;

                        alreadyAdded = true;
                    }
                }
                fixMethod(imageNode, name, image);
                if (alreadyAdded) continue;
                images.Add(name, image);
            }
            return images;
        }

        private static string GetExtension(string src)
        {
            src = src.Replace(" ", "").ToLower();
            if (src.Contains("image/png")) return "png";
            if (src.Contains("image/jpg")) return "jpg";
            if (src.Contains("image/jpeg")) return "jpg";
            if (src.Contains("image/gif")) return "gif";
            throw new Exception("Image not supported");
        }

        public static string ReplaceIllegalFilenameChars(this string input) =>
            Path.GetInvalidFileNameChars().Aggregate(input, (current, c) => current.Replace(c, '_'));

        public static string StripHtml(this string input) =>
            string.IsNullOrEmpty(input) ? string.Empty : StripHtmlRegex.Replace(input, string.Empty).Trim();

    }
}
