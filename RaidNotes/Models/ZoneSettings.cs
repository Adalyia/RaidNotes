using System.Collections.Generic;

namespace RaidNotes.Models
{
    public class ZoneSettings(bool enabled)
    {
        public bool Enabled { get; set; } = enabled;

        public List<Note> Notes { get; set; } = [new Note()];

        public int SelectedNoteIndex { get; set; } = 0;

        public void addNote(Note s)
        {
            Notes.Add(s);
        }

        public void removeNoteAt(int i)
        {
            Notes.RemoveAt(i);
        }

        public void clearNotes()
        {
            Notes.Clear();
        }

        public override string ToString()
        {
            return $"Enabled: {Enabled}, Notes: {Notes.Count}";
        }
    }
}
