using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;

public class OutOfFolder : MonoBehaviour
{

    [MenuItem("MyMenu/Do Something %g")]
    static void DoSomething()
    {
        Debug.Log(Application.dataPath);
        int idx = 0;
        string root = Application.dataPath + "/Resources/SatelliteMap/Tiles_5";
        string[] dirs = Directory.GetDirectories(root);
        dirs = dirs.OrderBy(dir => Int32.Parse(Path.GetFileNameWithoutExtension(dir))).ToArray();
        foreach (string dir in dirs) { 
            string[] files = Directory.GetFiles(dir);
            files = files.Where(file => !file.Contains(".meta")).OrderBy(file => Int32.Parse(Path.GetFileNameWithoutExtension(file))).ToArray();
            foreach (string file in files) {
                Debug.Log(Path.GetFileNameWithoutExtension(file)); 
                Debug.Log(file);
                Debug.Log(root + "\\Tile_" + idx + ".png");
                File.Move(file, root + "\\Tile_" + idx + ".png");
                idx++;
            }
        }
    }
}
