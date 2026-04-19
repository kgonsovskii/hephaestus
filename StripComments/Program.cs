using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: StripComments <root-dir> [more-roots...]");
    return 1;
}

var roots = args.Select(Path.GetFullPath).ToArray();
var opts = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "bin", "obj", ".git", "node_modules", "packages", ".vs"
};

var htmlXmlComment = new Regex("<!--[\\s\\S]*?-->", RegexOptions.Compiled, TimeSpan.FromSeconds(10));
var razorComment = new Regex("@\\*[\\s\\S]*?\\*@", RegexOptions.Compiled, TimeSpan.FromSeconds(10));

foreach (var root in roots)
{
    if (!Directory.Exists(root))
    {
        Console.Error.WriteLine("Skip missing: " + root);
        continue;
    }

    foreach (var path in Directory.EnumerateFiles(root, "*.cs", opts))
    {
        if (PathSplit(path).Any(p => skip.Contains(p)))
            continue;

        var text = File.ReadAllText(path);
        var tree = CSharpSyntaxTree.ParseText(text, path: path);
        var rootNode = tree.GetRoot();
        var newRoot = new CommentStripper().Visit(rootNode);
        var stripped = newRoot.ToFullString();
        if (string.Equals(stripped, text, StringComparison.Ordinal))
            continue;

        var outText = stripped.EndsWith('\n') ? stripped : stripped + Environment.NewLine;
        File.WriteAllText(path, outText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine(path);
    }

    foreach (var path in Directory.EnumerateFiles(root, "*.cshtml", opts))
    {
        if (PathSplit(path).Any(p => skip.Contains(p)))
            continue;

        var text = File.ReadAllText(path);
        var a = htmlXmlComment.Replace(text, "");
        var b = razorComment.Replace(a, "");
        if (string.Equals(b, text, StringComparison.Ordinal))
            continue;

        var outText = b.EndsWith('\n') ? b : b + Environment.NewLine;
        File.WriteAllText(path, outText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine(path);
    }

    foreach (var path in Directory.EnumerateFiles(root, "*.props", opts))
        StripXmlCommentsFile(path, skip, htmlXmlComment);

    foreach (var path in Directory.EnumerateFiles(root, "*.csproj", opts))
        StripXmlCommentsFile(path, skip, htmlXmlComment);
}

return 0;

static IEnumerable<string> PathSplit(string path) =>
    path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

static void StripXmlCommentsFile(string path, HashSet<string> skip, Regex htmlXmlComment)
{
    if (PathSplit(path).Any(p => skip.Contains(p)))
        return;

    var text = File.ReadAllText(path);
    var b = htmlXmlComment.Replace(text, "");
    if (string.Equals(b, text, StringComparison.Ordinal))
        return;

    var outText = b.EndsWith('\n') ? b : b + Environment.NewLine;
    File.WriteAllText(path, outText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Console.WriteLine(path);
}

file sealed class CommentStripper : CSharpSyntaxRewriter
{
    public override SyntaxToken VisitToken(SyntaxToken token)
    {
        token = base.VisitToken(token);
        if (!token.HasLeadingTrivia && !token.HasTrailingTrivia)
            return token;

        var lead = Strip(token.LeadingTrivia);
        var trail = Strip(token.TrailingTrivia);
        return token.WithLeadingTrivia(lead).WithTrailingTrivia(trail);
    }

    private static SyntaxTriviaList Strip(SyntaxTriviaList list)
    {
        var kept = new List<SyntaxTrivia>();
        foreach (var t in list)
        {
            if (IsRemoved(t))
                continue;
            kept.Add(t);
        }

        return SyntaxFactory.TriviaList(kept);
    }

    private static bool IsRemoved(SyntaxTrivia t)
    {
        if (t.IsKind(SyntaxKind.SingleLineCommentTrivia)
            || t.IsKind(SyntaxKind.MultiLineCommentTrivia)
            || t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
            || t.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia))
            return true;

        if (t.IsKind(SyntaxKind.RegionDirectiveTrivia) || t.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
            return true;

        return false;
    }
}
