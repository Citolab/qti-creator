
using Citolab.QTI.Package.Creator.Interfaces;
using System.Threading.Tasks;
using Citolab.QTI.Package.Creator.Helpers;
using Microsoft.Extensions.DependencyInjection;
using RazorLight;

namespace Citolab.QTI.Package.Creator.Model
{
    /// <summary>
    ///     Creates a test base on a list of items
    /// </summary>
    public class TestCreator : ICreateTest
    {
        private readonly QTIVersion _version;
        private readonly RazorLightEngine _engine;

        public TestCreator(QTIVersion version, RazorLightEngine engine)
        {
            _version = version;
            _engine = engine;
        }

        public Task<string> CreateAsync(QtiTest qtiTest)
        {
            return _engine.CompileRenderAsync($"Exports._{((int)_version).ToString()}.Test.cshtml", qtiTest);
        }
    }
}