using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Localizer
{
    /// <summary>
    ///     By Opata Chibueze. August 2013
    ///     Easily localize your Windows Forms apps
    ///     todo: Use an online translation tool to automatically translate extracted resource file output
    ///     todo: Correct progress bar for better syncing
    ///     todo: Develop better naming algorithm for variables
    ///     todo: Implement for multiple contstructors in forms
    /// </summary>
    public partial class Form1 : Form
    {
        private string directoryName;
        private Dictionary<string, string> duplicates;
        private Dictionary<string, List<string>> files;
        private Dictionary<string, string[]> found;
        private Dictionary<string, string> variables;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (buttonLoad.Text != Localizer.myLocalizer.Form1buttonLoadTextLocalize)
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    dataGridView1.Rows.Clear();
                    variables = new Dictionary<string, string>();
                    duplicates = new Dictionary<string, string>();
                    files = new Dictionary<string, List<string>>();
                    found = new Dictionary<string, string[]>();
                    directoryName = Path.GetDirectoryName(ofd.FileName);
                    if (directoryName == null) return;
                    //Let's process designer codes first if specified
                    if (checkBox1.Checked)
                    {
                        foreach (string s in Directory.EnumerateFiles(directoryName, "*.Designer.cs"))
                        {
                            var lines = new List<string>();
                            foreach (string line in File.ReadAllLines(s))
                            {
                                if (line.Contains(".Name")) continue; //ignore names
                                if (line.Contains("= \""))
                                {
                                    lines.Add(line.Trim());
                                }
                            }
                            string oFile = s.Replace("Designer.", "");
                            if (!File.Exists(oFile)) continue;
                            string formCode = File.ReadAllText(oFile);
                            if (formCode.Contains("InitializeUI()")) continue; //processed before, skip
                            int init = formCode.IndexOf("InitializeComponent();", StringComparison.Ordinal) + 22;
                            int endtag = formCode.IndexOf('}', init);
                            int begintag = formCode.IndexOf('{', init);
                            while (begintag != -1 && begintag < endtag)
                            {
                                begintag = formCode.IndexOf('{', begintag + 1);
                                endtag = formCode.IndexOf('}', endtag + 1);
                            }
                            formCode = formCode.Remove(endtag, 1);
                            File.WriteAllText(oFile,
                                              formCode.Insert(endtag, string.Format(@"    InitializeUI();
        }}

        private void InitializeUI()
        {{
            {0}
        }}", String.Join(Environment.NewLine + "            ", lines))));
                        }
                    }
                    foreach (string s in Directory.EnumerateFiles(directoryName, "*.cs"))
                    {
                        if (checkBox1.Checked && s.EndsWith("Designer.cs"))
                        {
                            //already processed ignore
                            continue;
                        }
                        string tempName = Path.GetFileNameWithoutExtension(s);
                        string allText = File.ReadAllText(s);
                        //extract strings
                        string[] splits = allText.Split('"');
                        found.Add(s, splits);
                        int last = 0;
                        for (int i = 1; i < splits.Length; i += 2)
                        {
                            try
                            {
                                string text = splits[i];
                                //we don't need to localize paths, links or empty strings
                                if (text.Trim() == String.Empty || text.Contains(":\\") || text.StartsWith("http:"))
                                    continue;
                                int fi = allText.IndexOf('"' + text + '"', last, StringComparison.Ordinal);
                                last = fi + text.Length;
                                int index = allText.LastIndexOf('=', fi);
                                int sindex = allText.LastIndexOf(" ", index, StringComparison.Ordinal);
                                switch (allText[fi - 1])
                                {
                                    case '\'':
                                        i -= 1; //account for fake trail
                                        continue; //this is probably a character, not a string. get out...
                                    case '@':
                                        text = "@" + text;
                                        allText = allText.Remove(fi - 1, 1);
                                        break;
                                }
                                //create variable name
                                var sb = new StringBuilder();
                                for (int t = sindex;; t--)
                                {
                                    if (Char.IsLetter(allText[t]))
                                    {
                                        sb.Append(allText[t]);
                                    }
                                    else
                                    {
                                        if (allText[t] == '/' || allText[t] == '<')
                                        {
                                            sb.Clear(); //comment zone, let's get out
                                            break;
                                        }
                                        if (allText[t] == '.')
                                        {
                                            continue;
                                        }
                                        if (sb.Length != 0)
                                            break;
                                    }
                                }
                                if (sb.Length == 0) continue;
                                char[] array = sb.ToString().Trim().ToCharArray();
                                Array.Reverse(array);
                                string varname = tempName + new string(array) +
                                                 text.Split(' ')[0].CleanString().Replace(".", "");
                                while (variables.ContainsKey(varname))
                                {
                                    varname += 1;
                                }
                                if (variables.ContainsValue(text))
                                {
                                    if (!duplicates.ContainsKey(text))
                                        duplicates.Add(text, variables.First(n => n.Value == text).Key);
                                }
                                variables.Add(varname, text);
                                if (files.ContainsKey(s))
                                {
                                    files[s].Add(varname);
                                }
                                else
                                {
                                    files.Add(s, new List<string>(new[] {varname}));
                                }
                                progressBar1.Value = Convert.ToInt32((i + 0.0D)/splits.Length*100);
                                //todo: Use text content as summary info for variable
                                //this makes it easier to work with localized strings
                                dataGridView1.Rows.Add(new object[] {varname, text});
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                //todo: anaylze possible causes
                            }
                        }
                    }
                    progressBar1.Value = 100;
                    buttonLoad.Text = Localizer.myLocalizer.Form1buttonLoadTextLocalize;
                }
            }
            else
            {
                if (!Directory.Exists(directoryName + ".backup"))
                {
                    Directory.CreateDirectory(directoryName + ".backup"); //let's backup first
                    foreach (string sf in Directory.EnumerateFiles(directoryName))
                    {
                        File.Copy(sf, directoryName + ".backup\\" + Path.GetFileName(sf));
                    }
                }
                string namesp = Localizer.myLocalizer.Form1namespGlobalizer;
                var document = new XmlDocument();
                var ns = new XmlNamespaceManager(document.NameTable);
                ns.AddNamespace("msbld", "http://schemas.microsoft.com/developer/msbuild/2003");
                document.Load(ofd.FileName);
                if (document.DocumentElement != null)
                {
                    XmlNode node = document.DocumentElement.SelectSingleNode("//msbld:RootNamespace", ns);
                    if (node != null)
                    {
                        namesp = node.InnerText;
                    }
                }
                variables.Clear();
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.Cells[0].Value == null || row.Cells[1].Value == null) continue;
                    variables.Add(row.Cells[0].Value.ToString(), row.Cells[1].Value.ToString());
                }
                foreach (var entry in files)
                {
                    string newText = File.ReadAllText(entry.Key);
                    foreach (string v in entry.Value)
                    {
                        if (variables.ContainsKey(v))
                        {
                            //replace text with variable name
                            newText = newText.Replace('"' + variables[v] + '"', "Localizer.myLocalizer." + v);
                        }
                    }
                    File.WriteAllText(entry.Key, newText);
                }

                #region Spoiler Write Localizer

                File.WriteAllText(directoryName + "\\Localizer.cs",
                                  string.Format(@"using System;
using System.IO;
using System.Xml.Serialization;

namespace {0}
{{
    /// <summary>
    /// Provides a model for localizing application
    /// </summary>
    public class Localizer
    {{
        /// <summary>
        /// Default constructor
        /// </summary>
        public Localizer()
        {{
            Language = ""en"";
        }}

        /// <summary>
        /// Public constructor for Localizer with default language initialization
        /// </summary>
        /// <param name=""lang"">The language key to intialize</param>
        public Localizer(string lang)
        {{
            Language = lang;
        }}

        private static Localizer _localizer;

        internal static Localizer myLocalizer
        {{
            set {{ _localizer = value; }}
            get {{ return _localizer; }}
        }}

        /// <summary>
        /// Contains the current language
        /// </summary>
        public static string Language;

        /// <summary>
        /// Serializes the language resources file
        /// </summary>
        /// <param name=""locals"">The language resource object to serialize</param>
        public void Serialize(Localizer locals)
        {{
            try
            {{
                var xs = new XmlSerializer(typeof(Localizer));
                using (TextWriter tw = new StreamWriter(""locals.resources."" + Language))
                {{    
                    xs.Serialize(tw, locals);
                }}
            }}
            catch(Exception ex)
            {{
                throw new Exception(""An error occurred while trying to save the language resources. Please try again."", ex.InnerException);
            }}
        }}

        /// <summary>
        /// Returns the string in the original form if it was previously escaped
        /// </summary>
        /// <param name=""s"">The string to unescape</param>
        /// <returns>The unescaped string</returns>
        public static string UnEscapeXml(string s)
        {{
            if (string.IsNullOrEmpty(s)) return s;

            string output = s;
            output =
                output.Replace(""&apos;"", ""'"")
                      .Replace(""&quot;"", ""\"""")
                      .Replace(""&gt;"", "">"")
                      .Replace(""&lt;"", ""<"")
                      .Replace(""&amp;"", ""&"");
            return output;
        }}

        /// <summary>
        ///     De-Serializes the language resource file.
        /// </summary>
        /// <returns>The language resource object returned form the XML Deserialization</returns>
        public Localizer Deserialize()
        {{
            if (!File.Exists(""locals.resources.""+Language)) return null;
            var xs = new XmlSerializer(typeof (Localizer));
            using (FileStream fs = File.Open(""locals.resources.""+Language, FileMode.Open))
            {{
                return xs.Deserialize(fs) as Localizer;
            }}
        }}
{1}
    }}
}}", namesp, ExpandVariables()));

                #endregion

                File.WriteAllText(directoryName + "\\locals.resources.en", @"<?xml version=""1.0"" encoding=""utf-8""?>
<Localizer xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">" +
                                                                           ExpandResources()
                                                                           + "</Localizer>");

                string allText = File.ReadAllText(ofd.FileName);
                File.WriteAllText(ofd.FileName,
                                  allText.Insert(allText.IndexOf("<Compile", StringComparison.Ordinal), @"
    <Compile Include=""Localizer.cs"" />
    <Content Include=""locals.resources.en"">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>"));
                //todo: Try detect actual file that contains entry point if not
                if (File.Exists(directoryName + "\\Program.cs"))
                {
                    string entry = File.ReadAllText(directoryName + "\\Program.cs");
                    int isert = entry.IndexOf("static void Main(", StringComparison.Ordinal);
                    File.WriteAllText(directoryName + "\\Program.cs",
                                      entry.Insert(entry.IndexOf("{", isert, StringComparison.Ordinal) + 1, @"
            Localizer.myLocalizer = new Localizer(""en"");
            Localizer.myLocalizer = Localizer.myLocalizer.Deserialize();
"));
                }
                MessageBox.Show(
                    Localizer.myLocalizer.Form1LocalizermyLocalizerYour,
                    Localizer.myLocalizer.Form1LocalizermyLocalizerLocalization, MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                buttonLoad.Text = Localizer.myLocalizer.Form1buttonLoadTextLoad;
                Process.Start("explorer.exe", directoryName);
            }
        }

        internal string ExpandVariables()
        {
            string output = "";
            foreach (var v in variables)
            {
                output += string.Format("{0}/// <summary>{2}{0}///Original: {3}{2}{0}///Returns unescaped _{1}{2}{0}/// </summary>{2}{0}[XmlIgnore]{2}{0}public string {1}",
                                        "        ", v.Key, Environment.NewLine, v.Value.EscapeXml());
                output += string.Format(@"
        {{
            get {{ return UnEscapeXml(_{0}); }}
        }}

        /// <summary>
        /// Serializer for {0}
        /// </summary>
        public string _{0};
" + Environment.NewLine, v.Key);
            }
            return output;
        }

        internal string ExpandResources()
        {
            string output = "";
            foreach (var v in variables)
            {
                output += string.Format(@"  <{0}>{1}</{0}>", v.Key, v.Value.EscapeXml()) +
                          Environment.NewLine;
            }
            return output;
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            string key = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
            variables[key] = dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString();
        }

        private void labelStatus_Click(object sender, EventArgs e)
        {
            buttonLoad.Text = Localizer.myLocalizer.Form1buttonLoadTextLoad;
        }
    }

    internal static class Utils
    {
        private static readonly bool[] BadCharValues;

        static Utils()
        {
            BadCharValues = new bool[char.MaxValue + 1];
            foreach (char c in @"[!@#$%^&~`(|)=+-<>?,._]*/\\;:{}'")
                BadCharValues[c] = true;
        }

        public static string CleanString(this string str)
        {
            var result = new StringBuilder(str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                if (!BadCharValues[str[i]])
                    result.Append(str[i]);
            }
            return result.ToString();
        }

        /// <summary>
        /// Escapes a string to make it valid xml if needed
        /// </summary>
        /// <param name="s">The string to convert</param>
        /// <returns>The escaped string</returns>
        public static string EscapeXml(this string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return SecurityElement.Escape(s);
        }
    }
}