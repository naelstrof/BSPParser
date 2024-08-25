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

HashSet<string> resources = new HashSet<string>();
var entities = bsp.GetEntities();
foreach (var ambient in entities.Where((ent) => ent["classname"] == "ambient_generic" && ent.ContainsKey("message"))) {
    resources.Add($"sound/{ambient["message"]}");
}

foreach (var ambientMusic in entities.Where((ent) => ent["classname"] == "ambient_music" && ent.ContainsKey("message"))) {
    resources.Add($"sound/{ambientMusic["message"]}");
}

foreach (var weaponAmmo in entities.Where((ent) => ent["classname"] == "weapon_custom_ammo" && ent.ContainsKey("w_model"))) {
    if (weaponAmmo["w_model"].StartsWith("*")) {
        continue;
    }
    resources.Add(weaponAmmo["w_model"]);
}

foreach (var weaponCustomSound in entities.Where((ent) => ent["classname"] == "weapon_custom_sound" && ent.ContainsKey("message"))) {
    resources.Add($"sound/{weaponCustomSound["message"]}");
}

foreach (var weaponCustomBullet in entities.Where((ent) => ent["classname"] == "weapon_custom_bullet" && ent.ContainsKey("sounds"))) {
    resources.Add($"sound/{weaponCustomBullet["sounds"]}");
}

foreach (var weapon in entities.Where((ent) => ent["classname"] == "weapon_custom")) {
    if (weapon.ContainsKey("sprite_directory") && weapon.ContainsKey("weapon_name")) {
        var spriteTextPath = $"sprites/{weapon["sprite_directory"]}/{weapon["weapon_name"]}.txt";
        resources.Add(spriteTextPath);
        foreach (var line in File.ReadLines(Path.Combine(gameDirectory.FullName, spriteTextPath))) {
            var splits = line.Split(null);
            int count = 0;
            foreach (var element in splits) {
                if (string.IsNullOrEmpty(element.Trim())) {
                    continue;
                }
                if (count++ == 2) {
                    resources.Add($"sprites/{element.Trim()}.spr");
                    break;
                }
            }
        }
    }

    if (weapon.TryGetValue("wpn_p_model", out var pmodel) && !pmodel.StartsWith("*")) {
        resources.Add(pmodel);
    }
    if (weapon.TryGetValue("wpn_v_model", out var vmodel) && !vmodel.StartsWith("*")) {
        resources.Add(vmodel);
    }
    if (weapon.TryGetValue("wpn_w_model", out var wmodel) && !wmodel.StartsWith("*")) {
        resources.Add(wmodel);
    }
}

foreach (var sprite in entities.Where((ent) => ent["classname"] == "env_sprite" && ent.ContainsKey("model"))) {
    resources.Add(sprite["model"]);
}

foreach (var item in entities.Where((ent) => ent["classname"] == "item_generic" && ent.ContainsKey("model"))) {
    var model = item["model"];
    if (model.StartsWith("*")) {
        continue;
    }
    resources.Add(model);
}

foreach (var breakable in entities.Where((ent) => ent["classname"] == "func_breakable" && ent.ContainsKey("gibmodel"))) {
    var model = breakable["gibmodel"];
    if (model.StartsWith("*")) {
        continue;
    }
    resources.Add(model);
}

foreach (var door in entities.Where((ent) => ent["classname"] == "func_door")) {
    if (door.ContainsKey("noise1")) {
        resources.Add($"sound/{door["noise1"]}");
    }
    if (door.ContainsKey("noise2")) {
        resources.Add($"sound/{door["noise2"]}");
    }
}

foreach (var camera in entities.Where((ent) => ent["classname"] == "trigger_camera" && ent.ContainsKey("cursor_sprite"))) {
    resources.Add(camera["cursor_sprite"]);
}

foreach (var squadmaker in entities.Where((ent) => ent["classname"] == "squadmaker" && ent.ContainsKey("new_model"))) {
    var model = squadmaker["new_model"];
    if (model.StartsWith("*")) {
        continue;
    }
    resources.Add(model);
}

foreach (var skychange in entities.Where((ent) => ent["classname"] == "trigger_changesky" && ent.ContainsKey("skyname"))) {
    var skyname = skychange["skyname"];
    resources.Add($"gfx/env/{skyname}bk.tga");
    resources.Add($"gfx/env/{skyname}bk.bmp");
    resources.Add($"gfx/env/{skyname}dn.tga");
    resources.Add($"gfx/env/{skyname}dn.bmp");
    resources.Add($"gfx/env/{skyname}ft.tga");
    resources.Add($"gfx/env/{skyname}ft.bmp");
    resources.Add($"gfx/env/{skyname}lf.tga");
    resources.Add($"gfx/env/{skyname}lf.bmp");
    resources.Add($"gfx/env/{skyname}rt.tga");
    resources.Add($"gfx/env/{skyname}rt.bmp");
    resources.Add($"gfx/env/{skyname}up.tga");
    resources.Add($"gfx/env/{skyname}up.bmp");
}

foreach (var funcTrain in entities.Where((ent) => ent["classname"] == "func_train" && ent.ContainsKey("noise"))) {
    resources.Add($"sound/{funcTrain["noise"]}");
}

foreach (var inventory in entities.Where((ent) => ent["classname"] == "item_inventory" && ent.ContainsKey("model"))) {
    resources.Add(inventory["model"]);
}

StringBuilder resourceFileBuilder = new StringBuilder();
foreach (var resource in resources) {
    if (string.IsNullOrEmpty(resource.Trim())) {
        continue;
    }
    resourceFileBuilder.Append($"{resource}\n");
}

HashSet<string> original_resources = new HashSet<string>();
foreach (var line in File.ReadLines(args[0].Substring(0, args[0].Length - 4) + ".res")) {
    if (string.IsNullOrEmpty(line.Trim())) {
        continue;
    }
    if (line.Trim().StartsWith("//")) {
        continue;
    }
    original_resources.Add(line.Trim());
}

foreach (var resource in original_resources) {
    if (resource.EndsWith(".wad")) {
        resources.Add(resource);
    }
}

Console.WriteLine("User included assets that we missed:");
foreach (var resource in original_resources.Where((a) => !resources.Contains(a))) {
    Console.WriteLine($"\t{resource}");
}

Console.WriteLine("Assets the user definitely typo'd the casing on (messes up FastDL):");
var assetsForgottenToBeIncluded = resources.Where((a) => !original_resources.Contains(a));
foreach (var check in assetsForgottenToBeIncluded) {
    if (File.Exists(Path.Combine(gameDirectory.FullName, check))) {
        continue;
    }
    var fileName = Path.GetFileName(check);
    var directory = Path.GetDirectoryName(check);
    if (directory == null) {
        continue;
    }
    var directoryInfo = new DirectoryInfo(Path.Combine(gameDirectory.FullName, directory));
    if (!directoryInfo.Exists) {
        continue;
    }
    foreach (var file in directoryInfo.GetFiles()) {
        if (file.Name.ToLowerInvariant() == fileName.ToLowerInvariant()) {
            Console.WriteLine($"\t{check}");
        }
    }
}

Console.WriteLine("Assets the user definitely forgot to include:");
foreach (var resource in resources.Where((a) => !original_resources.Contains(a) && File.Exists(Path.Combine(gameDirectory.FullName, a)))) {
    Console.WriteLine($"\t{resource}");
}