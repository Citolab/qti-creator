using System.Threading.Tasks;
using Citolab.QTI.Package.Creator.Model;

namespace Citolab.QTI.Package.Creator.Interfaces
{
    /// <summary>
    ///     Convert manifest
    /// </summary>
    public interface ICreateManifest
    {
        Task<string> CreateAsync(ManifestItems manifestItems);
    }
}