using System.Threading.Tasks;
using Citolab.QTI.Package.Creator.Interfaces;

namespace Citolab.QTI.package.Creator.Interfaces
{
    internal interface ICreateItem<in T> where T : IItem
    {
        Task<string> CreateWithHtmlAsync(T item, IHtmlToQtiConverter converter);
        Task<string> CreatePlainTextAsync(T item);
    }
}