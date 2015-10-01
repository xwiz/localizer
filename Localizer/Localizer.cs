using System;
using System.IO;
using System.Security;
using System.Xml.Serialization;

namespace Localizer
{
    /// <summary>
    /// Provides a model for localizing application
    /// </summary>
    public class Localizer
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public Localizer()
        {
            Language = "en";
        }

        /// <summary>
        /// Public constructor for Localizer with default language initialization
        /// </summary>
        /// <param name="lang">The language key to intialize</param>
        public Localizer(string lang)
        {
            Language = lang;
        }

        private static Localizer _localizer;

        internal static Localizer myLocalizer
        {
            set { _localizer = value; }
            get { return _localizer; }
        }

        /// <summary>
        /// Escapes a string to make it valid xml if needed
        /// </summary>
        /// <param name="s">The string to convert</param>
        /// <returns>The escaped string</returns>
        public static string EscapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            return !SecurityElement.IsValidText(s) ? SecurityElement.Escape(s) : s;
        }

        /// <summary>
        /// Returns the string in the original form if it was previously escaped
        /// </summary>
        /// <param name="s">The string to unescape</param>
        /// <returns>The unescaped string</returns>
        public static string UnEscapeXml(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            string output = s;
            output =
                output.Replace("&apos;", "'")
                      .Replace("&quot;", "\"")
                      .Replace("&gt;", ">")
                      .Replace("&lt;", "<")
                      .Replace("&amp;", "&");
            return output;
        }

        /// <summary>
        /// Contains the current language
        /// </summary>
        public static string Language;

        /// <summary>
        /// Serializes the language resources file
        /// </summary>
        /// <param name="locals">The language resource object to serialize</param>
        public void Serialize(Localizer locals)
        {
            try
            {
                var xs = new XmlSerializer(typeof(Localizer));
                using (TextWriter tw = new StreamWriter("locals.resources." + Language))
                {    
                    xs.Serialize(tw, locals);
                }
            }
            catch(Exception ex)
            {
                throw new Exception("An error occurred while trying to save the language resources. Please try again.", ex.InnerException);
            }
        }

        /// <summary>
        ///     De-Serializes the language resource file.
        /// </summary>
        /// <returns>The language resource object returned form the XML Deserialization</returns>
        public Localizer Deserialize()
        {
            if (!File.Exists("locals.resources."+Language)) return null;
            var xs = new XmlSerializer(typeof (Localizer));
            using (FileStream fs = File.Open("locals.resources."+Language, FileMode.Open))
            {
                return xs.Deserialize(fs) as Localizer;
            }
        }

        public string Form1buttonLoadTextLocalize;

        //Localize
        public string Form1buttonLoadTextLocalize1;
        //Globalizer
        public string Form1namespGlobalizer;
        //Error trying to load resource file. Message 
        public string Form1twError;
        //Your project has been localized successfully. Please verify that your project.
        public string Form1LocalizermyLocalizerYour;
        //Localization Cmplete
        public string Form1LocalizermyLocalizerLocalization;
        //Load Project File
        public string Form1buttonLoadTextLoad;

    }
}