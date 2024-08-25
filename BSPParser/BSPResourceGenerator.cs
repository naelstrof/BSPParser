using System.Text;

namespace BSPParser;

public class BSPResourceGenerator {
    private BSPResources resources = new();
    private BSP bsp;
    private DirectoryInfo addonDirectory;

    private void AddModel(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            if (ent[key].StartsWith("*")) {
                continue;
            }
            resources.Add(ent[key], new BSPResource(ent[key],ent));
        }
    }
    private void AddSound(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            var path = $"sound/{ent[key]}";
            resources.Add(path, new BSPResource(path, ent));
        }
    }
    private void AddSprite(string classname, string key) {
        foreach (var ent in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            resources.Add(ent[key], new BSPResource(ent[key], ent));
        }
    }
    
    private void CheckSkyboxAndAdd(string path, IResourceSource source) {
        if (File.Exists(Path.Combine(addonDirectory.FullName,path))) {
            resources.Add(path, new BSPResource(path, source));
        }
    }

    private void AddSkybox(string classname, string key) {
        foreach (var skychange in bsp.GetEntities().Where((ent) => ent["classname"] == classname && ent.ContainsKey(key))) {
            var skyname = skychange[key];
            CheckSkyboxAndAdd($"gfx/env/{skyname}bk.tga", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}bk.bmp", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}dn.tga", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}dn.bmp", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}ft.tga", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}ft.bmp", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}lf.tga", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}lf.bmp", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}rt.tga", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}rt.bmp", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}up.tga", skychange);
            CheckSkyboxAndAdd($"gfx/env/{skyname}up.bmp", skychange);
        }
    }
    
    public BSPResourceGenerator(BSP bsp, DirectoryInfo addonDirectory) {
        this.bsp = bsp;
        var entities = bsp.GetEntities();
        this.addonDirectory = addonDirectory;
        AddSound("ambient_generic", "message");
        AddSound("ambient_music", "message");
        AddModel("weapon_custom_ammo", "w_model");
        AddModel("custom_precache", "model_1");
        AddSound("weapon_custom_sound", "message");
        AddSound("weapon_custom_bullet", "sounds");
        AddSound("weapon_custom_bullet", "windup_snd");
        AddSound("weapon_custom_bullet", "wind_down_snd");
        foreach (var weapon in entities.Where((ent) => ent["classname"] == "weapon_custom")) {
            if (weapon.ContainsKey("sprite_directory") && weapon.TryGetValue("weapon_name", out var weaponName)) {
                var spriteTextPath = $"sprites/{weapon["sprite_directory"]}/{weaponName}.txt";
                resources.Add(spriteTextPath, new BSPResource(spriteTextPath, weapon));
                foreach (var line in File.ReadLines(Path.Combine(addonDirectory.FullName, spriteTextPath))) {
                    var splits = line.Split(null);
                    var count = 0;
                    foreach (var element in splits) {
                        if (string.IsNullOrEmpty(element.Trim())) {
                            continue;
                        }
                        if (count++ != 2) continue;
                        resources.Add($"sprites/{element.Trim()}.spr", new BSPResource($"sprites/{element.Trim()}.spr", weapon));
                        break;
                    }
                }
            }
            if (weapon.TryGetValue("wpn_p_model", out var pmodel) && !pmodel.StartsWith("*")) {
                resources.Add(pmodel, new BSPResource(pmodel, weapon));
            }
            if (weapon.TryGetValue("wpn_v_model", out var vmodel) && !vmodel.StartsWith("*")) {
                resources.Add(vmodel, new BSPResource(vmodel, weapon));
            }
            if (weapon.TryGetValue("wpn_w_model", out var wmodel) && !wmodel.StartsWith("*")) {
                resources.Add(wmodel, new BSPResource(wmodel, weapon));
            }
        }
        AddSprite("env_sprite", "model");
        AddModel("item_generic", "model");
        AddModel("func_breakable", "gibmodel");
        AddSound("func_door", "noise1");
        AddSound("func_door", "noise2");
        AddSprite("trigger_camera", "cursor_sprite");
        AddModel("squadmaker", "new_model");
        AddSkybox("trigger_changesky", "skyname");
        AddSound("func_train", "noise");
        AddModel("weapon_custom_projectile", "projectile_mdl");
        AddModel("item_inventory", "model");

        resources.Clean();

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

        Console.WriteLine("Assets the user definitely typo'd the casing on (messes up FastDL):");
        var assetsForgottenToBeIncluded = resources.Where((a) => !original_resources.Contains(a));
        HashSet<string> typoAssets = new HashSet<string>();
        foreach (var check in assetsForgottenToBeIncluded) {
            if (File.Exists(Path.Combine(addonDirectory.FullName, check))) {
                continue;
            }
            var fileName = Path.GetFileName(check);
            var directory = Path.GetDirectoryName(check);
            if (directory == null) {
                continue;
            }
            var directoryInfo = new DirectoryInfo(Path.Combine(addonDirectory.FullName, directory));
            if (!directoryInfo.Exists) {
                continue;
            }
            foreach (var file in directoryInfo.GetFiles()) {
                if (file.Name.ToLowerInvariant() == fileName.ToLowerInvariant()) {
                    typoAssets.Add(check);
                    Console.WriteLine($"\t{check}");
                }
            }
        }

        Console.WriteLine("Assets the user definitely forgot to include:");
        foreach (var resource in resources.Where((a) => !original_resources.Contains(a) && File.Exists(Path.Combine(addonDirectory.FullName, a)))) {
            Console.WriteLine($"\t{resource}");
        }

        Console.WriteLine("User included resources that we missed (possibly referred to by script):");
        foreach (var resource in original_resources.Where((a) => !resources.Contains(a) && !typoAssets.Contains(a) && File.Exists(Path.Combine(addonDirectory.FullName,a)))) {
            Console.WriteLine($"\t{resource}");
        }
        Console.WriteLine("User included resources that must be included in a wad, and were removed:");
        foreach (var resource in original_resources.Where((a) => !resources.Contains(a) && !typoAssets.Contains(a) && !File.Exists(Path.Combine(addonDirectory.FullName, a)))) {
            Console.WriteLine($"\t{resource}");
        }
    }
}