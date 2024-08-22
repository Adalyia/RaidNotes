namespace RaidNotes.Models
{
    public class Zone(uint id, string name)
    {
        public uint Id { get; set; } = id;
        public string Name { get; set; } = name;

        public override string ToString()
        {
            return $"{Id} - {Name}";
        }
    }
}
