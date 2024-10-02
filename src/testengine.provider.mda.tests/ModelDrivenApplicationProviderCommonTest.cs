using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.PowerApps.TestEngine.Providers;

namespace Microsoft.PowerApps.TestEngine.Tests.PowerApps
{
    internal class Common
    {
        public static string MockJavaScript(string text, string pageType, bool includeMocks = true, bool includeInterface = true, List<string> interfaceResourceNames = null)
        {
            // Initialize the list with the default value if it is null
            interfaceResourceNames ??= new List<string> { "testengine.provider.mda.PowerAppsTestEngineMDA.js" };

            StringBuilder javaScript = new StringBuilder();

            Assembly assembly;
            string resourceName = "";

            if (includeMocks)
            {
                assembly = Assembly.GetExecutingAssembly();
                resourceName = "testengine.provider.mda.tests.ModelDrivenApplicationMock.js";
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string mock = reader.ReadToEnd();

                    javaScript.Append(mock + ";");
                }
            }

            if (includeInterface)
            {
                assembly = typeof(ModelDrivenApplicationProvider).Assembly;
                foreach (string name in interfaceResourceNames)
                {
                    using (Stream stream = assembly.GetManifestResourceStream(name))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        javaScript.Append(reader.ReadToEnd());

                        javaScript.Append(text + ";");
                    }
                }
            }
            javaScript.Append(text + ";");

            return javaScript.ToString();
        }
    }
}
