using System.Threading.Tasks;
using Citolab.QTI.Package.Creator.Model;

namespace Citolab.QTI.Package.Creator.Interfaces
{
    public interface ICreateTest
    {
        Task<string> CreateAsync(QtiTest test);
    }
}