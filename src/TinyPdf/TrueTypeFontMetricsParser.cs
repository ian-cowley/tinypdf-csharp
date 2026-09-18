namespace TinyPdf;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;

/// <summary>
/// Authentic TrueType / OpenType font metrics table parser.
/// Parses SFNT directory and extracts typography metrics from 'head', 'hhea', 'hmtx', and 'cmap' tables.
/// </summary>
public sealed class TrueTypeFontMetricsParser
{
    private readonly byte[] _fontData;
    private readonly Dictionary<string, (int Offset, int Length)> _tables = new(StringComparer.Ordinal);

    public ushort UnitsPerEm { get; private set; } = 1000;
    public short Ascender { get; private set; }
    public short Descender { get; private set; }
    public short LineGap { get; private set; }
    public ushort NumberOfHMetrics { get; private set; }

    private int _cmapSubtableOffset = -1;
    private int _cmapFormat = -1;

    public TrueTypeFontMetricsParser(byte[] fontData)
    {
        _fontData = fontData ?? throw new ArgumentNullException(nameof(fontData));
        ParseFont();
    }

    public TrueTypeFontMetricsParser(ReadOnlySpan<byte> fontData)
    {
        _fontData = fontData.ToArray();
        ParseFont();
    }

    private void ParseFont()
    {
        if (_fontData.Length < 12)
            throw new FormatException("Font file is too small to be a valid SFNT font.");

        ushort numTables = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(4, 2));
        int offset = 12;

        for (int i = 0; i < numTables && offset + 16 <= _fontData.Length; i++)
        {
            string tag = System.Text.Encoding.ASCII.GetString(_fontData, offset, 4);
            int tableOffset = BinaryPrimitives.ReadInt32BigEndian(_fontData.AsSpan(offset + 8, 4));
            int tableLength = BinaryPrimitives.ReadInt32BigEndian(_fontData.AsSpan(offset + 12, 4));
            _tables[tag] = (tableOffset, tableLength);
            offset += 16;
        }

        ParseHeadTable();
        ParseHheaTable();
        ParseCmapTable();
    }

    private void ParseHeadTable()
    {
        if (!_tables.TryGetValue("head", out var entry) || entry.Offset + 54 > _fontData.Length)
            return;

        UnitsPerEm = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(entry.Offset + 18, 2));
        if (UnitsPerEm == 0) UnitsPerEm = 1000;
    }

    private void ParseHheaTable()
    {
        if (!_tables.TryGetValue("hhea", out var entry) || entry.Offset + 36 > _fontData.Length)
            return;

        Ascender = BinaryPrimitives.ReadInt16BigEndian(_fontData.AsSpan(entry.Offset + 4, 2));
        Descender = BinaryPrimitives.ReadInt16BigEndian(_fontData.AsSpan(entry.Offset + 6, 2));
        LineGap = BinaryPrimitives.ReadInt16BigEndian(_fontData.AsSpan(entry.Offset + 8, 2));
        NumberOfHMetrics = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(entry.Offset + 34, 2));
    }

    private void ParseCmapTable()
    {
        if (!_tables.TryGetValue("cmap", out var entry) || entry.Offset + 4 > _fontData.Length)
            return;

        int cmapStart = entry.Offset;
        ushort numSubtables = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(cmapStart + 2, 2));

        int bestOffset = -1;
        int bestFormat = -1;

        for (int i = 0; i < numSubtables; i++)
        {
            int recOffset = cmapStart + 4 + (i * 8);
            if (recOffset + 8 > _fontData.Length) break;

            ushort platformId = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(recOffset, 2));
            ushort encodingId = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(recOffset + 2, 2));
            int subtableOffset = cmapStart + BinaryPrimitives.ReadInt32BigEndian(_fontData.AsSpan(recOffset + 4, 4));

            if (subtableOffset + 2 > _fontData.Length) continue;
            ushort format = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(subtableOffset, 2));

            // Prefer Unicode platform (0) or Windows Unicode (3, 1 or 3, 10)
            if (format == 4 && (platformId == 0 || (platformId == 3 && encodingId == 1)))
            {
                bestOffset = subtableOffset;
                bestFormat = 4;
            }
            else if (format == 12 && (platformId == 0 || (platformId == 3 && encodingId == 10)))
            {
                bestOffset = subtableOffset;
                bestFormat = 12;
                break; // Format 12 is comprehensive 32-bit Unicode
            }
        }

        _cmapSubtableOffset = bestOffset;
        _cmapFormat = bestFormat;
    }

    public int GetGlyphIndex(int unicodeCodePoint)
    {
        if (_cmapSubtableOffset < 0 || _cmapSubtableOffset >= _fontData.Length)
            return 0;

        if (_cmapFormat == 4 && unicodeCodePoint <= 0xFFFF)
        {
            return LookupFormat4((ushort)unicodeCodePoint);
        }
        else if (_cmapFormat == 12)
        {
            return LookupFormat12((uint)unicodeCodePoint);
        }

        return 0;
    }

    private int LookupFormat4(ushort codePoint)
    {
        int offset = _cmapSubtableOffset;
        if (offset + 14 > _fontData.Length) return 0;

        ushort segCountX2 = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(offset + 6, 2));
        int segCount = segCountX2 / 2;

        int endCodeOffset = offset + 14;
        int startCodeOffset = endCodeOffset + segCountX2 + 2; // +2 for reservedPad
        int idDeltaOffset = startCodeOffset + segCountX2;
        int idRangeOffsetTable = idDeltaOffset + segCountX2;

        if (idRangeOffsetTable + segCountX2 > _fontData.Length) return 0;

        for (int i = 0; i < segCount; i++)
        {
            ushort endCode = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(endCodeOffset + (i * 2), 2));
            if (endCode >= codePoint)
            {
                ushort startCode = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(startCodeOffset + (i * 2), 2));
                if (startCode <= codePoint)
                {
                    short idDelta = BinaryPrimitives.ReadInt16BigEndian(_fontData.AsSpan(idDeltaOffset + (i * 2), 2));
                    int idRangeOffsetAddr = idRangeOffsetTable + (i * 2);
                    ushort idRangeOffset = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(idRangeOffsetAddr, 2));

                    if (idRangeOffset == 0)
                    {
                        return (ushort)((codePoint + idDelta) & 0xFFFF);
                    }
                    else
                    {
                        int glyphIndexAddr = idRangeOffsetAddr + idRangeOffset + ((codePoint - startCode) * 2);
                        if (glyphIndexAddr + 2 <= _fontData.Length)
                        {
                            ushort glyphIndex = BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(glyphIndexAddr, 2));
                            if (glyphIndex != 0)
                            {
                                return (ushort)((glyphIndex + idDelta) & 0xFFFF);
                            }
                        }
                    }
                }
                break;
            }
        }

        return 0;
    }

    private int LookupFormat12(uint codePoint)
    {
        int offset = _cmapSubtableOffset;
        if (offset + 16 > _fontData.Length) return 0;

        uint numGroups = BinaryPrimitives.ReadUInt32BigEndian(_fontData.AsSpan(offset + 12, 4));
        int groupsStart = offset + 16;

        int low = 0;
        int high = (int)numGroups - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            int groupOffset = groupsStart + (mid * 12);
            if (groupOffset + 12 > _fontData.Length) break;

            uint startCharCode = BinaryPrimitives.ReadUInt32BigEndian(_fontData.AsSpan(groupOffset, 4));
            uint endCharCode = BinaryPrimitives.ReadUInt32BigEndian(_fontData.AsSpan(groupOffset + 4, 4));
            uint startGlyphId = BinaryPrimitives.ReadUInt32BigEndian(_fontData.AsSpan(groupOffset + 8, 4));

            if (codePoint < startCharCode)
            {
                high = mid - 1;
            }
            else if (codePoint > endCharCode)
            {
                low = mid + 1;
            }
            else
            {
                return (int)(startGlyphId + (codePoint - startCharCode));
            }
        }

        return 0;
    }

    public ushort GetAdvanceWidth(int glyphIndex)
    {
        if (!_tables.TryGetValue("hmtx", out var entry) || NumberOfHMetrics == 0)
            return (ushort)UnitsPerEm;

        int hmtxOffset = entry.Offset;

        if (glyphIndex < NumberOfHMetrics)
        {
            int metricOffset = hmtxOffset + (glyphIndex * 4);
            if (metricOffset + 2 <= _fontData.Length)
            {
                return BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(metricOffset, 2));
            }
        }
        else
        {
            // Monospaced or glyphs beyond numberOfHMetrics share the last record's advanceWidth
            int lastMetricOffset = hmtxOffset + ((NumberOfHMetrics - 1) * 4);
            if (lastMetricOffset + 2 <= _fontData.Length)
            {
                return BinaryPrimitives.ReadUInt16BigEndian(_fontData.AsSpan(lastMetricOffset, 2));
            }
        }

        return (ushort)UnitsPerEm;
    }

    /// <summary>
    /// Calculates font advance width normalized to 1000 units per em (PDF point standard).
    /// </summary>
    public int GetPdfWidth(int unicodeCodePoint)
    {
        int glyphIndex = GetGlyphIndex(unicodeCodePoint);
        ushort advance = GetAdvanceWidth(glyphIndex);
        if (UnitsPerEm == 1000) return advance;
        return (int)Math.Round((double)advance * 1000.0 / UnitsPerEm);
    }

    /// <summary>
    /// Measures text width in PDF points for a given font size.
    /// </summary>
    public double MeasureText(ReadOnlySpan<char> text, double fontSize)
    {
        double totalUnits = 0;
        for (int i = 0; i < text.Length; i++)
        {
            int codePoint = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])
                ? char.ConvertToUtf32(text[i++], text[i])
                : text[i];

            totalUnits += GetPdfWidth(codePoint);
        }

        return (totalUnits * fontSize) / 1000.0;
    }
}
