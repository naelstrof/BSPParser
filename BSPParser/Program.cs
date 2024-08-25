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
BSPResources original_resources = bsp.GetResourceFile();
BSPResources malformed_resources = bsp.GetMalformedResources();

Console.WriteLine("Assets the user definitely typo'd the casing on (messes up FastDL):");
foreach (var pair in malformed_resources) {
    Console.WriteLine($"\t{pair.Value}");
}

Console.WriteLine("Assets the user definitely forgot to include:");
foreach (var resource in generated_resources.Where((a) => !original_resources.ContainsKey(a.Key) && File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName, a.Key)))) {
    Console.WriteLine($"\t{resource.Value}");
}

Console.WriteLine("User included resources that we missed (possibly referred to by script):");
foreach (var resource in original_resources.Where((a) => !generated_resources.ContainsKey(a.Key) && !malformed_resources.ContainsKey(a.Key) && File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName,a.Key)))) {
    Console.WriteLine($"\t{resource.Value}");
}
Console.WriteLine("User included resources that must be included in a wad, and probably should be removed:");
foreach (var resource in original_resources.Where((a) => !generated_resources.ContainsKey(a.Key) && !malformed_resources.ContainsKey(a.Key) && !File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName, a.Key)))) {
    Console.WriteLine($"\t{resource.Value}");
}
