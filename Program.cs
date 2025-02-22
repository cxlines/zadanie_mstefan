using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

class Program
{
    static void Main()
    {
        string invalidfolder = "Issue: Invalid input. Please provide a valid folder path or JSON file.";
        string invalidperms = "Error: No write access to the directory. Please check your permissions.";
        string invalidaccess = "Error: Unauthorized access or filename missing. Please check your permissions and filename.";

        while (true)
        {
            Console.Write("Please provide a folder or a json with folder information: ");
            string input = Console.ReadLine().Trim();

            if (input.ToLower() == "exit")
            {
                break;
            }

            DirectoryData directoryInfo = null;

            if (Directory.Exists(input))
            {
                directoryInfo = GetDirectories(input);
            }
            else if (File.Exists(input) && Path.GetExtension(input).ToLower() == ".json")
            {
                directoryInfo = DeserializeJson(File.ReadAllText(input));
            }
            else
            {
                Console.WriteLine(invalidfolder);
                continue;
            }

            Console.WriteLine("\nExtensions found in folder: " + string.Join(", ", UniqueFileExt(directoryInfo)));

            Console.Write("Save to JSON? (y/n): ");
            string saveJson = Console.ReadLine().Trim().ToLower();
            if (saveJson == "y")
            {
                Console.Write("Please provide the JSON file location: ");
                string jsonFilePath = Console.ReadLine().Trim();

                try
                {
                    string directoryPath = Path.GetDirectoryName(jsonFilePath);
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    if (!WritePermissions(directoryPath))
                    {
                        Console.WriteLine(invalidperms);
                        continue;
                    }

                    using (FileStream fs = new FileStream(jsonFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.Write(SerializeToJson(directoryInfo));
                    }
                    Console.WriteLine($"\nInformation saved to: {jsonFilePath}\n");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine(invalidaccess);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }

    static bool WritePermissions(string folderPath)
    {
        try
        {
            string tempFile = Path.Combine(folderPath, Path.GetRandomFileName());
            using (FileStream fs = File.Create(tempFile, 1, FileOptions.DeleteOnClose)) { }
            return true;
        }
        catch
        {
            return false;
        }
    }

    static DirectoryData GetDirectories(string path)
    {
        var directoryData = new DirectoryData
        {
            Name = Path.GetFileName(path),
            Files = Directory.GetFiles(path).Select(f => new FileData
            {
                Name = Path.GetFileName(f),
                Extension = Path.GetExtension(f)
            }).ToList(),
            Subdirectories = Directory.GetDirectories(path).Select(d => GetDirectories(d)).ToList()
        };
        return directoryData;
    }

    static string SerializeToJson(DirectoryData directoryData)
    {
        return JsonConvert.SerializeObject(directoryData, Newtonsoft.Json.Formatting.Indented);
    }

    static DirectoryData DeserializeJson(string jsonContent)
    {
        return JsonConvert.DeserializeObject<DirectoryData>(jsonContent);
    }

    static HashSet<string> UniqueFileExt(DirectoryData directoryData)
    {
        var extensions = new HashSet<string>();
        void CollectExtensions(DirectoryData dir)
        {
            foreach (var file in dir.Files)
            {
                extensions.Add(file.Extension);
            }
            foreach (var subdir in dir.Subdirectories)
            {
                CollectExtensions(subdir);
            }
        }
        CollectExtensions(directoryData);
        return extensions;
    }
}

class DirectoryData
{
    public string Name { get; set; }
    public List<FileData> Files { get; set; } = new List<FileData>();
    public List<DirectoryData> Subdirectories { get; set; } = new List<DirectoryData>();
}

class FileData
{
    public string Name { get; set; }
    public string Extension { get; set; }
}

