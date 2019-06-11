using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Citolab.QTI.Package.Creator.Helpers;
using Citolab.QTI.Package.Creator.Interfaces;
using HtmlAgilityPack;

namespace Citolab.QTI.Package.Creator
{
  public class HtmlToQtiConverter : IHtmlToQtiConverter
    {
        private const string XpathStrip = "node(){0}";
        private const string ExcludeTags = "[name()!=\"{0}\"]";
        private const string GetAllowedTag = "[name()=\"{0}\"]";
        private const string XpathStyles = "//*[@style]";
        private const string XpathIds = "//*[@id]";
        private const string GeneratedClassname = "cito_genclass";

        private readonly Dictionary<string, string> _convertionAttributeDictionary = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _convertionElementsDictionary = new Dictionary<string, string>();
        private readonly Dictionary<string, HashSet<string>> _dependencies = new Dictionary<string, HashSet<string>>();

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _stylesAdded = new Dictionary<string, string>();

        private string _itemId;
        private readonly string _tempPath;
        private readonly Func<string, IRetrievedFile> _resourceHandler;

        /// <param name="tempPath">Temp path to store the package</param>
        /// <param name="resourceHandler">Function that retrieves bytes and name of the resource based on src element in html</param>
        public HtmlToQtiConverter(string tempPath, Func<string, IRetrievedFile> resourceHandler)
        {
            //Initialise the dictionary that is used to convert 
            InitialiseAttributesToStyle();
            InitialiseElementsToStyle();
            _tempPath = tempPath;
            _resourceHandler = resourceHandler;
        }
        
        /// <summary>
        ///     Images added: name + image as byte[]
        /// </summary>
        public Dictionary<string, string> Images => _images;

        /// <summary>
        ///     The content of the css file. Needed because qti doesnt support inline styles.
        /// </summary>
        public string Css
        {
            get
            {
                var sb = new StringBuilder();
                return sb.ToString();
            }
        }

        /// <summary>
        ///     Depencies of items to images or css.
        /// </summary>
        public Dictionary<string, HashSet<string>> Dependencies => _dependencies;


        public string ConvertXhtmlToQti(string itemId, string html)
        {
            _itemId = itemId;
            //Remove namespaces, and word crap
            html = ExecuteRegex(html);
            html = FixMissingParagraph(html);
            html = $"<dummytag>{html}</dummytag>";
            var doc = new HtmlDocument { OptionOutputAsXml = true };
            doc.LoadHtml(html);
            //fix ids
            FixIds(ref doc);
            //Modify image
            FixImageTag(ref doc, itemId);
            //Modify table
            FixTableTag(ref doc);
            ConvertHtmlStyleToInlineCss(doc, doc.DocumentNode);
            //Remove unsupported tags
            StripAllUnknownTags(doc.DocumentNode.FirstChild, false);
            return doc.DocumentNode.FirstChild.InnerHtml;
        }


        /// <summary>
        ///     Convert Styles to seperated css file
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public string ConvertStylesToCss(string html)
        {
            html = $"<dummytag>{html}</dummytag>";
            var doc = new HtmlDocument { OptionOutputAsXml = true };
            doc.LoadHtml(html);
            ExtractAllStyles(ref doc);
            return doc.DocumentNode.FirstChild.InnerHtml;
        }

        #region Private Methods

        private void InitialiseAttributesToStyle()
        {
            _convertionAttributeDictionary.Add("width", "width: {0}px;");
            _convertionAttributeDictionary.Add("heigth", "heigth: {0}px;");
            _convertionAttributeDictionary.Add("border", "border: {0}px solid black;");
            _convertionAttributeDictionary.Add("bgcolor", "background-color: {0};");
            _convertionAttributeDictionary.Add("align", "text-align: {0};");
            _convertionAttributeDictionary.Add("valign", "vertical-align: {0};");
            _convertionAttributeDictionary.Add("cellpadding", "padding: {0};");
            _convertionAttributeDictionary.Add("dir", "direction: {0};");
            _convertionAttributeDictionary.Add("cellspacing", "border-spacing: {0}px;");
        }

        private void InitialiseElementsToStyle()
        {
            _convertionElementsDictionary.Add("i", "font-style:italic;");
            _convertionElementsDictionary.Add("u", "text-decoration:underline;");
            _convertionElementsDictionary.Add("b", "font-weight: bold;");
        }

        private static string ExecuteRegex(string xHtml)
        {
            // Remove the namespaces
            xHtml = RemoveNamespaces(xHtml, null, true);
            Regex.Replace(xHtml, "<([^>]*)(?:lang|[ovwxp]:\\w+)=(?:'[^']*'|\"[^\"]*\"|[^\\s>]+)([^>]*)>", "<$1$2>",
                RegexOptions.IgnoreCase);
            Regex.Replace(xHtml, "<([^>]*)(?:lang|[ovwxp]:\\w+)=(?:'[^']*'|\"[^\"]*\"|[^\\s>]+)([^>]*)>", "<$1$2>",
                RegexOptions.IgnoreCase);
            return xHtml;
        }

        private static string RemoveNamespaces(string element, List<string> namespacePrefixes,
            bool removeDefaultNamespaces)
        {
            if (!string.IsNullOrEmpty(element))
            {
                var result = element;
                if (namespacePrefixes != null)
                {
                    result = namespacePrefixes.Aggregate(result, (current, namespacePrefix) =>
                        Regex.Replace(current, $"(xmlns:{namespacePrefix}=[\"][^\"]*[\"])", "",
                            RegexOptions.IgnoreCase | RegexOptions.Multiline));
                }
                return removeDefaultNamespaces
                    ? Regex.Replace(result, "(xmlns=[\"][^\"]*[\"])", "", RegexOptions.IgnoreCase | RegexOptions.Multiline)
                    : result;
            }
            return string.Empty;
        }
        private void ConvertHtmlStyleToInlineCss(HtmlDocument doc, HtmlNode node)
        {
            foreach (var childNode in node.ChildNodes)
            {
                if (ShouldConvertToInlineCss(childNode.Name)) ConvertTagAndAttributesToStyle(doc, childNode);
                if (childNode.ChildNodes != null) ConvertHtmlStyleToInlineCss(doc, childNode);
            }
        }

        /// <summary>
        ///     Con
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="htmlNode"></param>
        private void ConvertTagAndAttributesToStyle(HtmlDocument doc, HtmlNode htmlNode)
        {
            //check the attributes, like: valign, border etc
            var listOfAttibutesToDelete = new List<string>();
            var listOfElementsToDelete = new List<HtmlNode>();
            var attributesToAdd = new List<string>();

            if (htmlNode.Attributes != null)
            {
                foreach (var attr in htmlNode.Attributes)
                {
                    if (_convertionAttributeDictionary.ContainsKey(attr.Name))
                    {
                        var value = string.Format(_convertionAttributeDictionary[attr.Name], attr.Value);
                        //if e.g. width is in percentage it shouldn't be in px.
                        if (attr.Value.Contains('%')) value = value.Replace("px", string.Empty);
                        attributesToAdd.Add(value);
                        listOfAttibutesToDelete.Add(attr.Name);
                    }

                    if (htmlNode.Name == "ol" && attr.Name == "type" && attr.Value == "I")
                        attributesToAdd.Add("list-style-type: upper-roman;");
                }

                //Add the style, not in the loop because we cant add a style attribute while looping through the attributes
                foreach (var styleValueToAdd in attributesToAdd)
                {
                    AddInlineCssToNode(htmlNode, styleValueToAdd, doc);
                }

                //delete attributes outside the loop
                foreach (var attributeToDelete in listOfAttibutesToDelete)
                {
                    htmlNode.Attributes.Remove(attributeToDelete);
                }
            }

            //check the elements, like: b i u etc
            foreach (var tagtoConvert in _convertionElementsDictionary.Keys)
            {
                var nodeCollectionToConvert =
                    htmlNode.SelectNodes(string.Format(XpathStrip, string.Format(GetAllowedTag, tagtoConvert)));

                if (nodeCollectionToConvert == null || nodeCollectionToConvert.Count == 0) continue;
                listOfElementsToDelete.AddRange(nodeCollectionToConvert);
                foreach (var nodeToDelete in listOfElementsToDelete)
                {
                    if (nodeToDelete.ParentNode == null) continue;
                    //if node contains divs then create a div, otherwise create a span
                    var node =
                        doc.CreateElement(nodeToDelete.ChildNodes.Any(n => n.Name.ToLower().Contains("div"))
                            ? "div"
                            : "span");

                    for (var x = 0; x <= nodeToDelete.ChildNodes.Count - 1; x++)
                    {
                        if (nodeToDelete.ChildNodes[x] == null) continue;
                        var clonedNode = nodeToDelete.ChildNodes[x].CloneNode(true);
                        node.AppendChild(clonedNode);
                    }
                    var value = _convertionElementsDictionary[tagtoConvert];
                    AddInlineCssToNode(node, value, doc);
                    nodeToDelete.ParentNode.ReplaceChild(node, nodeToDelete);
                }
            }
        }


        /// <summary>
        ///     Strip unknown tags
        /// </summary>
        /// <param name="dummyElement"></param>
        /// <param name="checkRoot"></param>
        private static void StripAllUnknownTags(HtmlNode dummyElement, bool checkRoot)
        {
            //these tags are allowed on the first level
            const string listOfAllowedRootTags =
                "responseDeclaration|outcomeDeclaration|stylesheet|itemBody|responseProcessing";
            const string listOfAllowedTags =
                "positionObjectStage|gapMatchInteraction|matchInteraction|graphicGapMatchInteraction|hotspotInteraction|graphicOrderInteraction|hottextInteraction|" +
                "selectPointInteraction|graphicAssociateInteraction|sliderInteraction|choiceInteraction|customInteraction|mediaInteraction|orderInteraction|extendedTextInteraction|" +
                "associateInteraction|uploadInteraction|pre|h2|h3|h1|h6|h4|h5|p|address|dl|ol|hr|rubricBlock|blockquote|feedbackBlock|ul|table|div|xi:include|m:math|&nbsp;";
            if (checkRoot)
            {
                RemoveUnsuppportedElements(dummyElement, listOfAllowedRootTags);

                foreach (var childNode in dummyElement.ChildNodes)
                {
                    if (!childNode.Name.ToLower().Contains("itembody")) continue;
                    RemoveUnsuppportedElements(childNode, listOfAllowedTags);
                    //now loop through the childnodes to validate these.
                    foreach (var node in childNode.ChildNodes)
                    {
                        CleanupChildNodes(node);
                    }
                }
            }
            else
            {
                RemoveUnsuppportedElements(dummyElement, listOfAllowedTags);
                //now loop through the childnodes to validate these.
                foreach (var childNode in dummyElement.ChildNodes)
                {
                    CleanupChildNodes(childNode);
                }
            }
        }


        /// <summary>
        /// </summary>
        /// <param name="node"></param>
        private static void CleanupChildNodes(HtmlNode node)
        {
            var supportedAttributes = GetSupportedAttributes(node.Name);
            var supportedTags = GetSupportedElements(node.Name);
            if (!string.IsNullOrEmpty(supportedTags) || !string.IsNullOrEmpty(supportedAttributes))
            {
                RemoveUnsupportedAttributes(node, supportedAttributes);
                RemoveUnsuppportedElements(node, supportedTags);
            }
            if (node.ChildNodes == null) return;
            foreach (var childNode in node.ChildNodes)
            {
                //loop recursive
                CleanupChildNodes(childNode);
            }
        }

        /// <summary>
        ///     Removes the unsuppported elements.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="supportedElements">The supported elements.</param>
        /// <history>
        ///     [marcelh] 16-7-2012 Created
        /// </history>
        private static void RemoveUnsuppportedElements(HtmlNode node, string supportedElements)
        {
            //build a xpath query that get all elements, not in the supported elements list
            var xpathBuilderForChildNodes = new StringBuilder(string.Empty);
            foreach (var allowedChildTag in supportedElements.Split('|'))
            {
                xpathBuilderForChildNodes.AppendFormat(ExcludeTags, allowedChildTag);
            }
            if (supportedElements.Contains("*")) return;
            var notSupportedChildTagCollection =
                node.SelectNodes(string.Format(XpathStrip, xpathBuilderForChildNodes));
            var listOfNotSupportedElements = new List<HtmlNode>();

            if (notSupportedChildTagCollection == null) return;
            listOfNotSupportedElements.AddRange(notSupportedChildTagCollection.Where(unSupportedTag =>
                !(unSupportedTag is HtmlTextNode) && !(unSupportedTag is HtmlCommentNode)));

            //delete outside the loop
            foreach (var nodeToDelete in listOfNotSupportedElements.Where(n => !n.ParentNode.Name.ToLower().Equals("p"))
            )
            {
                nodeToDelete.ParentNode.RemoveChild(nodeToDelete);
            }
        }

        /// <summary>
        ///     Removes the un supported attributes.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <param name="supportedAttributes">The supported attributes.</param>
        /// <history>
        ///     [marcelh] 16-7-2012 Created
        /// </history>
        private static void RemoveUnsupportedAttributes(HtmlNode node, string supportedAttributes)
        {
            var listOfAttributes = new List<string>();
            listOfAttributes.AddRange(supportedAttributes.Split('|'));

            //remove attributes

            //If not defined which attributes are supported, don't strip all attributes.
            if (listOfAttributes.Contains("*") || node.Attributes == null) return;
            var listOfAttributesToRemove = (from attr in node.Attributes
                                            where !listOfAttributes.Contains(attr.Name, StringComparer.OrdinalIgnoreCase) &&
                                                  string.Compare(attr.Name, "style", StringComparison.OrdinalIgnoreCase) != 0
                                            select attr.Name).ToList();

            // remove attributes that are not supported
            foreach (var attributeToRemove in listOfAttributesToRemove)
            {
                node.Attributes.Remove(attributeToRemove);
            }
        }

        /// <summary>
        ///     Gets the supported elements.
        /// </summary>
        /// <param name="elementName">Name of the element.</param>
        /// <returns></returns>
        /// <history>
        ///     [marcelh] 16-7-2012 Created
        /// </history>
        private static string GetSupportedElements(string elementName)
        {
            var returnValue = "*";
            //if not defined don't strip
            switch (elementName.ToLower())
            {
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                case "p":
                case "span":
                    returnValue =
                        "textEntryInteraction|inlineChoiceInteraction|endAttemptInteraction|hottext|img|br|printedVariable|object|gap|em|a|code|span|sub|acronym|big" +
                        "|tt|kbd|q|i|dfn|feedbackInline|abbr|strong|sup|var|small|samp|b|cite|xi:include|m:math|hottext";
                    break;
                case "ol":
                    returnValue = "li";
                    break;
                case "td":
                case "div":
                case "li":
                    returnValue =
                        "pre|h2|h3|h1|h6|h4|h5|p|address|dl|ol|img|br|ul|hr|printedVariable|object|rubricBlock|blockquote|feedbackBlock" +
                        "|hottext|em|a|code|span|sub|acronym|big|tt|kbd|q|i|dfn|feedbackInline|abbr|strong|sup|var|small|samp|b|cite|table" +
                        "|div|xi:include|m:math|textEntryInteraction|inlineChoiceInteraction|endAttemptInteraction|customInteraction|gapMatchInteraction" +
                        "|matchInteraction|graphicGapMatchInteraction|hotspotInteraction|graphicOrderInteraction|selectPointInteraction|graphicAssociateInteraction" +
                        "|sliderInteraction|choiceInteraction|mediaInteraction|orderInteraction|extendedTextInteraction|associateInteraction|hottextInteraction|positionObjectStage" +
                        "|uploadInteraction";
                    break;
                case "table":
                    returnValue = "caption|col|colgroup|thead|tfoot|tbody";
                    break;
                case "tbody":
                    returnValue = "tr";
                    break;
                case "colgroup":
                    returnValue = "col";
                    break;
                case "tr":
                    returnValue = "th|td";
                    break;
            }

            return returnValue;
        }

        /// <summary>
        ///     Gets the supported attributes.
        /// </summary>
        /// <param name="elementName">Name of the element.</param>
        /// <returns></returns>
        /// <history>
        ///     [marcelh] 16-7-2012 Created
        /// </history>
        private static string GetSupportedAttributes(string elementName)
        {
            var returnValue = "*";
            //if not defined don't strip
            if (elementName == null) return returnValue;
            switch (elementName.ToLower())
            {
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                case "p":
                case "span":
                case "div":
                case "pre":
                    returnValue = "class|id|label|xml:base|xml:lang|xsi:type";
                    break;
                case "ol":
                case "li":
                case "sup":
                case "sub":
                case "tbody":
                case "tr":
                case "ul":
                    returnValue = "class|id|label|xml:base|xml:lang|xsi:type";
                    break;
                case "img":
                    returnValue = "alt|src|height|width|longdesc|class|id|label|xml:base|xml:lang|xsi:type";
                    break;
                case "td":
                    returnValue = "abbr|axis|colspan|rowspan|scope|headers|class|id|label|header|xml:lang|xsi:type";
                    break;
                case "table":
                    returnValue = "summary|class|id|label|xml:base|xml:lang|xsi:type";
                    break;
                case "col":
                case "colgroup":
                    returnValue = "class|id|label|span|xml:lang|xsi:type";
                    break;
                case "mediainteraction":
                    returnValue = "class|id|label|span|responseidentifier|autostart|maxplays|xml:lang|xsi:type";
                    break;
            }

            return returnValue;
        }

        private void FixIds(ref HtmlDocument doc)
        {
            if (doc?.DocumentNode == null) return;
            var nodeCollection = doc.DocumentNode.SelectNodes(XpathIds);
            var index = 1;
            if (nodeCollection == null) return;
            foreach (var htmlNode in nodeCollection)
            {
                htmlNode.Attributes["id"].Value = $"ID_{index}";
                index++;
            }
        }

        /// <summary>
        ///     Exstract the inline css to a seperate css file.
        /// </summary>
        /// <param name="doc">The xHtml doc.</param>
        /// <history>
        ///     [marcelh] 12-7-2012 Created
        /// </history>
        private void ExtractAllStyles(ref HtmlDocument doc)
        {
            var styleAdded = false;
            if (doc?.DocumentNode == null) return;
            var nodeCollection = doc.DocumentNode.SelectNodes(XpathStyles);

            string trClassName = null;
            if (nodeCollection == null) return;
            var index = 0;
            foreach (var htmlNode in nodeCollection)
            {
                index++;
                var style = string.Empty;
                if (htmlNode.Attributes["style"] != null) style = htmlNode.Attributes["style"].Value.Trim();
                if (string.IsNullOrEmpty(style)) continue;
                var className = $"{GeneratedClassname}_{GetUniqueId()}_{index}";
                switch (htmlNode.Name)
                {
                    case "tr":
                        trClassName = className;
                        break;
                    //Remember the class that was intended to be put on the TR. Due to some 'weirdness' in IE (standard-mode vs quirks-mode) TR-borders are impossible to be overridden in a TD. When adding the TR-style to the TD element, all works as to be expected.
                    case "td":
                        if (htmlNode.Attributes["class"] == null)
                            htmlNode.Attributes.Append(doc.CreateAttribute("class"));
                        style = AddNoLineBorderAttributes(style);
                        className =
                            string.Concat(htmlNode.Attributes["class"].Value, " ", trClassName, " ", className).Trim();
                        break;
                }
                style = AddImportantRules(style);
                if (_stylesAdded.ContainsKey(style))
                {
                    className = _stylesAdded[style];
                }
                else
                {
                    _stylesAdded.Add(style, className);
                }
                if (htmlNode.Attributes["class"] == null) htmlNode.Attributes.Append(doc.CreateAttribute("class"));
                htmlNode.Attributes["class"].Value =
                    string.Concat(htmlNode.Attributes["class"].Value, " ", className).Trim();
                var htmlAttribute = htmlNode.Attributes["style"];
                htmlAttribute?.Remove();
                styleAdded = true;
            }
            if (styleAdded)
            {
                AddAsDependency("generated_styles.css");
            }
        }

        private static string GetUniqueId()
        {
            var g = Guid.NewGuid();
            var guidString = Convert.ToBase64String(g.ToByteArray());
            guidString = guidString.Replace("=", "");
            return guidString.Replace("+", "");
        }

        private static string AddNoLineBorderAttributes(string styleString)
        {
            var newStyle = new StringBuilder(styleString);
            var styles = styleString.Split(";".ToCharArray());
            var borderLeftWidthExists = false;
            var borderTopWidthExists = false;
            var borderRightWidthExists = false;
            var borderBottomWidthExists = false;

            foreach (var style in styles)
            {
                if (style.Trim().StartsWith("border-left-width", StringComparison.CurrentCultureIgnoreCase) ||
                    style.Trim().StartsWith("border-left", StringComparison.CurrentCultureIgnoreCase))
                    borderLeftWidthExists = true;
                if (style.Trim().StartsWith("border-top-width", StringComparison.CurrentCultureIgnoreCase) ||
                    style.Trim().StartsWith("border-top", StringComparison.CurrentCultureIgnoreCase))
                    borderTopWidthExists = true;
                if (style.Trim().StartsWith("border-right-width", StringComparison.CurrentCultureIgnoreCase) ||
                    style.Trim().StartsWith("border-right", StringComparison.CurrentCultureIgnoreCase))
                    borderRightWidthExists = true;
                if (style.Trim().StartsWith("border-bottom-width", StringComparison.CurrentCultureIgnoreCase) ||
                    style.Trim().StartsWith("border-bottom", StringComparison.CurrentCultureIgnoreCase))
                    borderBottomWidthExists = true;
            }
            if (!styleString.EndsWith(";"))
                newStyle.Append(";");
            if (!borderLeftWidthExists)
                newStyle.Append(" border-left-width: 0px;");
            if (!borderTopWidthExists)
                newStyle.Append(" border-top-width: 0px;");
            if (!borderRightWidthExists)
                newStyle.Append(" border-right-width: 0px;");
            if (!borderBottomWidthExists)
                newStyle.Append(" border-bottom-width: 0px;");

            return newStyle.ToString();
        }

        private static string AddImportantRules(string style)
        {
            if (style.Contains(":") && !style.EndsWith(";"))
                style = $"{style};";
            if (!style.Contains(";")) return style;
            style = style.Replace(";", "!important;");
            //In case styling already had important attribute, skip one.
            style = style.Replace("!important!important;", "!important;");
            style = style.Replace("!important; !important;", "!important;");
            return style;
        }


        private void FixImageTag(ref HtmlDocument doc, string itemcode)
        {
            var newImages = doc.GetImages(itemcode, _resourceHandler)?.ToList();
            newImages?.ForEach(i =>
            {
                var name = $"IMG-{i.Key}";
                AddAsDependency(name);
                if (_images.ContainsKey(name)) return;
                var dir = new DirectoryInfo(Path.Combine(_tempPath, "img"));
                if (!dir.Exists) dir.Create();
                var fileName = Path.Combine(dir.ToString(), $"{name}");
                File.WriteAllBytes(fileName, i.Value);
                _images.Add(name, fileName);
            });
        }

        private void AddAsDependency(string name)
        {
            if (!_dependencies.ContainsKey(_itemId))
                _dependencies.Add(_itemId, new HashSet<string>());
            var list = _dependencies[_itemId];
            list.Add(name);
            _dependencies[_itemId] = list;
        }


        /// <summary>
        ///     Fixes the table tag. in qti the table structure is:
        ///     <table>
        ///         <tbody>
        ///             <tr>
        ///                 <td></td>
        ///             </tr>
        ///         </tbody>
        ///     </table>
        /// </summary>
        /// <param name="doc">The doc.</param>
        /// <history>
        ///     [marcelh] 12-7-2012 Created
        /// </history>
        private static void FixTableTag(ref HtmlDocument doc)
        {
            var nodeCollection = doc?.DocumentNode.SelectNodes("//table");
            if (nodeCollection == null) return;
            foreach (var tableNode in nodeCollection)
            {
                if (tableNode.SelectNodes("//tbody") != null && tableNode.SelectNodes("//tbody").Count != 0) continue;
                var clonedTableNode = tableNode.CloneNode(true);
                RemoveAllchildNodesFromNode(tableNode);
                var tbodyNode = doc.CreateElement("tbody");
                foreach (var tableNodeToAdd in clonedTableNode.ChildNodes)
                {
                    tbodyNode.AppendChild(tableNodeToAdd);
                }
                tableNode.AppendChild(tbodyNode);
            }
        }

        /// <summary>
        ///     Removes the allchild nodes from node.
        /// </summary>
        /// <param name="xmlNode">The XML node.</param>
        /// <history>
        ///     [marcelh] 25-7-2012 Created
        /// </history>
        private static void RemoveAllchildNodesFromNode(HtmlNode xmlNode)
        {
            if (xmlNode.ChildNodes == null) return;
            for (var i = 1; i <= xmlNode.ChildNodes.Count; i++)
            {
                xmlNode.RemoveChild(xmlNode.ChildNodes[0]);
            }
        }

        /// <summary>
        ///     Wrapps in paragraph if not yet in paragraph
        /// </summary>
        /// <param name="xHtml"></param>
        /// <returns></returns>
        private static string FixMissingParagraph(string xHtml)
        {
            //if the first character is not a < or is a <span, surround with a paragraph
            if (xHtml.Trim().Length == 0 ||
                xHtml.Trim().Length != 0 && (xHtml.Trim().Substring(0, 1) != "<" ||
                                             xHtml.Trim().Length > 5 && xHtml.Trim().Substring(0, 5) == "<span" ||
                                             xHtml.Trim().Length > 4 && xHtml.Trim().Substring(0, 4) == "<img"))
            {
                xHtml = $"<p>{xHtml}</p>";
            }
            return xHtml;
        }

        ///// <summary>
        /////     Adds the style to CSS.
        ///// </summary>
        ///// <param name="cssStringBuilder">The CSS string builder.</param>
        ///// <param name="className">Name of the class.</param>
        ///// <param name="styles">The styles.</param>
        ///// <history>
        /////     [marcelh] 12-7-2012 Created
        ///// </history>
        //private static void AddStyleToCss(StringBuilder cssStringBuilder, string className, string styles)
        //{
        //    const string formatCss = ".{0}{1}{{{1}{2}{1}}}";
        //    //add return after ;
        //    styles = styles.Replace(";", $";{Environment.NewLine}");
        //    styles = string.Format(formatCss, className, Environment.NewLine, styles);
        //    cssStringBuilder.AppendLine(styles);
        //}

        /// <summary>
        ///     Shoulds the convert to inline CSS. Some interaction tags can have e.g. width and height that should be converted to
        ///     inline css. Thats why there is a list of tags, for which converting of attributes and styles should occur.
        /// </summary>
        /// <param name="tagname">The tagname.</param>
        /// <returns></returns>
        /// <history>
        ///     [marcelh] 17-7-2012 Created
        /// </history>
        private static bool ShouldConvertToInlineCss(string tagname)
        {
            var returnValue = tagname.ToLower() == "h1" || tagname.ToLower() == "h2" || tagname.ToLower() == "h3" ||
                              tagname.ToLower() == "h4" || tagname.ToLower() == "h5" || tagname.ToLower() == "h6" ||
                              tagname.ToLower() == "p" || tagname.ToLower() == "span" || tagname.ToLower() == "ol" ||
                              tagname.ToLower() == "li" || tagname.ToLower() == "td" || tagname.ToLower() == "table" ||
                              tagname.ToLower() == "tbody" || tagname.ToLower() == "colgroup" ||
                              tagname.ToLower() == "col" ||
                              tagname.ToLower() == "tr" || tagname.ToLower() == "div";

            return returnValue;
        }

        private static void AddInlineCssToNode(HtmlNode node, string value, HtmlDocument doc)
        {
            if (node.Attributes["style"] == null)
                node.Attributes.Append(doc.CreateAttribute("style"));
            string newvalue;
            var regexToCheckIfStyleAlreayExists = $"{Regex.Match(value, ".+?:").Value.Replace(":", string.Empty)}.+?;";
            // node.Attributes("style").Value.Contains(value.Substring(0, value.IndexOf(":"c) + 1))
            if (
                !Regex.Match(node.Attributes["style"].Value, regexToCheckIfStyleAlreayExists, RegexOptions.IgnoreCase)
                    .Success)
            {
                newvalue = string.Concat(node.Attributes["style"].Value,
                    node.Attributes["style"].Value.EndsWith(";") ? " " : "; ", value).Trim();
            }
            else
            {
                newvalue = Regex.Replace(node.Attributes["style"].Value,
                    regexToCheckIfStyleAlreayExists, value, RegexOptions.IgnoreCase);
            }
            node.Attributes["style"].Value = newvalue;
        }

        #endregion
    }
}
