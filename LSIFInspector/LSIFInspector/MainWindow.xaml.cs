namespace LSIFInspector
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;
    using ICSharpCode.AvalonEdit;
    using Microsoft.Win32;
    using static LSIFInspector.LsifGraph;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TextEditor textEditor;

        private bool loading;
        private LsifGraph? graph;

        public MainWindow()
        {
            InitializeComponent();

            this.textEditor = new TextEditor()
            {
                FontSize = 15,
                IsReadOnly = true,
                ShowLineNumbers = true,
            };

            this.textEditor.TextArea.SelectionChanged += this.OnSelectionChanged;
            this.textEditor.TextArea.Caret.PositionChanged += this.OnSelectionChanged;

            this.LSIFTextContainer.Content = this.textEditor;

            this.KeyUp += OnKeyUp;
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
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

                this.graph = LsifGraph.FromLines(lines);

                try
                {
                    this.loading = true;

                    this.textEditor.Clear();

                    foreach (var line in this.graph.LineStrings)
                    {
                        this.textEditor.AppendText(line);
                        this.textEditor.AppendText(Environment.NewLine);
                    }
                }
                finally
                {
                    this.loading = false;
                }
            }
        }

        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            // Ignore caret changes until loaded.
            if (this.loading is true)
            {
                return;
            }

            var selectionStart = this.textEditor.SelectionStart;

            if (selectionStart < 0 ||
                this.graph is null)
            {
                return;
            }

            var selectionStartLine = this.graph.GetLineIndexFromCharacterIndex(selectionStart);

            if (selectionStartLine < 0)
            {
                return;
            }

            var selectionStartLineStart = this.graph.GetCharacterIndexFromLineIndex(selectionStartLine);
            var selectionStartLineLength = this.graph.GetLineLength(selectionStartLine);

            // On click, adjust the selection to be the length of the line, for visibility.
            if (this.textEditor.SelectionStart != selectionStartLineStart)
            {
                this.textEditor.SelectionStart = selectionStartLineStart;
                return;
            }

            if (this.textEditor.SelectionLength != selectionStartLineLength)
            {
                this.textEditor.SelectionLength = selectionStartLineLength;
                this.textEditor.ScrollTo(selectionStartLine, column: 0);
                this.textEditor.ScrollToHorizontalOffset(0);
                return;
            }

            var lineText = this.graph.GetLineText(selectionStartLine);

            if (lineText is null)
            {
                return;
            }

            var deserializedLine = JsonSerializer.Deserialize<EdgeOrVertex>(lineText);

            this.Preview.Children.Clear();

            if (deserializedLine?.type == "edge")
            {
                this.textEditor.TextArea.SelectionBrush = Brushes.Green;

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
                this.textEditor.TextArea.SelectionBrush = Brushes.Blue;

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
                var textBlock = new TextBlock() { FontSize = 15, Text = this.graph.GetLineText(item.lineNumber!.Value), HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left };

                textBlock.MouseUp += (sender, e) =>
                {
                    var characterIndex = this.graph.GetCharacterIndexFromLineIndex(item.lineNumber.Value);
                    var lineLength = this.graph.GetLineLength(item.lineNumber.Value);

                    this.textEditor.Focus();
                    this.textEditor.SelectionStart = characterIndex;
                    this.textEditor.SelectionLength = lineLength;
                    this.textEditor.ScrollToLine(item.lineNumber!.Value);
                };

                this.Preview.Children.Add(textBlock);
            }
        }

        private void OnFind(object sender, RoutedEventArgs e)
        {
            var findWindow = new FindWindow();

            if (findWindow.ShowDialog() is true)
            {
                var startIndex = this.textEditor.SelectionStart + this.textEditor.SelectionLength;
                var matchIndex = this.textEditor.Document.IndexOf(
                    findWindow.FindText.Text,
                    startIndex,
                    this.textEditor.Document.TextLength - startIndex,
                    StringComparison.OrdinalIgnoreCase);

                this.textEditor.Focus();

                if (matchIndex > -1)
                {
                    this.textEditor.CaretOffset = matchIndex;
                }
                else
                {
                    this.textEditor.SelectionStart = 0;
                    this.textEditor.ScrollToLine(0);
                }
            }
        }

        private void OnFileStatsClicked(object sender, RoutedEventArgs e)
        {
            // Ensure document is open.
            if (this.graph is null)
            {
                MessageBox.Show("Must open document first");
                return;
            }

            // Count types of each vertex.
            Dictionary<string, int> vertexTypeCounts = new();
            foreach (var vertex in this.graph.VerticiesById.Values)
            {
                if (!vertexTypeCounts.TryGetValue(vertex.label, out var value))
                {
                    vertexTypeCounts[vertex.label] = 1;
                }
                else
                {
                    vertexTypeCounts[vertex.label] = value + 1;
                }
            }

            // Count types of each edge.
            Dictionary<string, int> edgeTypeCounts = new();
            foreach (var vertex in this.graph.EdgesById.Values)
            {
                if (!edgeTypeCounts.TryGetValue(vertex.label, out var value))
                {
                    edgeTypeCounts[vertex.label] = 1;
                }
                else
                {
                    edgeTypeCounts[vertex.label] = value + 1;
                }
            }

            var output = $"Total lines: {this.graph.Lines.Count}\n" +
                $"Verticies: {this.graph.VerticiesById.Count} ({(float)this.graph.VerticiesById.Count / this.graph.Lines.Count * 100}%)\n" +
                $"Edges: {this.graph.EdgesById.Count} ({(float)this.graph.EdgesById.Count / this.graph.Lines.Count * 100}%)" +
                $"\n\n---------------------\n";

            foreach (var count in vertexTypeCounts.OrderByDescending(pair => pair.Value))
            {
                output += $"{count.Key} Vertices: {count.Value} ({(float)count.Value / this.graph.Lines.Count * 100}%) \n";
            }

            output += $"\n---------------------\n\n";

            foreach (var count in edgeTypeCounts.OrderByDescending(pair => pair.Value))
            {
                output += $"{count.Key} Edges: {count.Value} ({(float)count.Value / this.graph.Lines.Count * 100}%) \n";
            }

            MessageBox.Show(output);
        }
    }
}
