using System.Collections.Generic;
using System.Linq;
using Citolab.QTI.Package.Creator.Interfaces;

namespace Citolab.QTI.Package.Creator.Model
{
    /// <summary>
    ///     Multiple choice item
    /// </summary>
    public class MultipleChoiceItem : QtiItemBase, IMultipleChoiceItem
    {
        public MultipleChoiceItem(string uniqueId, string title, string body,  IList<Alternative> alternatives)
            : base(uniqueId, body, title)
        {
            Alternatives = alternatives.ToList();
            Keys = alternatives.Select((choiceItem, index) =>
                new
                {
                    Item = choiceItem,
                    Index = index
                })
                    .Where(c => c.Item.IsKey)
                    .Select(c => (char)(c.Index + 65))
                    .ToList();
        }
        public IList<char> Keys { get; }
        public IList<Alternative> Alternatives { get; set; }
    }
}