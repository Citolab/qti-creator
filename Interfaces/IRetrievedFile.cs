using System;
using System.Collections.Generic;
using System.Text;

namespace Citolab.QTI.Package.Creator.Interfaces
{
    public interface IRetrievedFile
    {
        string Name { get; set; }
        byte[] Bytes { get; set; }
    }
}
