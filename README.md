# Sven Coop BSPParser

This is an extremely specialized application intended for sven coop server owners to post-process downloaded map packs to make them more compatible with their linux servers.

It very specifically reads a bsp directly to find all resources that should be included, looks at the existing .res file next to the bsp to compare them. Then does its best to correct mistakes and inconsistencies to fix a few issues I had with hosting a fastdl.

## Features

- Scans bsp files for usages of skyboxes, models, sentence files, sprites, model replacement files, and sound replacement files. Very similarly to [resguy](https://github.com/wootguy/resguy).
- Removes unnecessary usages of files that don't exist within the addon.
- Detects inappropriate casing of filenames and corrects them, so that fast-dl doesn't break.

## Limitations

Since this is so highly specialized, it's built within heavy limitations.

- Custom textures within .wads are ignored, and in-fact, all wads are included by assumption. I didn't want to write a bsp texture lump parser, or wad parser. Sorry! This objectively worsens the quality of correctly configured resource files. (Though improves utterly broken ones!)

## Showcase

After running it on the [Hazardous Course 2](http://scmapdb.wikidot.com/map:hazardous-course-2) map pack, it finds a skybox in a trigger on map `maps/hc2_b2b.bsp` and adds it to the resources file. Fixing an immersion break for clients where suddenly you are no longer in space.

![maps/hc2_b2b](https://github.com/user-attachments/assets/260bcd21-f62d-44d4-8688-aafe5d93417a)

## Compiling

```bash
#!/bin/bash

dotnet build ./BSPParser/ --configuration Release
```

## Usage

Lets say I wanted to install the [Wanted campaign](http://scmapdb.wikidot.com/map:wanted) from the sven coop map database. And I've extracted it to my Downloads directory, I could run BSPParser to touch up the .res files.
```bash
$ ./BSPParser ~/Downloads/wanted
want0.bsp:
        Adding: sound/wanted/weapons/knife_hit1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/knife_hit2.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/knife_miss1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/knife_hitbod1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/knife_hitbod2.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/knife_hitbod3.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/pistol_shot1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/grenade_hit1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/grenade_hit2.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/grenade_hit3.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/pistol_shot2.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/sbarrel1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/dbarrel1.wav: [File:wanted.gsr]
        Adding: sound/wanted/eagle/eagle_alert1.wav: [File:wanted.gsr]
        Adding: sound/wanted/eagle/eagle_alert2.wav: [File:wanted.gsr]
        Adding: sound/wanted/eagle/eagle_idle1.wav: [File:wanted.gsr]
        Adding: sound/wanted/eagle/eagle_idle2.wav: [File:wanted.gsr]
        Adding: sound/wanted/dyndave/spit.wav: [File:wanted.gsr]
        Adding: sound/wanted/ramone/ra_mgun2.wav: [File:wanted.gsr]
        Adding: sound/wanted/ramone/ra_mgun1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/gat_spindown.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/gat_spinup.wav: [File:wanted.gsr]
        Adding: sound/wanted/items/flashlight1.wav: [File:wanted.gsr]
want1.bsp:
        Adding: sound/wanted/weapons/pistol_shot1.wav: [Entity:ambient_generic, in: want1.bsp]
        Adding: sound/wanted/twnwest/zekesnore.wav: [Entity:ambient_generic, in: want1.bsp]
        Adding: sound/wanted/weapons/knife_hit1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/knife_hit2.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/knife_miss1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/knife_hitbod1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/knife_hitbod2.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/knife_hitbod3.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/grenade_hit1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/grenade_hit2.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/grenade_hit3.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/pistol_shot2.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/sbarrel1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/dbarrel1.wav: [File:wanted.gsr]
        Adding: sound/wanted/eagle/eagle_alert1.wav: [File:wanted.gsr]
        Adding: sound/wanted/eagle/eagle_alert2.wav: [File:wanted.gsr]
        Adding: sound/wanted/eagle/eagle_idle1.wav: [File:wanted.gsr]
        Adding: sound/wanted/eagle/eagle_idle2.wav: [File:wanted.gsr]
        Adding: sound/wanted/dyndave/spit.wav: [File:wanted.gsr]
        Adding: sound/wanted/ramone/ra_mgun2.wav: [File:wanted.gsr]
        Adding: sound/wanted/ramone/ra_mgun1.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/gat_spindown.wav: [File:wanted.gsr]
        Adding: sound/wanted/weapons/gat_spinup.wav: [File:wanted.gsr]
        Adding: sound/wanted/items/flashlight1.wav: [File:wanted.gsr]
        Adding: sound/wanted/twnwest/keystothebank.wav: [File:wanted_sentences.txt]
        Adding: sound/wanted/twnwest/fineday.wav: [File:wanted_sentences.txt]
        Adding: sound/wanted/twnwest/stagelate.wav: [File:wanted_sentences.txt]
        Adding: sound/wanted/crispen/telnorp.wav: [File:wanted_sentences.txt]
        Adding: sound/wanted/hoss/lookdrunk.wav: [File:wanted_sentences.txt]
        Adding: sound/wanted/hoss/dragdrunk.wav: [File:wanted_sentences.txt]
        Adding: sound/wanted/crispen/cstelegram.wav: [File:wanted_sentences.txt]
        Adding: sound/wanted/annie/script2.wav: [File:wanted_sentences.txt]
        Adding: sound/wanted/twnwest/possie.wav: [File:wanted_sentences.txt]
        Adding: sound/wanted/twnwest/breifboys.wav: [File:wanted_sentences.txt]
        Adding: sound/wanted/hoss/homeonerange.wav: [File:wanted_sentences.txt]
...
```
Here you can see it's found a missing snoring sound effect used in an ambient_generic entity on want1.bsp, a full set of weapon replacement sound effects, and a few "sentences".

The weapon replacement sound effects are overzealous as it might not be possible to recieve those weapons in the map. However, the sentences have direct references to being used within the bsp, and were almost certainly missing!

It does this for every map, and replaces the res file immediately (and destructively.)
Now I can install the map to the server with just a bit more confidence that it will transfer correctly over fastdl.
