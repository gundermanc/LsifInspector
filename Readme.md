# LSIF Inspector

![Application Screenshot](assets/LsifInspector.gif)

LSIF inspector is a trivial, hacky WPF application for browsing files in Microsoft's [Language Server Index Format](https://microsoft.github.io/language-server-protocol/overviews/lsif/overview/).

## Capabilities
- File > Open arbitrary LSIF files.
- Indent LSIF content by project and document begin and end `$event` scopes.
- Highlight edges and vertexes in different colors to help distinguish them.
- Ctrl+F / Find to search the left pane text.
- Display incoming/outgoing edges/vertexes for edge/vertex in the right pane.
- Click left or right pane to traverse the graph by stepping to the current edge or vertex' direct neighbor.

## Known issues
- Code quality - prototype quality. Could be better.
- UI thread - everything is done on the UI thread. Haven't taken the time to optimize.
- Performance and Scalability - Only really works with small LSIF samples indexing only a small number of projects.

## Change Log
4/28/2023 - Initial prototype version.