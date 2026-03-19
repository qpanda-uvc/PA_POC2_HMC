using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class CargoData
{
    public string cargoID;
    public string cargoName;
    public string POU;
    public float width;
    public float length;
    public float depth;
    public float warterVolume;
    public float weight;
    public List<string> SCCs = new List<string>();
    public string state;
}

public class DataManager : MonoBehaviour
{
    static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    static char[] TRIM_CHARS = { '\"' };
    static string LIBRARY_FILE_NAME = "LibraryExmaple";
    public List<CargoData> cargoDatas = new List<CargoData>();

    List<string> headers;
    public List<Dictionary<string, object>> csv_Data;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Init()
    {
        csv_Data = Read(LIBRARY_FILE_NAME, out headers);
    }

    public void GenerateCargoDatas()
    {
        CargoData tmpData = new CargoData();

        foreach(var item in csv_Data)
        {

        }

    }

    public List<Dictionary<string, object>> ReadCopy(string file, out List<string> _headers)
    {
        return Read("base", out _headers);
    }

    public static List<Dictionary<string, object>> Read(string file, out List<string> headers)
    {
        var list = new List<Dictionary<string, object>>();
        //TextAsset data = Resources.Load(file) as TextAsset;

        string FileContent = File.ReadAllText(Application.streamingAssetsPath + "/base600.csv");

        TextAsset data = new TextAsset(FileContent);


        headers = new List<string>();

        var lines = Regex.Split(data.text, LINE_SPLIT_RE);

        if (lines.Length <= 1)
        {
            headers = null;
            return list;
        }

        var header = Regex.Split(lines[0], SPLIT_RE);
        headers.AddRange(header);
        for (var i = 1; i < lines.Length; i++)
        {

            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            var entry = new Dictionary<string, object>();
            for (var j = 0; j < header.Length && j < values.Length; j++)
            {
                string value = values[j];
                value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\\", "");
                string finalvalue = value;
                
                entry[header[j]] = finalvalue;
            }
            list.Add(entry);
        }
        return list;
    }
}

