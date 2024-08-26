using BSPParser;

FileInfo bspFile = new FileInfo(args[0]);
DirectoryInfo? gameDirectory = bspFile.Directory?.Parent?.Parent;
if (gameDirectory == null) {
    throw new Exception("Map isn't in a directory that makes sense! Make sure it's a map from a freshly unzipped sven coop map database. Isolated from the game and other unzipped maps.");
}

DirectoryInfo? check = new DirectoryInfo(Path.Combine(gameDirectory.FullName, "svencoop"));
if (check is { Exists: true }) {
    throw new Exception("Please don't run this within your game directory. This is intended to be ran on unzipped map files from sven map database. To post-process them.");
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
