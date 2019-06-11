using System.Collections.Generic;
using Citolab.QTI.Package.Creator.Interfaces;

namespace Citolab.QTI.Package.Creator.Model
{
    public class QtiTest
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public IList<IItem> Items { get; set; }
    }
}