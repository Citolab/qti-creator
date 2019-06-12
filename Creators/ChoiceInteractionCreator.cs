using System.Linq;
using System.Threading.Tasks;
using Citolab.QTI.package.Creator.Interfaces;
using Citolab.QTI.Package.Creator.Helpers;
using Citolab.QTI.Package.Creator.Interfaces;
using Citolab.QTI.Package.Creator.Model;
using RazorLight;


namespace Citolab.QTI.Package.Creator.Creators
{
    public class ChoiceInteractionCreator : ICreateItem<IMultipleChoiceItem>
    {
        private readonly QTIVersion _version;
        private readonly RazorLightEngine _engine;

        public ChoiceInteractionCreator(QTIVersion version, RazorLightEngine engine)
        {
            _version = version;
            _engine = engine;
        }

        public Task<string> CreateWithHtmlAsync(IMultipleChoiceItem item, IHtmlToQtiConverter converter)
        {
            item.Body = converter.ConvertXhtmlToQti(item.UniqueId, item.Body);
            item.Body = converter.ConvertStylesToCss(item.Body);
            item.Alternatives = item.Alternatives.Select(alternative =>
            {
                var newText = converter.ConvertXhtmlToQti(item.UniqueId, alternative.Text);
                newText = converter.ConvertStylesToCss(newText);
                alternative.Text = newText;
                return alternative;
            }).ToList();
            return GetRenderedItem(item);
        }

        public Task<string> CreatePlainTextAsync(IMultipleChoiceItem item)
        {
            item.Body = item.Body.WrapTextInParagraph();
            foreach (var itemAlternative in item.Alternatives)
            {
                itemAlternative.Text = itemAlternative.Text.WrapTextInParagraph();
            }
            return GetRenderedItem(item);
        }


        private Task<string> GetRenderedItem(IMultipleChoiceItem item)
        {
            return _engine.CompileRenderAsync($"Exports._{((int)_version).ToString()}.ChoiceInteraction.cshtml", item);
        }
    }
}