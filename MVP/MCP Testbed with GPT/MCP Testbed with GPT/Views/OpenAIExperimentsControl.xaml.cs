using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using OpenAITestGenerator;

namespace OpenAITestGenerator.Views
{
    /// <summary>
    /// Interaction logic for OpenAIExperimentsControl.xaml
    /// </summary>
    public partial class OpenAIExperimentsControl : UserControl
    {
        private PromptEvaluator promptEvaluator;

        public OpenAIExperimentsControl()
        {
            InitializeComponent();

            var model = Model.Model.GetModel();

            this.SystemPrompt.Text = model.SystemPrompt;
            this.promptEvaluator = new PromptEvaluator(model.SystemPrompt);

            this.SystemPrompt.Text = model.SystemPrompt;

            this.UserPrompt.Text = "";
            this.Result.Text = "";
        }

        private async void Generate_Click(object sender, RoutedEventArgs e)
        {
            this.ElapsedTime.Text = "Running...";
            this.Result.Text = "";

            var (result, duration) = await promptEvaluator.EvaluatePromptAsync(this.UserPrompt.Text);

            this.Result.Text = result;

            this.ElapsedTime.Text = duration.Duration().ToString();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            this.SystemPrompt.IsEnabled = true;

            //This is a good time to update the system prompt
            var model = Model.Model.GetModel();
            model.SystemPrompt = this.SystemPrompt.Text;

            promptEvaluator.NewChat(this.SystemPrompt.Text);
        }

        private static string StripMarkup(string text)
        {
            Regex r = new Regex("```.*");
            return r.Replace(text, "", count: 2);
        }

        private void Insert_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(StripMarkup(this.Result.Text));
        }
    }
}
