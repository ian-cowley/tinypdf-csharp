using System.Buffers;
using System.IO.Compression;
using System.Text;

namespace TinyPdf;

public partial class TinyPdfCreate
{
    public partial class Builder
    {
        public bool Compress { get; set; } = true;
        private List<PdfObject> _objects = new List<PdfObject>();
        private List<Ref> _pages = new List<Ref>();
        private int _nextId = 1;

        public Ref AddObject(Dictionary<string, object> dict, byte[]? streamBytes = null)
        {
            var id = _nextId++;
            var obj = new PdfObject { Id = id, Dict = dict, Stream = streamBytes };
            _objects.Add(obj);
            return new Ref(id);
        }

        public void Page(double width, double height, Action<IPageContext> fn)
        {
            var writer = new ArrayBufferWriter<byte>(1024);
            var images = new List<(string Name, Ref Ref)>();
            var links = new List<(string Url, double[] Rect)>();
            int imageCount = 0;

            var ctx = new PageContextImpl(this, writer, images, imageCount, (newCount) => imageCount = newCount, links);
            fn(ctx);

            var contentBytes = writer.WrittenSpan.ToArray();
            var contentRef = AddObject(new Dictionary<string, object> { ["Length"] = contentBytes.Length }, contentBytes);

            var xobjects = new Dictionary<string, object>();
            foreach (var img in images) xobjects[img.Name[1..]] = img.Ref;

            var annots = new List<Ref>();
            foreach (var lnk in links)
            {
                annots.Add(AddObject(new Dictionary<string, object>
                {
                    ["Type"] = "/Annot",
                    ["Subtype"] = "/Link",
                    ["Rect"] = new List<double> { lnk.Rect[0], lnk.Rect[1], lnk.Rect[2], lnk.Rect[3] },
                    ["Border"] = new List<int> { 0, 0, 0 },
                    ["A"] = new Dictionary<string, object>
                    {
                        ["Type"] = "/Action",
                        ["S"] = "/URI",
                        ["URI"] = lnk.Url
                    }
                }));
            }

            var pageDict = new Dictionary<string, object>
            {
                ["Type"] = "/Page",
                ["Parent"] = null!, // Will be set in build
                ["MediaBox"] = new List<double> { 0, 0, width, height },
                ["Contents"] = contentRef,
                ["Resources"] = new Dictionary<string, object>
                {
                    ["Font"] = new Dictionary<string, object> { ["F1"] = null!, ["F2"] = null!, ["F3"] = null! },
                    ["XObject"] = xobjects.Count > 0 ? xobjects : null!
                }
            };
            if (annots.Count > 0) pageDict["Annots"] = annots;

            var pageRef = AddObject(pageDict);

            _pages.Add(pageRef);
        }

        public void Page(Action<IPageContext> fn) => Page(612, 792, fn);

        public double MeasureText(string str, double size) => TinyPdfCreate.MeasureText(str, size);

        public byte[] Build()
        {
            var fontMap = new Dictionary<PdfFont, Ref>();
            fontMap[PdfFont.Helvetica] = AddObject(new Dictionary<string, object> { ["Type"] = "/Font", ["Subtype"] = "/Type1", ["BaseFont"] = "/Helvetica" });
            fontMap[PdfFont.Times] = AddObject(new Dictionary<string, object> { ["Type"] = "/Font", ["Subtype"] = "/Type1", ["BaseFont"] = "/Times-Roman" });
            fontMap[PdfFont.Courier] = AddObject(new Dictionary<string, object> { ["Type"] = "/Font", ["Subtype"] = "/Type1", ["BaseFont"] = "/Courier" });

            var pagesRef = AddObject(new Dictionary<string, object> { ["Type"] = "/Pages", ["Kids"] = _pages, ["Count"] = _pages.Count });

            foreach (var obj in _objects)
            {
                if (obj.Dict.TryGetValue("Type", out var type) && type.ToString() == "/Page")
                {
                    obj.Dict["Parent"] = pagesRef;
                    if (obj.Dict.TryGetValue("Resources", out var resObj) && resObj is Dictionary<string, object> resources)
                    {
                        if (resources.TryGetValue("Font", out var fontObj) && fontObj is Dictionary<string, object> fonts)
                        {
                            fonts["F1"] = fontMap[PdfFont.Helvetica];
                            fonts["F2"] = fontMap[PdfFont.Times];
                            fonts["F3"] = fontMap[PdfFont.Courier];
                        }
                    }
                }
            }

            var catalogRef = AddObject(new Dictionary<string, object> { ["Type"] = "/Catalog", ["Pages"] = pagesRef });

            var offsets = new int[_objects.Count + 1];
            var headerBytes = new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-', (byte)'1', (byte)'.', (byte)'4', (byte)'\n', (byte)'%', 0xFF, 0xFF, 0xFF, 0xFF, (byte)'\n' };

            using var ms = new MemoryStream();
            ms.Write(headerBytes, 0, headerBytes.Length);

            foreach (var obj in _objects)
            {
                offsets[obj.Id] = (int)ms.Position;

                if (obj.Stream != null)
                {
                    byte[] streamData = obj.Stream;
                    PooledBufferWriter? pbw = null;
                    if (Compress)
                    {
                        pbw = new PooledBufferWriter();
                        using (var zs = new ZLibStream(new PooledBufferStream(pbw), CompressionLevel.Optimal, leaveOpen: true))
                        {
                            zs.Write(streamData, 0, streamData.Length);
                        }
                        int compressedLen = pbw.WrittenCount;
                        obj.Dict["Filter"] = "/FlateDecode";
                        obj.Dict["Length"] = compressedLen;
                    }
                    else obj.Dict["Length"] = streamData.Length;

                    var header = Encoding.UTF8.GetBytes($"{obj.Id} 0 obj\n");
                    ms.Write(header, 0, header.Length);

                    SerializeTo(ms, obj.Dict);

                    var streamMarker = Encoding.UTF8.GetBytes("\nstream\n");
                    ms.Write(streamMarker, 0, streamMarker.Length);

                    if (Compress && pbw is not null)
                    {
                        pbw.CopyTo(ms);
                        pbw.Dispose();
                    }
                    else ms.Write(streamData, 0, streamData.Length);

                    var suffixBytes = Encoding.UTF8.GetBytes("\nendstream\nendobj\n");
                    ms.Write(suffixBytes, 0, suffixBytes.Length);
                }
                else
                {
                    var header = Encoding.UTF8.GetBytes($"{obj.Id} 0 obj\n");
                    ms.Write(header, 0, header.Length);
                    SerializeTo(ms, obj.Dict);
                    var endBytes = Encoding.UTF8.GetBytes("\nendobj\n");
                    ms.Write(endBytes, 0, endBytes.Length);
                }
            }

            int xrefOffset = (int)ms.Position;

            var xrefHeader = Encoding.UTF8.GetBytes($"xref\n0 {_objects.Count + 1}\n0000000000 65535 f \n");
            ms.Write(xrefHeader, 0, xrefHeader.Length);
            for (int i = 1; i <= _objects.Count; i++)
            {
                var line = Encoding.UTF8.GetBytes($"{offsets[i]:D10} 00000 n \n");
                ms.Write(line, 0, line.Length);
            }

            var trailerHeader = Encoding.UTF8.GetBytes("trailer\n");
            ms.Write(trailerHeader, 0, trailerHeader.Length);
            SerializeTo(ms, new Dictionary<string, object> { ["Size"] = _objects.Count + 1, ["Root"] = catalogRef });
            ms.Write(Encoding.UTF8.GetBytes("\n"), 0, 1);

            var startxref = Encoding.UTF8.GetBytes($"startxref\n{xrefOffset}\n%%EOF\n");
            ms.Write(startxref, 0, startxref.Length);

            return ms.ToArray();
        }
    }
}
