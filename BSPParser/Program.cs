// See https://aka.ms/new-console-template for more information

using System.Text;
using BSPParser;

Console.WriteLine("Hello, World!");

FileInfo bspFile = new FileInfo(args[0]);
DirectoryInfo? gameDirectory = bspFile.Directory?.Parent;
if (gameDirectory == null) {
    throw new Exception("Map isn't in a directory that makes sense! Please input a map either in a game folder, or freshly unzipped within a maps/ folder.");
}

BSP bsp = new BSP(args[0]);

BSPResources generated_resources = bsp.GetResources();
BSPResources original_resources = new BSPResources(args[0].Substring(0, args[0].Length - 4) + ".res");
foreach (var pair in original_resources) {
    if (pair.Key.EndsWith(".wad")) {
        generated_resources.Add(pair.Key, pair.Value);
    }
}

Console.WriteLine("Assets the user definitely typo'd the casing on (messes up FastDL):");
var assetsForgottenToBeIncluded = generated_resources.Where((a) => !original_resources.ContainsKey(a.Key));
HashSet<string> typoAssets = new HashSet<string>();
foreach (var check in assetsForgottenToBeIncluded) {
    if (File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName, check.Key))) {
        continue;
    }
    var fileName = Path.GetFileName(check.Key);
    var directory = Path.GetDirectoryName(check.Key);
    if (directory == null) {
        continue;
    }
    var directoryInfo = new DirectoryInfo(Path.Combine(bsp.GetAddonDirectory().FullName, directory));
    if (!directoryInfo.Exists) {
        continue;
    }
    foreach (var file in directoryInfo.GetFiles()) {
        if (file.Name.ToLowerInvariant() == fileName.ToLowerInvariant()) {
            typoAssets.Add(check.Key);
            Console.WriteLine($"\t{check.Value}");
        }
    }
}

Console.WriteLine("Assets the user definitely forgot to include:");
foreach (var resource in generated_resources.Where((a) => !original_resources.ContainsKey(a.Key) && File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName, a.Key)))) {
    Console.WriteLine($"\t{resource.Value}");
}

Console.WriteLine("User included resources that we missed (possibly referred to by script):");
foreach (var resource in original_resources.Where((a) => !generated_resources.ContainsKey(a.Key) && !typoAssets.Contains(a.Key) && File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName,a.Key)))) {
    Console.WriteLine($"\t{resource.Value}");
}
Console.WriteLine("User included resources that must be included in a wad, and were removed:");
foreach (var resource in original_resources.Where((a) => !generated_resources.ContainsKey(a.Key) && !typoAssets.Contains(a.Key) && !File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName, a.Key)))) {
    Console.WriteLine($"\t{resource.Value}");
}
