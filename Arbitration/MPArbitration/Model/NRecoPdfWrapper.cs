using NReco.PdfGenerator;
using System.Text.RegularExpressions;
using System.Xml;

namespace MPArbitration.Model
{
    public class NRecoPdfWrapper
    {
        //30 day trial license
        //static string licenseKey = "pjfsL9eBhU5mER7ULRNO/pgqeXBsJF15ea8d+vpzJ/ja8LpELgrs2FoaZwNLRmsJpgwKahfzSvyCZDHZsI0Bs9KCaby6CXo02YxpA3iiwJPaDdfO+vTK/JCsjH/d9l6118KUVjegNFAraGSKA3Q6tMTg4/injiZ9wZ3kcjoXPjs=";
        //static string licenseOwner = "DEMO";

        // license purchased by JB 2/21/2023
        static string licenseOwner = "PDF_Generator_Src_Examples_Pack_254174562496"; // john.baldwin@mpowerhealth.com licensed it
        static string licenseKey = "A5xJw5Eez3BGMXdochWyBmFdMcBqqX/YrAj587hjrE4NRK9y6Bx0cfWekpWKyUPtf2MVFdTamnhSh43bXe7pjdfXaqqw2TO3XV6OWSvnGKl+8edTOh/XyozTcWBA9ektyo//ZDtSwe/O1KXN70UG+kK/uUx15uR4SBUHy4X0zgQ=";
        ILogger? _logger;

        public NRecoPdfWrapper(ILogger? logger)
        {
            _logger = logger;
        }

        public static byte[]? GeneratePDF(ILogger? logger, string html, Dictionary<string, string> base64Images, Dictionary<string, string> formValues, out string message)
        {
            message = string.Empty;
            var generator = new NRecoPdfWrapper(logger);

            var tmp = html.Replace("<br>", "<br />"); //.Replace("&nbsp;", "&#160;").Replace("&amp;", "&#38;").Replace("&lt;", "&#60;").Replace("&gt;", "&#62;");
            tmp = Regex.Replace(tmp, @"&(?!\w+;)", "&amp;");

            if (!tmp.StartsWith("<!DOCTYPE") && !tmp.StartsWith("<html")) { 
                tmp = "<!DOCTYPE inline_dtd[<!ENTITY nbsp '&#160;'><!ENTITY amp '&#38;'><!ENTITY lt '&#60;'><!ENTITY gt '&#62;'>]>\r\n<html><head><meta charset=\"utf-8\"/></head><body>" + tmp + "</body></html>";
            }

            try
            {
                using (var strm = generator.Run(tmp, base64Images, formValues))
                {
                    return strm.ToArray();
                }
            }
            catch (Exception ex)
            {
                message = "<html lang=\"en\"><body><p>Error rendering html to PDF: " + ex.Message.Replace("<", "[").Replace(">", "]");
                if (ex.InnerException != null)
                    message += " <br/>" + ex.InnerException.Message.Replace("<", "[").Replace(">", "]");
                message += "</p></body></html>";

                if (logger != null)
                {
                    logger.LogError(ex.Message);
                    if (ex.InnerException != null)
                        logger.LogError(ex.InnerException.Message);
                }
            }
            return null;
        }

        private MemoryStream Run(string html, Dictionary<string, string> base64Images, Dictionary<string, string> form)
        {
            string s = html;

            // NOTE: If images are not needed then this section can be removed and the straight html can go into the converter (below)
            // Loading an HTML document into an XML processor carries the risk of the processor freaking out on stray ampersands / entities
            // that are not accounted for in the inline DTD
            if (base64Images.Count > 0)
            {
                var xml = new XmlDocument();
                xml.LoadXml(html);
                var images = xml.SelectNodes("//img");

                //drop in inline images
                if (images != null)
                {
                    foreach (XmlElement xChild in images) //xml.SelectNodes("/html/body/*"))
                    {
                        switch (xChild.Name)
                        {
                            case "img":
                            case "showImage":
                                var src = xChild.GetAttribute("src");
                                if (base64Images.ContainsKey(src))
                                {
                                    var value = base64Images[src];
                                    if (src.ToLower().Contains("signature"))
                                    {
                                        xChild.SetAttribute("style", "height:3em");
                                        xChild.RemoveAttribute("width");
                                    }
                                    xChild.SetAttribute("src", "data:image/png;base64," + value);
                                }
                                break;
                        }
                    }
                }
                s = xml.InnerXml;
            }

            var converter = new HtmlToPdfConverter();
            converter.License.SetLicenseKey(NRecoPdfWrapper.licenseOwner, NRecoPdfWrapper.licenseKey);
            
            converter.WkHtmlToPdfExeName = "wkhtmltopdf.exe";
            converter.PdfToolPath = @"c:\home\site\deployments\tools\wkhtmltopdf";

            var stream = new MemoryStream();
            converter.GeneratePdf(s, null, stream);
            
            return stream;
        }
    }
}
