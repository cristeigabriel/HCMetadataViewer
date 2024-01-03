using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace HCMetadataViewer
{
  public class Argument
  {
    public string CArgument { get; set; }
    public List<string> PotentialValues { get; set; } = new List<string>();
  }

  public class ParsedTitle
  {
    public string? FunctionName = null;
    public List<Argument> Arguments = new List<Argument>();
    public string? ReturnType = null;
    public string? Library = null;
    public string? Dll = null;
    public string? Header = null;
    public string? MinVersionConsumer = null;
    public string? MinVersionServer = null;

    public string GenerateFunctionPrototype()
    {
      if (FunctionName == null)
      {
        return "<Cannot generate>";
      }

      var arguments = String.Join(", ", Arguments.Select(x => x.CArgument));
      var result = (ReturnType ?? "/* unknown */") + " " + FunctionName + "(" + arguments + ");";
      return result;
    }
  }

  public class TitleParser
  {
    public TitleParser(Title title)
    {
      this.title = title;
    }

    public ParsedTitle ParseTitle()
    {
      var result = new ParsedTitle();

      var metadata = title.metadata;
      var startLine = title.startLine;
      var endLine = title.endLine;
      for (int i = startLine + 1; i <= endLine; i++)
      {
        var line = new String(metadata[i]);
        line = line.TrimStart();

        // Metadata title variables
        var fun = "FUN=";
        var arg = "ARG=";
        var ret = "RET=";
        var lib = "LIB=";
        var dll = "DLL=";
        var hdr = "HDR=";
        var minc = "MINC=";
        var mins = "MINS=";
        var par = "PAR=";
        var values = "VALUES=";
        var val = "VAL=";

        if (line.StartsWith(fun))
        {
          line = line.Substring(fun.Length);
          if (!string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line))
          {
            result.FunctionName = line;
          }
        }
        else if (line.StartsWith(arg))
        {
          line = line.Substring(arg.Length);
          if (!string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line))
          {
            // Argument may end with ',', and if that's the case remove it
            if (line.EndsWith(','))
            {
              line = line.Substring(0, line.Length - 1);
            }

            result.Arguments.Add(new Argument { CArgument = line });
          }
        }
        else if (line.StartsWith(ret))
        {
          line = line.Substring(ret.Length);
          if (!string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line))
          {
            result.ReturnType = line;
          }
        }
        else if (line.StartsWith(lib))
        {
          line = line.Substring(lib.Length);
          if (!string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line))
          {
            result.Library = line;
          }
        }
        else if (line.StartsWith(dll))
        {
          line = line.Substring(dll.Length);
          if (!string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line))
          {
            result.Dll = line;
          }
        }
        else if (line.StartsWith(hdr))
        {
          line = line.Substring(hdr.Length);
          if (!string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line))
          {
            result.Header = line;
          }
        }
        else if (line.StartsWith(minc))
        {
          line = line.Substring(minc.Length);
          if (!string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line))
          {
            result.MinVersionConsumer = line;
          }
        }
        else if (line.StartsWith(mins))
        {
          line = line.Substring(mins.Length);
          if (!string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line))
          {
            result.MinVersionServer = line;
          }
        }
        else if (line.StartsWith(par))
        {
          line = line.Substring(par.Length).Split(' ')[0];

          foreach (var entry in result.Arguments)
          {
            // Check if the current parameter is the current argument
            var name = entry.CArgument.Split(' ').Last();
            if (name != line)
            {
              continue;
            }

            // Go next line
            i++;
            line = title.metadata[i].TrimStart();
            if (!string.IsNullOrEmpty(line) && !string.IsNullOrWhiteSpace(line))
            {
              // Check if the current parameter has values
              if (!line.StartsWith(values))
              {
                // Go back, and have this line be reinterpreted, as this parameter doesn't have values
                i--;
                break;
              }

              // Go next line
              i++;
              line = title.metadata[i].TrimStart();

              // While we have potential values
              while ((line = title.metadata[i].TrimStart()).StartsWith(val))
              {
                line = line.Substring(val.Length);

                // Add potential value to argument
                entry.PotentialValues.Add(line);

                i++;
              }
            }

            // We need to do it in either case, so
            i--;

            break;
          }
        }
      }

      return result;
    }

    public Title title;
  }

  public class Title
  {
    public Title(string[] metadata, string name, int startLine, int endLine)
    {
      this.metadata = metadata;
      this.name = name;
      this.startLine = startLine;
      this.endLine = endLine;
    }

    public string[] metadata;
    public string name;
    public int startLine;
    public int endLine;

    public override string ToString()
    {
      return name;
    }
  }

  internal class TitleDeclaration
  {
    public TitleDeclaration(int line, string name)
    {
      this.line = line;
      this.name = name;
    }

    public int line;
    public string name;
  }

  public class MetadataParser
  {
    public MetadataParser(byte[] resource)
    {
      // Read file
      this.metadata = System.Text.Encoding.UTF8.GetString(resource).
        Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

      // Store lines
      TitleDeclaration? previousTitleDecl = null;
      for (int i = 0; i < this.metadata.Length; i++)
      {
        string line = this.metadata[i];
        if (string.IsNullOrEmpty(line)) { continue; }

        // Means there's no spacing
        bool isStartLine = Char.IsLetter(line[0]);

        if (isStartLine)
        {
          if (previousTitleDecl != null)
          {
            var name = previousTitleDecl.name;
            var startLine = previousTitleDecl.line;
            var endLine = i - 1;
            if (startLine != endLine)
            {
              this.titles.Add(new Title(this.metadata, name, startLine, endLine));
            }
            previousTitleDecl = null;
          }

          // Check if line is title
          var stringBegin = "TITLE=";
          if (line.StartsWith(stringBegin))
          {
            previousTitleDecl = new TitleDeclaration(i, line.Substring(stringBegin.Length));
          }
        }
      }
    }

    private string[] metadata;
    public List<Title> titles = new List<Title>();
  }
}