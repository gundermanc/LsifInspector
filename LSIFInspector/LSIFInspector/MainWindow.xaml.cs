namespace LSIFInspector
{
    using System.IO;
    using System.Net;
    using System.Text;
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
                var lines = File.ReadAllLines(openFile.FileName);

                this.LSIFText.Text = PreprocessLsif(lines);
                this.graph = LsifGraph.FromLines(lines);
            }
        }

        private string PreprocessLsif(string[] lines)
        {
            var content = new StringBuilder();

            int indent = 0;

            foreach (var line in lines)
            {
                var deserializedLine = JsonSerializer.Deserialize<EdgeOrVertex>(line);

                if (deserializedLine?.label == "$event")
                {
                    if (deserializedLine.kind == "begin")
                    {
                        indent += 4;
                    }
                    else if (deserializedLine.kind == "end")
                    {
                        indent -= 4;
                    }
                }

                content.Append(new string(' ', indent));
                content.AppendLine(line);
            }

            return content.ToString();
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

            if (this.Preview.Children.Count is 0)
            {
                this.Preview.Children.Add(new TextBlock() { FontSize = 20, Text = "This item has no immediate neighbors" });
            }
        }

        private void PopulateItem(int? id)
        {
            if (id is not null &&
                (this.graph?.VerticiesById.TryGetValue(id.Value, out var item) is true || (this.graph?.EdgesById.TryGetValue(id.Value, out item) is true)))
            {
                var textBlock = new TextBlock() { FontSize = 20, Text = this.LSIFText.GetLineText(item.lineNumber!.Value), HorizontalAlignment = HorizontalAlignment.Left };

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

        private void OnFind(object sender, RoutedEventArgs e)
        {
            var findWindow = new FindWindow();

            if (findWindow.ShowDialog() is true)
            {
                var matchIndex = this.LSIFText.Text.IndexOf(findWindow.FindText.Text);
                if (matchIndex > -1)
                {
                    this.LSIFText.Focus();
                    this.LSIFText.SelectionStart = matchIndex;
                    this.LSIFText.ScrollToLine(this.LSIFText.GetLineIndexFromCharacterIndex(matchIndex));
                }
            }
        }
    }
}
