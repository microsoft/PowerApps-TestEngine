using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace OpenAITestGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.DataContext = new ViewModels.ViewModel();
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var clients = Model.Model.ClientPool;
            var conf = Model.Model.GetModel().McpServerConfigurationCollection;

            var allFunctions = await clients.GetAllAIFunctionsAsync();

            var viewModel = (ViewModels.ViewModel)this.DataContext;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            foreach (var function in allFunctions)
            {

                var schemaString = "";
                if (function is MCPSharp.MCPFunction f)
                {
                    JsonElement json = f.JsonSchema;

                    if (json.TryGetProperty("properties", out JsonElement propertiesElement))
                    {
                        schemaString = JsonSerializer.Serialize(propertiesElement, options);
                    }
                }
                viewModel.Functions.Add($"{function.Name}: {function.Description}\n{schemaString}");
            }
        }

    }
}
