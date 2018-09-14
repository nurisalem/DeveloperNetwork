using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace DeveloperNetwork.Aliases
{
    public class AliasReader
    {
        public AliasReader(ProgramConfiguration configuration)
        {
            // build file path
            var path = $"Aliases/{configuration.CleanProjectName}.yml";

            if (!File.Exists(path))
            {
                Aliases = null;
            }
            else
            {
                // read aliases
                var aliasesText = File.ReadAllText(path);
                var deserializer = new DeserializerBuilder().Build();
                Aliases = deserializer.Deserialize<Dictionary<string, List<string>>>(aliasesText);
            }

            // default it null
            Aliases = Aliases ?? new Dictionary<string, List<string>>();
        }

        public Dictionary<string, List<string>> Aliases { get; }
    }
}
