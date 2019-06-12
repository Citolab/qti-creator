using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Citolab.QTI.Package.Creator;
using Citolab.QTI.Package.Creator.Helpers;
using Citolab.QTI.Package.Creator.Interfaces;
using Citolab.QTI.Package.Creator.Model;
using Microsoft.Extensions.DependencyInjection;

namespace SampleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            
            // create plain text items
            var temp = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().FullName
            ), "temp"));
            if (!temp.Exists) temp.Create();
            var services = new ServiceCollection();
            services.AddQtiCreator();
            var serviceProvider = services.BuildServiceProvider();
            var qtiPackageCreator = serviceProvider.GetRequiredService<QtiPackageCreator>();
            var packageLocation = await qtiPackageCreator.CreatePackageWithPlainTextItems(
                QTIVersion.v2_2,
                "plaintext_assessment",
                new List<IItem>
                {
                        new TextEntryItem("001", "one plus one", "1 + 1", "2"),
                        new TextEntryItem("002", "five plus five","5 + 5", "10"),
                        new MultipleChoiceItem("003", "one plus ten", "1 + 10",
                            new List<Alternative>
                            {
                                new Alternative {IsKey = true,Text = "11"},
                                new Alternative {IsKey = false,Text = "20"},
                                new Alternative {IsKey = false,Text = "110"}
                            })

                }, temp.FullName);
            // create richText items
            var imageIndex = 0;
            var packageLocationRichText = await qtiPackageCreator.CreatePackageWithRichTextItems(
                QTIVersion.v2_2,
                "richtext_assessment",
                new List<IItem>
                {
                    new TextEntryItem("001", "richtext1", "<div>\r\n    <p>Taken from wikpedia</p>\r\n    <img src=\"data:image/png;base64, iVBORw0KGgoAAAANSUhEUgAAAAUA\r\nAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO\r\n    9TXL0Y4OHwAAAABJRU5ErkJggg==\" alt=\"Red dot\" />\r\n</div> ", "2"),
                     new MultipleChoiceItem("002", " richtext2", "1 + 10",
                        new List<Alternative>
                        {
                            new Alternative {IsKey = true,Text = "<div>\r\n    <p style=\"color:red;\">Taken from wikpedia</p>\r\n    <img src=\"data:image/png;base64, iVBORw0KGgoAAAANSUhEUgAAAAUA\r\nAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO\r\n    9TXL0Y4OHwAAAABJRU5ErkJggg==\" alt=\"Red dot\" />\r\n</div> "},
                            new Alternative {IsKey = false,Text = "<div>\r\n    <p style=\"color:blue;\">Taken from wikpedia</p>\r\n    <img src=\"data:image/png;base64, iVBORw0KGgoAAAANSUhEUgAAAAUA\r\nAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO\r\n    9TXL0Y4OHwAAAABJRU5ErkJggg==\" alt=\"Red dot\" />\r\n</div> "},
                            new Alternative {IsKey = false,Text = "<div>\r\n    <p style=\"color:green;\">Taken from wikpedia</p>\r\n    <img src=\"data:image/png;base64, iVBORw0KGgoAAAANSUhEUgAAAAUA\r\nAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO\r\n    9TXL0Y4OHwAAAABJRU5ErkJggg==\" alt=\"Red dot\" />\r\n</div> "}
                        })

                }, temp.FullName,
                (imageSource) =>
                {
                    var base64 = imageSource.Substring(imageSource.IndexOf("base64,",
                        StringComparison.Ordinal)).Trim();
                    var bytes = Convert.FromBase64String(base64);
                    imageIndex++;
                    return new RetrievedFile
                    {
                        Name = $"image_{imageIndex}.png",
                        Bytes = bytes
                    };
                });

            Console.WriteLine($"Create package with plain text item on location: {packageLocation}");
            Console.WriteLine($"Create package with rich text item on location: {packageLocationRichText}");


        }
    }
}
