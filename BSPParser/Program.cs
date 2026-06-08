using System.Collections;
using BSPParser;

IEnumerable<FileInfo> GetMaps(bool allowGameFolder, string path) {
    DirectoryInfo addonDirectory = new DirectoryInfo(path);
    if (!addonDirectory.Exists) {
        throw new Exception($"Can't read directory {path}. Please input directory of freshly unzipped, isolated addon to sven coop. eg: ./mycoolmappack (which contains maps/ models/ etc)");
    }

    if (!allowGameFolder) {
        if (Directory.Exists(Path.Combine( addonDirectory.Parent?.FullName ?? throw new InvalidOperationException( "Don't run this on a root directory please, or maybe I don't have enough permission to see up a dir?"), "svencoop"))) {
            throw new Exception("Please only run this utility on uninstalled map packs. It's designed within limitations that we cannot truly build a full dependency graph.");
        }
    }

    DirectoryInfo mapDirectory = new DirectoryInfo(Path.Combine(addonDirectory.FullName, "maps"));
    if (!mapDirectory.Exists) {
        throw new Exception($"Found no bsp files within {mapDirectory.FullName}");
    }

    foreach (var file in mapDirectory.GetFiles()) {
        if (!file.Name.EndsWith(".bsp")) {
            continue;
        }
        yield return file;
    }
}

if (args.Length > 0) {
    foreach (var arg in args) {
        if (arg == "-h" || arg == "--help" || arg == "/?") {
            Console.WriteLine("""
                              BSPParser

                              This application will scan a downloaded map pack and fix it up for linux servers for deployment.
                              
                              
                              As a deployment post-process on downloaded maps, to correct res files to work on a linux server:
                              Warning, this will overwrite files in the provided folder, it will rename and update res files to match descriptions in the provided within the BSP.
                              Usage:  
                                  BSPParser <directory of unzipped addon's svencoop_addon folder (the one containing the folders maps, sound, models, etc)>
                              Example:
                                  BSPParser "~/Downloads/D E A T H - W I S H/svencoop_addon/"
                              
                              
                              As a mapcycle list generator, to create a linux-compatible map list that only contains the starts of campaigns with correct casing:
                              Usage:
                                  BSPParser --cycle <svencoop_addon directory>
                              Example:
                                  BSPParser --cycle "~/Sven Coop/svencoop_addon/" > mapcycle.txt
                                  
                              As a mapvote list generator, to create a linux-compatible map list that only contains the starts of campaigns with correct casing:
                              Usage:
                                  BSPParser --vote <svencoop_addon directory>
                              Example:
                                  BSPParser --vote "~/Sven Coop/svencoop_addon/" > mapvote.cfg
                              
                              """);
            return;
        }
    }
}

// Generate map cycle for existing maps
if (args.Length == 2) {
    bool foundCycleFlag = false;
    bool foundVoteFlag = false;
    var path = "";
    foreach (var arg in args) {
        if (arg == "-c" || arg == "--cycle") {
            foundCycleFlag = true;
        } else if (arg == "--vote") {
            foundVoteFlag = true;
        } else {
            path = arg;
        }
    }
    if ((!foundCycleFlag && !foundVoteFlag) || (foundCycleFlag && foundVoteFlag)) {
        throw new Exception("Invalid number of arguments, use -h or --help for help.");
    }
    BSPChangeLevelTree changeLevelTree = new BSPChangeLevelTree(GetMaps(true, path));
    if (foundCycleFlag) {
        Console.WriteLine(changeLevelTree.GetMapCycleString());
    } else if (foundVoteFlag) {
        Console.WriteLine(changeLevelTree.GetMapVoteString());
    } else {
        throw new Exception("Invalid number of arguments, use -h or --help for help.");
    }
}


// Scary in-place fixup of random map downloaded from sven coop map database
if (args.Length == 1) {
    foreach (var map in GetMaps(false, args[0])) {
        Console.WriteLine($"{map.Name}:");
        BSP bsp = new BSP(map.FullName);
        bsp.FixResourcesInPlace();
    }
}
