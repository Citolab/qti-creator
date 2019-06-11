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
            // eeturn provider.GetRequiredService<RazorViewToStringRenderer>();
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

            Console.WriteLine(packageLocation);
            // create richText items
        }
    }
}
