namespace TinyPdf;

public partial class TinyPdfCreate
{
    private class PdfObject
    {
        public int Id { get; set; }
        public Dictionary<string, object> Dict { get; set; } = new Dictionary<string, object>();
        public byte[]? Stream { get; set; }
    }
}
