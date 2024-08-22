namespace RaidNotes.Models
{
    public class Note
    {
        public string Body { get; set; }
        public string Title { get; set; }

        public Note()
        {
            Body = string.Empty;
            Title = "Default";
        }

        public override string ToString()
        {
            return $"{Title}";
        }
    }
              
}
