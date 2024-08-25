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
