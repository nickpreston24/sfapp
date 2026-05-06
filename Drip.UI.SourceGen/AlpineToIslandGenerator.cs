using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DripUI.SourceGen
{
    [Generator]
    public sealed class AlpineToIslandGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // List EVERY file the generator can see (for debugging)
            var allFiles = context.AdditionalTextsProvider
                .Where(static f => true);

            context.RegisterSourceOutput(allFiles.Collect(), (spc, file_list) =>
            {
                var files = file_list
                    .Where(f =>
                        (f.Path.Contains("badge") ||
                         f.Path.Contains("accordion")
                        )
                        &&
                        !f.Path.Contains("example-")
                    )
                    // .Take(3)
                    .ToArray();

                var sb = new StringBuilder();
                sb.AppendLine("// ");
                sb.AppendLine($"// DripUI.SourceGen File Discovery - {files.Length} files seen");

                for (int index = 0; index < files.Length; index++)
                {
                    var file = files[index];
                    var fileName = Path.GetFileNameWithoutExtension(file.Path) ?? $"file{index}";
                    // sanitize filename to remove invalid chars
                    var safeName =
                        SafeIdentifier(
                            fileName); //string.Concat(fileName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));

                    var hintName = $"{safeName}.g.cs";

                    // todo: move this and the previous 2x lines to a CreateSafeName method, and add a boolean that toggles the hash.
                    // var safeName = string.Concat(fileName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
                    // var hintName = $"{safeName}_{ComputeHash(f.Path)}.g.cs";

                    // var content = new StringBuilder(sb.ToString());

                    string code = file.GetText()?.ToString() ?? string.Empty;
                    // var componentName = (Path.GetFileNameWithoutExtension(file.Path));
                    var componentName = hintName.Replace(".g.cs", "");

                    var island = new Island(componentName, hintName, code, file.Path);

                    string content = GenerateIsland(island);

                    sb.AppendLine($"// → hint: '{hintName}' | name: '{componentName}'");
                    spc.AddSource(hintName, SourceText.From(content, Encoding.UTF8));
                }

                spc.AddSource("DripUI_SourceGen_FileDiscovery.g.cs",
                    SourceText.From(sb.ToString(), Encoding.UTF8));
            });
        }


        static string ComputeHash(string input)
        {
            // Use UTF8 deterministically
            var bytes = Encoding.UTF8.GetBytes(input);

            // SHA256 then take first 8 bytes (64 bits) — low collision risk, short result
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(bytes);

            // Convert first 8 bytes to hex
            var sb = new StringBuilder(16);
            for (int i = 0; i < 8; i++)
                sb.Append(hash[i].ToString("x2"));

            return sb.ToString();
        }

        private record Island
        {
            public Island(string ComponentName, string HintName, string Html, string OriginalPath)
            {
                component_name = ComponentName;
                hintName = HintName;
                html = Html;
                originalPath = OriginalPath;
            }

            public string component_name { get; set; }
            public string hintName { get; set; }
            public string html { get; set; }
            public string originalPath { get; set; }

            public void Deconstruct(out string component_name, out string hintName, out string html,
                out string originalPath)
            {
                component_name = this.component_name;
                hintName = this.hintName;
                html = this.html;
                originalPath = this.originalPath;
            }
        }

        private static string GenerateIsland(Island island)
        {
            (string component_name, string hintName, string html, string originalPath) = island;

            var lower = component_name.ToLowerInvariant();

            var raw = RunFixes(html);

            Console.WriteLine($"============= \n {nameof(html)} :>> {html}");

            Console.WriteLine($"{nameof(raw)} :>> {raw}");

            // var raw =
            //     "<span class=\"rounded-full px-2.5 py-0.5 text-white text-xs font-semibold bg-black\">Badge</span>";


            // ensure no \u003C escapes:
            var opts = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };


            // var serialized = JsonSerializer.Serialize(raw);
            var serialized = JsonSerializer.Serialize(raw, opts); // yields: "\"<span class=\\\"...\\\">Badge</span>\""


            var escaped = raw.Replace("\"", "\"\"\"\""); // for verbatim @"..." literals

            var encoded = JsonSerializer.Serialize(raw); // returns "\"<div class=\\\"...\\\">...\""


            return $$"""
                     // <auto-generated />
                     // Source: {{originalPath}}

                     using Microsoft.AspNetCore.Razor.TagHelpers;
                     using Microsoft.AspNetCore.Html;
                     using System.Collections.Generic;

                     namespace Drip.UI
                     {
                         [HtmlTargetElement("{{lower}}")]
                         public class {{component_name}}Island : IslandTagHelper
                         {
                             public {{component_name}}Island()
                             {
                                 Event = IslandEvents.Intersect;   // default: load when scrolled into view
                                 Swap = IslandSwaps.OuterHTML;
                             }

                             public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
                             {
                                 //output.TagName = "div";



                                 // before anything
                     Console.WriteLine("[TAGHELPER] Before base:");
                     Console.WriteLine("  TagName: " + (output.TagName ?? "<null>"));
                     Console.WriteLine("  Attrs: " + string.Join(", ", output.Attributes.Select(a => $"{a.Name}={a.Value}")));
                     Console.WriteLine("  Content (current): '" + output.Content.GetContent() + "'");


                                 await base.ProcessAsync(context, output); // performs hx setup / default behavior


                     // after base
                     Console.WriteLine("[TAGHELPER] After base:");
                     Console.WriteLine("  TagName: " + (output.TagName ?? "<null>"));
                     Console.WriteLine("  Attrs: " + string.Join(", ", output.Attributes.Select(a => $"{a.Name}={a.Value}")));
                     Console.WriteLine("  Content (current): '" + output.Content.GetContent() + "'");


                                 output.TagName = null; // or "div" depending on desired wrapper
                                 output.TagMode = TagMode.StartTagAndEndTag;



                                 // Using serialized html technique
                                 output.Content.SetHtmlContent(new HtmlString({{serialized}}));



                             }
                         }
                     }
                     """;
        }


        // Set raw HTML content without further encoding
        // Using encoded html technique
        //output.Content.SetHtmlContent(new HtmlString({{encoded}}));

        // Using escaped html technique
        //output.Content.SetHtmlContent(new HtmlString(@"{{escaped}}"));


        private static string RunFixes(string html)
        {
            var array = html.AsArray();

            // html = EscapeForVerbatimString(html);

            Dictionary<string, string> map = new Dictionary<string, string>()
            {
                [@"\@(\w+)="] = "x-on:$1", // fixes the collision between alpine events and Razor's weird @ events.
                // [@""""] = @"\""",

                [@""""""] = "\"", // fixes quad-quotes "" → " and AVOIDS single quotes (again, razor is a butt about those).
            };

            html = array.ReplaceAll(map).Rollup().Trim();


            return html;
        }

        private static string GenerateHandlerStub(string name)
        {
            var lower = name.ToLowerInvariant();

            return $$"""
                     // <auto-generated />
                     // Minimal HTMX handler for {{name}}

                     using Microsoft.AspNetCore.Mvc;

                     namespace Drip.UI.Handlers
                     {
                         public class {{name}}Handler : Controller
                         {
                             [HttpGet("/{{lower}}")]
                             public IActionResult OnGet(string? viewname = null)
                             {
                                 return viewname is not null ? PartialView(viewname) : View();
                             }

                             [HttpPost("/{{lower}}")]
                             public IActionResult OnPost(string? value = null, string? viewname = null)
                             {
                                 // Self-updating / CRUD logic goes here
                                 return viewname is not null ? PartialView(viewname) : View();
                             }
                         }
                     }
                     """;
        }

        private static string SafeIdentifier(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Component";
            return new string(input.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        }

        private static string EscapeForVerbatimString(string s)
            => (s ?? string.Empty).Replace("\"", "\"\"");
    }
}


public static class QuickExts
{
    public static RegexOptions gmix = RegexOptions.Compiled |
                                      RegexOptions.Multiline |
                                      RegexOptions.IgnoreCase |
                                      RegexOptions.IgnorePatternWhitespace;

    /// <summary>
    /// Takes a dictionary full of Regex patterns (or words) and swaps those values with whatever you set as the .Value.
    ///
    /// <usage>
    /// So, for example, a dictionary like this:
    ///
    /// var replacements = new Dictionary<..>{ { "\d+", "hello there!"}, {"Order", "66"}  }
    ///
    /// ... and a text string like this:
    ///
    /// string text = "Order was valued at $100.00";
    /// var altered_text = text.ReplaceAll(replacements);
    ///
    /// Should look something like:
    ///
    /// `66 was valued at $hello there!.hello there!`
    ///
    /// This can be used to do quick (but not comprehensive) replacements to format things like:
    /// * Random Unicode chars you don't want
    /// * Extra spaces
    /// * Other garbage like CLRF
    ///
    /// It does have a flaw in that the more you replace things, the less reliable it can be, especially if your replacements replace OTHER replacements.  So, tread lightly...
    /// </usage>
    /// </summary>
    public static string[] ReplaceAll(
        this string[] lines,
        Dictionary<string, string> replacementMap,
        RegexOptions options = RegexOptions.None
    )
    {
        if (options == RegexOptions.None)
            options = gmix;

        Dictionary<string, string> map = replacementMap.Aggregate(
            new Dictionary<string, string>(),
            (modified, next) =>
            {
                // Sometimes in JSON \ have to be represented in unicode.  This reverts it.
                string fixedKey = next.Key
                    .Replace("%5C", @"\")
                    .Replace(@"\\", @"\");

                string fixedValue =
                    Regex.Replace(
                        next.Value,
                        @"\""",
                        "'"
                    );

                modified.Add(fixedKey, fixedValue);
                return modified;
            }
        );

        List<string> results = new List<string>();

        foreach (string line in lines)
        {
            string modified = line;
            foreach (KeyValuePair<string, string> replacement in map)
            {
                modified = Regex.Replace(
                    modified,
                    replacement.Key,
                    replacement.Value,
                    options
                );
            }

            results.Add(modified);
        }

        return results.ToArray();
    }

    public static string Rollup(this string[] lines_of_code)
    {
        return lines_of_code.Aggregate(
                new StringBuilder(),
                (sb, next) =>
                {
                    sb.AppendLine(next);
                    return sb;
                })
            .ToString();
    }

    public static T[] AsArray<T>(this T obj) => obj.Map(o => new List<T> { o }.ToArray());

    /// <summary>
    /// Map a Source class to a Target
    /// except now it's between two different classes
    /// </summary>
    public static TResult Map<TSource, TResult>(this TSource source, Func<TSource, TResult> map) =>
        map(source);
}
