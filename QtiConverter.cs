using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Citolab.QTI.package.Creator.Interfaces;
using Citolab.QTI.Package.Creator.Creators;
using Citolab.QTI.Package.Creator.Helpers;
using Citolab.QTI.Package.Creator.Interfaces;
using Citolab.QTI.Package.Creator.Model;
using Microsoft.Extensions.DependencyInjection;
using RazorLight;
using RazorLight.Razor;

namespace Citolab.QTI.Package.Creator
{
    public class QtiPackageCreator
    {
        private ICreateItem<IMultipleChoiceItem> _choiceInterationCreator;
        private ICreateItem<ITextEntryItem> _textEntryInteractionCreator;
        private ICreateManifest _manifestCreator;
        private ICreateTest _testCreator;
        private string _testname;
    

        public void Init(string tempPath, string testname, QTIVersion version)
        {
            var engine = new RazorLightEngineBuilder()
                //.UseProject(new EmbeddedRazorProject(typeof(QtiPackageCreator)))
                .UseEmbeddedResourcesProject(typeof(QtiPackageCreator))
                .UseMemoryCachingProvider()
                .Build();
            _choiceInterationCreator = new ChoiceInteractionCreator(version, engine);
            _textEntryInteractionCreator = new TextEntryInteractionCreator(version, engine);
            _testCreator = new TestCreator(version, engine);
            _manifestCreator = new ManifestCreator(version, engine);
            _testname = testname;
            var dir = new DirectoryInfo(tempPath);
            if (!dir.Exists)
            {
                dir.Create();
            }
            var packagePath = Path.Combine(tempPath, Path.GetRandomFileName());
            var itemPath = Path.Combine(packagePath, "items");
            var baseDir = new DirectoryInfo(packagePath);
            var itemDir = new DirectoryInfo(itemPath);
            if (!baseDir.Exists) baseDir.Create();
            if (!itemDir.Exists) itemDir.Create();
        }

        /// <summary>
        ///     Create items and create package
        /// </summary>
        /// <param name="testname"></param>
        /// <param name="items"></param>
        /// <param name="tempPath"></param>
        /// <param name="resourceHandler"></param>
        /// <param name="version"></param>
        /// <returns>File location of zip file</returns
        public Task<string> CreatePackageWithRichTextItems(QTIVersion version, string testname, IEnumerable<IItem> items, string tempPath,
            Func<string, IRetrievedFile> resourceHandler)
        {
            Init(tempPath, testname, version);
            return ConvertItems(items, tempPath, true, resourceHandler);
        }

        /// <summary>
        ///     Convert items and create package
        /// </summary>
        /// <param name="testname"></param>
        /// <param name="items"></param>
        /// <param name="tempPath"></param>
        /// <param name="version"></param>
        /// <returns>File location of zip file</returns
        public Task<string> CreatePackageWithPlainTextItems(QTIVersion version, string testname,
            IEnumerable<IItem> items, string tempPath)
        {
            Init(tempPath, testname, version);
            return ConvertItems(items, tempPath, false, null);
        }

        private async Task<string> ConvertItems(IEnumerable<IItem> items, string tempPath,
              bool convertHtml, Func<string, IRetrievedFile> resourceHandler)
        {
            var baseTempPath = tempPath;
            tempPath = Path.Combine(baseTempPath, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            var dir = new DirectoryInfo(tempPath);
            if (!dir.Exists) dir.Create();
            var itemsList = items.ToList();
            var htmlToQtiConverter = new HtmlToQtiConverter(tempPath, resourceHandler);
            var itemsDir = new DirectoryInfo(Path.Combine(tempPath, "items"));
            if (!itemsDir.Exists) itemsDir.Create();
            var itemQti = string.Empty;
            foreach (var item in itemsList)
            {
                item.UniqueId = item.UniqueId.ReplaceIllegalFilenameChars();
                switch (item)
                {
                    case IMultipleChoiceItem choiceItem:
                        itemQti = convertHtml ?
                            await _choiceInterationCreator.CreateWithHtmlAsync(choiceItem, htmlToQtiConverter) :
                            await _choiceInterationCreator.CreatePlainTextAsync(choiceItem);
                        break;
                    case ITextEntryItem textEntryItem:
                         itemQti = convertHtml ?
                            await _textEntryInteractionCreator.CreateWithHtmlAsync(textEntryItem, htmlToQtiConverter) :
                            await _textEntryInteractionCreator.CreatePlainTextAsync(textEntryItem); ;
                        break;
                }
                var fileName = Path.Combine(itemsDir.ToString(), $"ITM-{item.UniqueId}.xml");
                File.WriteAllText(fileName, itemQti);
            }
            var test = new QtiTest { Id = $"TST-{Guid.NewGuid()}", Items = itemsList, Title = _testname };
            var testQti = await _testCreator.CreateAsync(test);
            var fileNameTest = Path.Combine(itemsDir.ToString(), $"{test.Id}.xml");
            File.WriteAllText(fileNameTest, testQti);
            var css = htmlToQtiConverter.Css;
            var images = htmlToQtiConverter.Images;

            var imageList = images?.Select(i => $"img/{i.Key}").ToList() ?? new List<string>();
            var cssList = !string.IsNullOrEmpty(css)
                ? new List<string> { "css/generated_styles.css" }
                : new List<string>();
            var manifestItems = new ManifestItems
            {
                TestId = test.Id,
                Items = itemsList,
                Dependencies = htmlToQtiConverter.Dependencies,
                Media = imageList,
                Css = cssList
            };
            var manifestQti = await _manifestCreator.CreateAsync(manifestItems);
            var fileNameManifest = Path.Combine(tempPath, "imsmanifest.xml");
            File.WriteAllText(fileNameManifest, manifestQti);
            var cssDir = new DirectoryInfo(Path.Combine(tempPath, "css"));
            if (!cssDir.Exists) cssDir.Create();
            var fileNameCss = Path.Combine(cssDir.ToString(), "generated_styles.css");
            if (!string.IsNullOrEmpty(css)) File.WriteAllText(fileNameCss, css);
            var fileNameZip = Path.Combine(baseTempPath, $"qti-package-{DateTime.Now:yyyy-MM-dd_hh-mm-ss}.zip");
            ZipFile.CreateFromDirectory(tempPath, fileNameZip,
                        CompressionLevel.Optimal,
                        false);
            return fileNameZip;
        }
    }
}