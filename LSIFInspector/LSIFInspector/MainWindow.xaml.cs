namespace LSIFInspector
{
    using System.IO;
    using System.Text.Json;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media;
    using Microsoft.Win32;
    using static LSIFInspector.LsifGraph;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private LsifGraph? graph;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OnFileOpenClicked(object sender, RoutedEventArgs e)
        {
            var openFile = new OpenFileDialog();

            if (openFile.ShowDialog() is true)
            {
                this.LSIFText.Text = File.ReadAllText(openFile.FileName);
                this.graph = LsifGraph.FromLines(File.ReadAllLines(openFile.FileName));
            }
        }

        private void OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            var selectionStart = this.LSIFText.SelectionStart;
            var selectionStartLine = this.LSIFText.GetLineIndexFromCharacterIndex(selectionStart);

            var selectionStartLineStart = this.LSIFText.GetCharacterIndexFromLineIndex(selectionStartLine);
            var selectionStartLineLength = this.LSIFText.GetLineLength(selectionStartLine);

            // On click, adjust the selection to be the length of the line, for visibility.
            if (this.LSIFText.SelectionStart != selectionStartLineStart)
            {
                this.LSIFText.SelectionStart = selectionStartLineStart;
                return;
            }

            if (this.LSIFText.SelectionLength != selectionStartLineLength)
            {
                this.LSIFText.SelectionLength = selectionStartLineLength;
                return;
            }

            if (selectionStart < 0 ||
                this.graph is null)
            {
                return;
            }

            var lineText = this.LSIFText.GetLineText(selectionStartLine);

            if (lineText is null)
            {
                return;
            }

            var deserializedLine = JsonSerializer.Deserialize<EdgeOrVertex>(lineText);

            this.Preview.Children.Clear();

            if (deserializedLine?.type == "edge")
            {
                this.LSIFText.SelectionBrush = Brushes.Green;

                // Add edge origin.
                this.PopulateItem(deserializedLine.outV);

                this.PopulateItem(deserializedLine.inV);

                if (deserializedLine.inVs is not null)
                {
                    foreach (var inV in deserializedLine.inVs)
                    {
                        this.PopulateItem(inV);
                    }
                }
            }
            else
            {
                this.LSIFText.SelectionBrush = Brushes.Blue;

                if (deserializedLine?.id is not null &&
                    this.graph.EdgesByOutVertexId.TryGetValue(deserializedLine.id.Value, out var departingEdges))
                {
                    foreach (var edge in departingEdges)
                    {
                        PopulateItem(edge.id);
                    }
                }

                if (deserializedLine?.id is not null &&
                    this.graph.EdgesByInVertexId.TryGetValue(deserializedLine.id.Value, out var arrivingEdges))
                {
                    foreach (var edge in arrivingEdges)
                    {
                        PopulateItem(edge.id);
                    }
                }
            }
        }

        private void PopulateItem(int? id)
        {
            if (id is not null &&
                (this.graph?.VerticiesById.TryGetValue(id.Value, out var item) is true || (this.graph?.EdgesById.TryGetValue(id.Value, out item) is true)))
            {
                var textBlock = new TextBlock() { Text = this.LSIFText.GetLineText(item.lineNumber!.Value) };

                textBlock.MouseUp += (sender, e) =>
                {
                    var characterIndex = this.LSIFText.GetCharacterIndexFromLineIndex(item.lineNumber.Value);
                    var lineLength = this.LSIFText.GetLineLength(item.lineNumber.Value);

                    this.LSIFText.SelectionStart = characterIndex;
                    this.LSIFText.SelectionLength = lineLength;
                };

                this.Preview.Children.Add(textBlock);
            }
        }
    }
}
