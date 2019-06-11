using System;
using System.Collections.Generic;
using Citolab.QTI.Package.Creator.Interfaces;

namespace Citolab.QTI.Package.Creator.Model
{
    public class ManifestItems
    {
        public Dictionary<string, HashSet<string>> Dependencies;
        public string TestId { get; set; }
        public IList<IItem> Items { get; set; }
        public IList<string> Media { get; set; }
        public IList<string> Css { get; set; }
    }
}