﻿namespace CommunicationsApp.Models
{
    public class ServerRole
    {
        public string? Id { get; set; }
        public string ServerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string HexColour { get; set; } = string.Empty;
        public int Hierarchy { get; set; }

        public override string ToString()
        {
            return $"ServerRole [Id={Id ?? "null"}, ServerId={ServerId}, Name={Name}]";
        }
    }
}
