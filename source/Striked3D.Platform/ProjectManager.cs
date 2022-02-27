using Newtonsoft.Json;

namespace Striked3D.Platform
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct ProjectInfo
    {
        [JsonProperty]
        public string Name { get; set; }

        public string SystemPath { get; set; }
    }
    public static class ProjectManager
    {
        public const string projectExtension = ".projs3d";

        private static ProjectInfo _CurrentProject;
        public static ProjectInfo CurrentProject {
            get => _CurrentProject;
            set => _CurrentProject = value;
        }

        public static void CreateProject(string path, string projectName)
        {
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            var filePath = System.IO.Path.Combine(path, projectExtension);

            if (System.IO.File.Exists(filePath))
            {
                throw new System.Exception("Cant create project. Project file already exist.");
            }

            var info = new ProjectInfo { Name = projectName };
            string output = JsonConvert.SerializeObject(info);

            System.IO.File.WriteAllText(filePath, output);

        }
        public static void LoadProject(string path)
        {
            var filePath = System.IO.Path.Combine(path, projectExtension);

            if (!System.IO.File.Exists(filePath))
            {
                throw new System.Exception("Cant find project file.");
            }

            string input = System.IO.File.ReadAllText(filePath);
            var projectInput = JsonConvert.DeserializeObject<ProjectInfo>(input);
            projectInput.SystemPath = path;

            CurrentProject = projectInput;
        }
    }
}
