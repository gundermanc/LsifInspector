namespace LSIFInspector
{
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
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

            this.KeyUp += OnKeyUp;
        }

        private void OnKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) &&
                e.Key == Key.F)
            {
                this.OnFind(null!, null!);
            }
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
                PopulateHeaderItem("Edge Goes from vertex: ---------------------------------");

                this.PopulateItem(deserializedLine.outV);

                PopulateHeaderItem("Edge Goes to verticies: --------------------------------");

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

                PopulateHeaderItem("Vertex has outgoing edges: -----------------------------");

                if (deserializedLine?.id is not null &&
                    this.graph.EdgesByOutVertexId.TryGetValue(deserializedLine.id.Value, out var departingEdges))
                {
                    foreach (var edge in departingEdges)
                    {
                        PopulateItem(edge.id);
                    }
                }

                PopulateHeaderItem("Vertex has incoming edges: -----------------------------");

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

        private void PopulateHeaderItem(string header)
        {
            var textBlock = new TextBlock() { FontSize = 20, Text = header, HorizontalAlignment = HorizontalAlignment.Left, FontWeight = FontWeight.FromOpenTypeWeight(100), TextAlignment = TextAlignment.Left };

            this.Preview.Children.Add(textBlock);
        }

        private void PopulateItem(int? id)
        {
            if (id is not null &&
                (this.graph?.VerticiesById.TryGetValue(id.Value, out var item) is true || (this.graph?.EdgesById.TryGetValue(id.Value, out item) is true)))
            {
                var textBlock = new TextBlock() { FontSize = 15, Text = this.LSIFText.GetLineText(item.lineNumber!.Value), HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left };

                textBlock.MouseUp += (sender, e) =>
                {
                    var characterIndex = this.LSIFText.GetCharacterIndexFromLineIndex(item.lineNumber.Value);
                    var lineLength = this.LSIFText.GetLineLength(item.lineNumber.Value);

                    this.LSIFText.SelectionStart = characterIndex;
                    this.LSIFText.SelectionLength = lineLength;
                    this.LSIFText.Focus();
                };

                this.Preview.Children.Add(textBlock);
            }
        }

        private void OnFind(object sender, RoutedEventArgs e)
        {
            var findWindow = new FindWindow();

            if (findWindow.ShowDialog() is true)
            {
                var matchIndex = this.LSIFText.Text.IndexOf(
                    findWindow.FindText.Text,
                    this.LSIFText.SelectionStart + this.LSIFText.SelectionLength);
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
