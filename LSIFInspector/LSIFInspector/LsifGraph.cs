﻿namespace LSIFInspector
{
    using System.Collections.Generic;
    using System;
    using System.Text.Json;
    using System.Linq;

    internal sealed class LsifGraph
    {
        public LsifGraph(
            List<Line> lines,
            Dictionary<int, EdgeOrVertex> verticiesById,
            Dictionary<int, EdgeOrVertex> edgesById,
            Dictionary<int, HashSet<EdgeOrVertex>> edgesByOutVertexId,
            Dictionary<int, HashSet<EdgeOrVertex>> edgesByInVertexId)
        {
            this.Lines = lines;
            this.VerticiesById = verticiesById;
            this.EdgesById = edgesById;
            this.EdgesByOutVertexId = edgesByOutVertexId;
            this.EdgesByInVertexId = edgesByInVertexId;
        }

        public static LsifGraph FromLines(IReadOnlyList<string> lines)
        {
            var verticiesById = new Dictionary<int, EdgeOrVertex>();
            var edgesById = new Dictionary<int, EdgeOrVertex>();
            var edgesByOutVertexId = new Dictionary<int, HashSet<EdgeOrVertex>>();
            var edgesByInVertexId = new Dictionary<int, HashSet<EdgeOrVertex>>();

            var linesWithMetadata = new List<Line>();
            var currentDocumentPosition = 0;
            int currentLine = 0;

            // Create a lookup table for nodes + edges.
            foreach (var line in IndentLines(lines))
            {
                linesWithMetadata.Add(new Line(line, currentDocumentPosition));
                currentDocumentPosition += line.Length + Environment.NewLine.Length;

                var edgeOrVertex = JsonSerializer.Deserialize<EdgeOrVertex>(line);

                edgeOrVertex = edgeOrVertex with { lineNumber = currentLine };

                if (edgeOrVertex is not null)
                {
                    if (edgeOrVertex.type == "vertex" &&
                        edgeOrVertex.id is not null)
                    {
                        verticiesById.Add(edgeOrVertex.id.Value, edgeOrVertex);
                    }
                    else if (edgeOrVertex.type == "edge")
                    {
                        var id = edgeOrVertex.id;
                        var outV = edgeOrVertex.outV;
                        var inV = edgeOrVertex.inV;
                        var inVs = edgeOrVertex.inVs;

                        if (id is not null)
                        {
                            edgesById.Add(id.Value, edgeOrVertex);
                        }

                        if (outV is not null)
                        {
                            if (!edgesByOutVertexId.TryGetValue(outV.Value, out var outVEdgesSet))
                            {
                                outVEdgesSet = edgesByOutVertexId[outV.Value] = new HashSet<EdgeOrVertex>();
                            }
                            outVEdgesSet.Add(edgeOrVertex);

                            if (inV is not null)
                            {
                                if (!edgesByInVertexId.TryGetValue(inV.Value, out var inVIdEdgesSet))
                                {
                                    inVIdEdgesSet = edgesByInVertexId[inV.Value] = new HashSet<EdgeOrVertex>();
                                }
                                inVIdEdgesSet.Add(edgeOrVertex);
                            }
                            else
                            {
                                foreach (var inV2 in inVs)
                                {
                                    if (!edgesByInVertexId.TryGetValue(inV2, out var inVIdEdgesSet))
                                    {
                                        inVIdEdgesSet = edgesByInVertexId[inV2] = new HashSet<EdgeOrVertex>();
                                    }
                                    inVIdEdgesSet.Add(edgeOrVertex);
                                }
                            }
                        }
                    }
                }

                currentLine++;
            }

            return new LsifGraph(linesWithMetadata, verticiesById, edgesById, edgesByOutVertexId, edgesByInVertexId);
        }

        public IReadOnlyList<Line> Lines { get; }

        public IEnumerable<string> LineStrings => this.Lines.Select(line => line.Text);

        public IReadOnlyDictionary<int, EdgeOrVertex> VerticiesById { get; }

        public IReadOnlyDictionary<int, EdgeOrVertex> EdgesById { get; }

        public IReadOnlyDictionary<int, HashSet<EdgeOrVertex>> EdgesByOutVertexId { get; }

        public IReadOnlyDictionary<int, HashSet<EdgeOrVertex>> EdgesByInVertexId { get; }

        public int GetLineIndexFromCharacterIndex(int index)
        {
            // TODO: stop being lazy and implement binary search.
            int i = 0;
            foreach (var line in this.Lines)
            {
                if (line.Contains(index))
                {
                    return i;
                }

                i++;
            }

            return -1;
        }

        public int GetCharacterIndexFromLineIndex(int lineIndex)
        {
            if (lineIndex >= this.Lines.Count)
            {
                return -1;
            }

            return this.Lines[lineIndex].Start;
        }

        public string GetLineText(int lineIndex)
        {
            if (lineIndex >= this.Lines.Count)
            {
                return string.Empty;
            }

            return this.Lines[lineIndex].Text;
        }

        public int GetLineLength(int lineIndex)
        {
            if (lineIndex >= this.Lines.Count)
            {
                return -1;
            }

            return this.Lines[lineIndex].Length;
        }

        private static IReadOnlyList<string> IndentLines(IEnumerable<string> lines)
        {
            var outputLines = new List<string>();

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

                outputLines.Add(new string(' ', indent) + line);
            }

            return outputLines;
        }

        public record Line(string Text, int Start)
        {
            public int Length => this.Text.Length;

            public bool Contains(int position) => position >= this.Start &&
                position < (this.Start + this.Length);
        }

        public record EdgeOrVertex(int? id, string type, string label, int? outV, int? inV, int[] inVs, Uri uri, Position start, Position end, string? identifier, string? name, int? lineNumber, string? kind);

        public record Position(int? line, int? character);
    }
}
