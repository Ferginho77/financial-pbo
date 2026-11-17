using System.Text;
using Microsoft.AspNetCore.Html;

namespace Kantongku.Helpers;

/// <summary>Ikon SVG inline (gaya outline) agar tidak bergantung pada library ikon eksternal.</summary>
public static class Icons
{
    private static readonly Dictionary<string, string> Paths = new()
    {
        ["plus"] = "<line x1=\"12\" y1=\"5\" x2=\"12\" y2=\"19\"/><line x1=\"5\" y1=\"12\" x2=\"19\" y2=\"12\"/>",
        ["minus"] = "<line x1=\"5\" y1=\"12\" x2=\"19\" y2=\"12\"/>",
        ["wallet"] = "<path d=\"M20 12V8H6a2 2 0 0 1 0-4h12v4\"/><path d=\"M4 6v12a2 2 0 0 0 2 2h14v-4\"/><path d=\"M18 12a2 2 0 0 0 0 4h4v-4Z\"/>",
        ["swap"] = "<polyline points=\"17 1 21 5 17 9\"/><path d=\"M3 11V9a4 4 0 0 1 4-4h14\"/><polyline points=\"7 23 3 19 7 15\"/><path d=\"M21 13v2a4 4 0 0 1-4 4H3\"/>",
        ["arrow-down-left"] = "<line x1=\"17\" y1=\"7\" x2=\"7\" y2=\"17\"/><polyline points=\"17 17 7 17 7 7\"/>",
        ["arrow-up-right"] = "<line x1=\"7\" y1=\"17\" x2=\"17\" y2=\"7\"/><polyline points=\"7 7 17 7 17 17\"/>",
        ["arrow-left"] = "<line x1=\"19\" y1=\"12\" x2=\"5\" y2=\"12\"/><polyline points=\"12 19 5 12 12 5\"/>",
        ["chevron-left"] = "<polyline points=\"15 18 9 12 15 6\"/>",
        ["chevron-right"] = "<polyline points=\"9 18 15 12 9 6\"/>",
        ["users"] = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>",
        ["trash"] = "<polyline points=\"3 6 5 6 21 6\"/><path d=\"M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6\"/><path d=\"M10 11v6\"/><path d=\"M14 11v6\"/><path d=\"M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2\"/>",
        ["edit"] = "<path d=\"M12 20h9\"/><path d=\"M16.5 3.5a2.121 2.121 0 0 1 3 3L7 19l-4 1 1-4Z\"/>",
        ["logout"] = "<path d=\"M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4\"/><polyline points=\"16 17 21 12 16 7\"/><line x1=\"21\" y1=\"12\" x2=\"9\" y2=\"12\"/>",
        ["tag"] = "<path d=\"M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z\"/><line x1=\"7\" y1=\"7\" x2=\"7.01\" y2=\"7\"/>",
        ["grid"] = "<rect x=\"3\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"14\" y=\"3\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"14\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\"/><rect x=\"3\" y=\"14\" width=\"7\" height=\"7\" rx=\"1\"/>",
        ["eye"] = "<path d=\"M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z\"/><circle cx=\"12\" cy=\"12\" r=\"3\"/>",
        ["lock"] = "<rect x=\"3\" y=\"11\" width=\"18\" height=\"11\" rx=\"2\" ry=\"2\"/><path d=\"M7 11V7a5 5 0 0 1 10 0v4\"/>",
        ["shield"] = "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/><polyline points=\"9 12 11 14 15 10\"/>",
        ["mail"] = "<path d=\"M4 4h16a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2z\"/><polyline points=\"22,6 12,13 2,6\"/>",
        ["check"] = "<polyline points=\"20 6 9 17 4 12\"/>",
        ["x"] = "<line x1=\"18\" y1=\"6\" x2=\"6\" y2=\"18\"/><line x1=\"6\" y1=\"6\" x2=\"18\" y2=\"18\"/>",
        ["pie"] = "<path d=\"M21.21 15.89A10 10 0 1 1 8 2.83\"/><path d=\"M22 12A10 10 0 0 0 12 2v10z\"/>",
        ["inbox"] = "<polyline points=\"22 12 16 12 14 15 10 15 8 12 2 12\"/><path d=\"M5.45 5.11L2 12v6a2 2 0 0 0 2 2h16a2 2 0 0 0 2-2v-6l-3.45-6.89A2 2 0 0 0 16.76 4H7.24a2 2 0 0 0-1.79 1.11z\"/>",
        ["search"] = "<circle cx=\"11\" cy=\"11\" r=\"8\"/><line x1=\"21\" y1=\"21\" x2=\"16.65\" y2=\"16.65\"/>",
        ["download"] = "<path d=\"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4\"/><polyline points=\"7 10 12 15 17 10\"/><line x1=\"12\" y1=\"15\" x2=\"12\" y2=\"3\"/>",
        ["calendar"] = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2\" ry=\"2\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>",
        ["user"] = "<path d=\"M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2\"/><circle cx=\"12\" cy=\"7\" r=\"4\"/>",
        ["target"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><circle cx=\"12\" cy=\"12\" r=\"6\"/><circle cx=\"12\" cy=\"12\" r=\"2\"/>"
    };

    public static IHtmlContent Get(string name, int size = 18)
    {
        if (!Paths.TryGetValue(name, out var inner))
        {
            return HtmlString.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("<svg class=\"icon\" xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(size).Append("\" height=\"").Append(size)
          .Append("\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">")
          .Append(inner).Append("</svg>");
        return new HtmlString(sb.ToString());
    }
}
