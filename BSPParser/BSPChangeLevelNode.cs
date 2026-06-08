using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BSPParser;

/// <summary>
/// Builds a dependency graph of the given bsp maps, then gives you all the root nodes for use in mapcycles and whatnot
/// </summary>
public class BSPChangeLevelTree {
    private List<BSPChangeLevelNode> nodes;
    
    public bool TryGetNode(FileInfo map, [NotNullWhen(true)] out BSPChangeLevelNode? node) {
        foreach (var search in nodes) {
            if (search.GetRepresentedBy(map)) {
                node = search;
                return true;
            }
        }
        node = null;
        return false;
    }

    public void AddNode(BSPChangeLevelNode node) {
        nodes.Add(node);
    }
    
    public BSPChangeLevelTree(IEnumerable<FileInfo> maps) {
        nodes = new List<BSPChangeLevelNode>();
        foreach (var map in maps) {
            if (TryGetNode(map, out var existingNode)) {
                continue;
            }
            var _ = new BSPChangeLevelNode(map, null, 0, this);
        }
    }
    
    public string GetMapCycleString() {
        StringBuilder builder = new StringBuilder();
        foreach (var node in nodes) {
            if (node.IsRootLevel()) {
                builder.AppendLine(node.GetMapName());
            }
        }
        return builder.ToString();
    }
    
    public string GetMapVoteString() {
        StringBuilder builder = new StringBuilder();
        foreach (var node in nodes) {
            if (node.IsRootLevel()) {
                builder.AppendLine($"addvotemap {node.GetMapName()}");
            }
        }
        return builder.ToString();
    }

    public override string ToString() {
        StringBuilder builder = new StringBuilder();
        foreach (var node in nodes) {
            builder.AppendLine(node.ToString());
        }
        return builder.ToString();
    }
}

public class BSPChangeLevelNode {
    private FileInfo bspFile;
    
    private List<BSPChangeLevelNode> parents;
    private List<BSPChangeLevelNode> children;
    private int depth;

    public bool GetRepresentedBy(FileInfo map) {
        return bspFile.FullName == map.FullName;
    }

    public bool IsRootLevel() {
        return parents.Count == 0 && bspFile.Exists;
    }
    
    public BSPChangeLevelNode(FileInfo map, BSPChangeLevelNode? parent, int depth, BSPChangeLevelTree tree) {
        bspFile = map;
        children = new List<BSPChangeLevelNode>();
        this.depth = depth;
        parents = new List<BSPChangeLevelNode>();
        if (parent != null) {
            parents.Add(parent);
        }
        if (!map.Exists) {
            Console.Error.WriteLine($"Missing map on disk, yet found in a level change trigger: {map.FullName}");
            return;
        }
        tree.AddNode(this);
        
        var bsp = new BSP(map.FullName);
        foreach (var exit in bsp.GetMapExits()) {
            var exitFileInfo = new FileInfo(Path.Combine(bsp.GetAddonDirectory().FullName, "maps", exit+".bsp"));
            if (tree.TryGetNode(exitFileInfo, out var existingNode)) {
                existingNode.parents.Add(this);
                children.Add(existingNode);
                continue;
            }
            var newNode = new BSPChangeLevelNode(exitFileInfo, this, depth + 1, tree);
            children.Add(newNode);
        }
    }

    public string GetMapName() {
        return Path.GetFileNameWithoutExtension(bspFile.FullName);
    }

    public override string ToString() {
        StringBuilder builder = new StringBuilder();
        builder.Append($"{{bsp:{bspFile.FullName}, children:[");
        foreach (var child in children) {
            builder.Append($"{child.GetMapName()}, ");
        }
        builder.Remove(builder.Length - 2, 2);
        builder.Append("], parents:[");
        foreach (var parent in parents) {
            builder.Append($"{parent.GetMapName()}, ");
        }
        builder.Remove(builder.Length - 2, 2);
        builder.Append($"], depth:{depth} }}");
        return builder.ToString();
    }
}
