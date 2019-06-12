using System;
using System.Collections.Generic;
using System.Text;
using Citolab.QTI.Package.Creator.Interfaces;

namespace Citolab.QTI.Package.Creator.Model
{
    public class RetrievedFile: IRetrievedFile
    {
        public string Name { get; set; }
        public byte[] Bytes { get; set; }
    }
}
