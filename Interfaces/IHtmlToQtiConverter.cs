using System;
using System.Collections.Generic;
using System.Text;

namespace Citolab.QTI.Package.Creator.Interfaces
{
    public interface IHtmlToQtiConverter
    {
        /// <summary>
        ///     Images added: name + image as byte[]
        /// </summary>
        Dictionary<string, string> Images { get; }

        /// <summary>
        ///     The content of the css file. Needed because qti doesnt support inline styles.
        /// </summary>
        string Css { get; }

        /// <summary>
        ///     Depencies of items to images or css.
        /// </summary>
        Dictionary<string, HashSet<string>> Dependencies { get; }

        string ConvertXhtmlToQti(string itemId, string html);

        /// <summary>
        ///     Convert Styles to seperated css file
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        string ConvertStylesToCss(string html);
    }
}
