using Citolab.QTI.Package.Creator.Helpers;
using Citolab.QTI.Package.Creator.Interfaces;

namespace Citolab.QTI.Package.Creator.Model
{
    public abstract class QtiItemBase : IItem
    {
        protected QtiItemBase(string uniqueId, string body, string title)
        {
            UniqueId = uniqueId.StartsWith("ITM") ?
                uniqueId :
                $"ITM-{uniqueId}";
            Title = !string.IsNullOrWhiteSpace(title) ?
                title : body.TruncateAndPlainText(64);
            Body = body;
        }


        public string UniqueId { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
    }
}