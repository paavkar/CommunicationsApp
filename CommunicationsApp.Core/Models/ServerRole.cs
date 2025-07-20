using System.ComponentModel.DataAnnotations.Schema;

namespace CommunicationsApp.Core.Models
{
    public class ServerRole
    {
        public string? Id { get; set; }
        public string ServerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string HexColour { get; set; } = string.Empty;
        public int Hierarchy { get; set; }
        public bool DisplaySeparately { get; set; }

        [NotMapped]
        public List<ServerPermission> Permissions { get; set; } = [];

        public override string ToString()
        {
            return $"ServerRole [Id={Id ?? "null"}, ServerId={ServerId}, Name={Name}, HexColour={HexColour}," +
                $"Hierarchy={Hierarchy}, DisplaySeparately={DisplaySeparately}]";
        }
    }
}
