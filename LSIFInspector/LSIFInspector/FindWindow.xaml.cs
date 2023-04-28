namespace LSIFInspector
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for FindWindow.xaml
    /// </summary>
    public partial class FindWindow : Window
    {
        private static string previousSearch = string.Empty;

        public FindWindow()
        {
            InitializeComponent();

            this.FindText.Text = previousSearch;
            this.FindText.SelectionStart = previousSearch.Length;
            this.FindText.Focus();
        }

        private void OnClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            previousSearch = this.FindText.Text;
            this.Close();
        }
    }
}
