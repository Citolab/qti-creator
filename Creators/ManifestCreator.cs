
using System.Threading.Tasks;
using Citolab.QTI.Package.Creator.Helpers;
using Citolab.QTI.Package.Creator.Interfaces;
using Citolab.QTI.Package.Creator.Model;
using Microsoft.Extensions.DependencyInjection;
using RazorLight;

namespace Citolab.QTI.Package.Creator.Creators
{
    /// <summary>
    ///     Creates a manifest base on a list of items
    /// </summary>
    public class ManifestCreator : ICreateManifest
    {
        private readonly QTIVersion _version;
        private readonly RazorLightEngine _engine;

        public ManifestCreator(QTIVersion version, RazorLightEngine engine)
        {
            _version = version;
            _engine = engine;
        }

        public Task<string> CreateAsync(ManifestItems items)
        {
            return _engine.CompileRenderAsync($"Exports._{((int)_version).ToString()}.Manifest.cshtml", items);
        }
    }
}