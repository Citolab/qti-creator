using System;
using System.Collections.Generic;
using System.Text;

namespace Citolab.QTI.Package.Creator.Interfaces
{
    public interface ITextEntryItem : IItem
    {
        string Key { get; set; }
    }
}
