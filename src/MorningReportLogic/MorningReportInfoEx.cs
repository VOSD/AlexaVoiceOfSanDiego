using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace VoiceOfSanDiego.Alexa.MorningReport {
    public static class MorningReportInfoEx {

        //--- Class Fields ---
        private static readonly Regex _htmlEntitiesRegEx = new Regex("&(?<value>#(x[a-f0-9]+|[0-9]+)|[a-z0-9]+);", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        //--- Class Methods ---
        public static string ConvertContentsToSsml(
            this MorningReportInfo morningReport,
            string _preHeadingBreak = "750ms",
            string _postHeadingBreak = "250ms",
            string _bulletBreak = "750ms"
        ) {

            // extract all inner text nodes
            var ssml = new XDocument(new XElement("speak"));
            var root = ssml.Root;

            // add title when present
            if(morningReport.Title != null) {
                root.Add(new XText(morningReport.Title + " "));
                root.Add(new XElement("break", new XAttribute("time", _postHeadingBreak)));
            }

            // convert HTML to SSML format
            Visit(root, morningReport.Document.Root);
            root.Add(new XElement("p", new XText($"This was the morning report by {morningReport.Author}, published on {morningReport.Date:dddd, MMMM d, yyyy}.")));
            return ssml.ToString();

            //--- Local Functions ---
            void VisitNodes(XElement parent, XElement element) {
                foreach(var node in element.Nodes()) {
                    Visit(parent, node);
                }
            }

            void Visit(XElement parent, XNode node) {
                switch(node) {
                case XElement xelement:
                    var name = xelement.Name.ToString();
                    switch(name) {
                    case "p":
                        var p = new XElement("p");
                        parent.Add(p);
                        VisitNodes(p, xelement);
                        break;
                    case "h1":
                    case "h2":
                    case "h3":
                    case "h4":
                    case "h5":
                    case "h6":
                        parent.Add(new XElement("break", new XAttribute("time", _preHeadingBreak)));
                        VisitNodes(parent, xelement);
                        parent.Add(new XElement("break", new XAttribute("time", _postHeadingBreak)));
                        break;
                    default:
                        VisitNodes(parent, xelement);
                        break;
                    }
                    break;
                case XText xtext:
                    var decodedText = DecodeHtmlEntities(xtext.Value);

                    // add bauses to bullet points
                    if(decodedText.TrimStart().StartsWith("\u2022")) {
                        parent.Add(new XElement("break", new XAttribute("time", _bulletBreak)));
                    }
                    parent.Add(new XText(decodedText));
                    break;
                }
            }
        }

        public static string ConvertContentsToText(this MorningReportInfo morningReport) {

            // extract all inner text nodes
            var text = new StringBuilder();

            // add title when present
            if(morningReport.Title != null) {
                text.AppendLine($"=== {morningReport.Title} ===");
                if(morningReport.Author != null) {
                    text.AppendLine($"{morningReport.Date:dddd, MMMM d, yyyy} by {morningReport.Author}");
                } else {
                    text.AppendLine(morningReport.Date.ToString("dddd, MMMM d, yyyy"));
                }
                text.AppendLine();
            }

            // convert HTML to plain-text format
            Visit(morningReport.Document.Root);
            return text.ToString();

            //--- Local Functions ---
            void VisitNodes(XElement element) {
                foreach(var node in element.Nodes()) {
                    Visit(node);
                }
            }

            void Visit(XNode node) {
                switch(node) {
                case XElement xelement:
                    var name = xelement.Name.ToString();
                    switch(name) {
                    case "p":
                        VisitNodes(xelement);
                        text.AppendLine();
                        text.AppendLine();
                        break;
                    case "h1":
                    case "h2":
                    case "h3":
                    case "h4":
                    case "h5":
                    case "h6":
                        text.Append("-- ");
                        VisitNodes(xelement);
                        text.Append(" --");
                        text.AppendLine();
                        break;
                    default:
                        VisitNodes(xelement);
                        break;
                    }
                    break;
                case XText xtext:
                    text.Append(DecodeHtmlEntities(xtext.Value));
                    break;
                }
            }
        }

        public static string DecodeHtmlEntities(string text) {
            return _htmlEntitiesRegEx.Replace(text, m => {
                string v = m.Groups["value"].Value;
                if(v[0] == '#') {
                    if(char.ToLowerInvariant(v[1]) == 'x') {
                        string value = v.Substring(2);
                        return ((char)int.Parse(value, NumberStyles.HexNumber)).ToString();
                    } else {
                        string value = v.Substring(1);
                        return ((char)int.Parse(value)).ToString();
                    }
                } else {
                    switch(v) {
                    case "amp":
                        return "&";
                    case "apos":
                        return "'";
                    case "gt":
                        return ">";
                    case "lt":
                        return "<";
                    case "quot":
                        return "\"";
                    }
                    return v;
                }
            }, int.MaxValue);
        }
    }
}