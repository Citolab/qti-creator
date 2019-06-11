using System;
using System.Collections.Generic;
using Citolab.QTI.Package.Creator.Model;

namespace Citolab.QTI.Package.Creator.Interfaces
{
    public interface IMultipleChoiceItem : IItem
    {
        IList<Alternative> Alternatives { get; set; }
    }
}
