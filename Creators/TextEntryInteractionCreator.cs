

using System.Threading.Tasks;
using Citolab.QTI.package.Creator.Interfaces;
using Citolab.QTI.Package.Creator.Helpers;
using Citolab.QTI.Package.Creator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using RazorLight;

namespace Citolab.QTI.Package.Creator.Model
{
    /// <summary>
    ///     creates item to an QTI Item
    /// </summary>
    public class TextEntryInteractionCreator : ICreateItem<ITextEntryItem>
    {
        private readonly QTIVersion _version;
        private readonly RazorLightEngine _engine;

        public TextEntryInteractionCreator(QTIVersion version, RazorLightEngine engine)
        {
            _version = version;
            _engine = engine;
        }

        public Task<string> CreateWithHtmlAsync(ITextEntryItem item, IHtmlToQtiConverter converter)
        {
            item.Body = converter.ConvertXhtmlToQti(item.UniqueId, item.Body);
            item.Body = converter.ConvertStylesToCss(item.Body);
            item.Key = item.Key;
            return GetRenderedItem(item);
        }

        public Task<string> CreatePlainTextAsync(ITextEntryItem item) =>
            GetRenderedItem(item);

        private Task<string> GetRenderedItem(ITextEntryItem item)
        {
            return _engine.CompileRenderAsync($"Exports._{((int)_version).ToString()}.TextEntryInteraction.cshtml", item);
        }
    }
}