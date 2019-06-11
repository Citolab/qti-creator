

using Citolab.QTI.Package.Creator.Helpers;
using Citolab.QTI.Package.Creator.Interfaces;

namespace Citolab.QTI.Package.Creator.Model
{
    public class TextEntryItem : QtiItemBase, ITextEntryItem
    {
        public TextEntryItem(string uniqueId, string title, string body, string correctAnswer) 
            : base(uniqueId, body, title)
        {
            Key = correctAnswer;
        }

        public string Key
        {
            get;
            set;
        }
    }
}