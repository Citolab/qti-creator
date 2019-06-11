using System;
using System.Collections.Generic;
using System.Text;

namespace Citolab.QTI.Package.Creator.Interfaces
{
    public interface IItem
    {
        string UniqueId { get; set; }
        string Title { get; set; }
        string Body { get; set; }
    }
}
