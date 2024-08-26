using BSPParser;

if (args.Length == 0 || string.IsNullOrEmpty(args[0])) {
    throw new Exception($"Please input directory of freshly unzipped, isolated addon to sven coop. eg: ./mycoolmappack (which contains maps/ models/ etc)");
}
DirectoryInfo addonDirectory = new DirectoryInfo(args[0]);
if (!addonDirectory.Exists) {
    throw new Exception($"Can't read directory {args[0]}. Please input directory of freshly unzipped, isolated addon to sven coop. eg: ./mycoolmappack (which contains maps/ models/ etc)");
}

if (Directory.Exists(Path.Combine(addonDirectory.Parent?.FullName ?? throw new InvalidOperationException("Don't run this on a root directory please, or maybe I don't have enough permission to see up a dir?"), "svencoop"))) {
    throw new Exception("Please only run this utility on uninstalled map packs. It's designed within limitations that we cannot truely build a full dependency graph.");
}

DirectoryInfo mapDirectory = new DirectoryInfo(Path.Combine(addonDirectory.FullName, "maps"));
if (!mapDirectory.Exists) {
    throw new Exception($"Found no bsp files within {mapDirectory.FullName}");
}

foreach (var file in mapDirectory.GetFiles()) {
    if (!file.Name.EndsWith(".bsp")) {
        continue;
    }

    Console.WriteLine($"{file.FullName}:");
    BSP bsp = new BSP(file.FullName);

    BSPResources generated_resources = bsp.GetResources();
    BSPResources original_resources = bsp.GetResourceFile();
    BSPResources malformed_resources = bsp.GetMalformedResources();

    Console.WriteLine("Assets the user definitely typo'd the casing on (messes up FastDL):");
    foreach (var pair in malformed_resources) {
        Console.WriteLine($"\t{pair.Value}");
    }

    Console.WriteLine("Assets the user definitely forgot to include:");
    foreach (var resource in generated_resources.Where((a) =>
                 !original_resources.ContainsKey(a.Key) &&
                 File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName, a.Key)))) {
        Console.WriteLine($"\t{resource.Value}");
    }

    Console.WriteLine("User included resources that we missed (possibly referred to by script):");
    foreach (var resource in original_resources.Where((a) =>
                 !generated_resources.ContainsKey(a.Key) && !malformed_resources.ContainsKey(a.Key) &&
                 File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName, a.Key)))) {
        Console.WriteLine($"\t{resource.Value}");
    }

    Console.WriteLine("User included resources that must be included in a wad, and probably should be removed:");
    foreach (var resource in original_resources.Where((a) =>
                 !generated_resources.ContainsKey(a.Key) && !malformed_resources.ContainsKey(a.Key) &&
                 !File.Exists(Path.Combine(bsp.GetAddonDirectory().FullName, a.Key)))) {
        Console.WriteLine($"\t{resource.Value}");
    }
}
