namespace TinyPdf;

using System;
using System.Collections.Generic;

/// <summary>
/// Adobe Font Metrics (AFM) standard tables and 256-entry WinAnsi width tables
/// for the Standard 14 PDF Type 1 fonts.
/// </summary>
public static class AfmMetrics
{
    // 256-entry WinAnsi width tables
    private static readonly int[] s_helveticaWinAnsi = BuildHelveticaWinAnsi();
    private static readonly int[] s_helveticaBoldWinAnsi = BuildHelveticaBoldWinAnsi();
    private static readonly int[] s_helveticaObliqueWinAnsi = s_helveticaWinAnsi;
    private static readonly int[] s_helveticaBoldObliqueWinAnsi = s_helveticaBoldWinAnsi;
    private static readonly int[] s_timesWinAnsi = BuildTimesWinAnsi();
    private static readonly int[] s_timesBoldWinAnsi = BuildTimesBoldWinAnsi();
    private static readonly int[] s_courierWinAnsi = BuildCourierWinAnsi();

    public static int GetGlyphWidth(char c, TinyPdfCreate.PdfFont font)
    {
        // 1. Check ASCII 0..127
        int code = (int)c;
        if (code >= 32 && code <= 126)
        {
            var table = GetFontTable(font);
            return table[code];
        }

        // 2. Map Unicode character to WinAnsi byte code (0..255)
        int winAnsiCode = UnicodeToWinAnsi(c);
        if (winAnsiCode >= 0)
        {
            var table = GetFontTable(font);
            return table[winAnsiCode];
        }

        // 3. CJK / Fullwidth East Asian Ideographs (e.g. '中') are full 1000-unit em width
        if (IsFullWidth(c))
        {
            return 1000;
        }

        // 4. Non-printable / zero-width characters
        if (char.IsControl(c) || c == '\u200B' || c == '\uFEFF')
        {
            return 0;
        }

        // 5. Half-width Katakana / Hangul or proportional fallback
        if (c >= 0xFF61 && c <= 0xFFDC)
        {
            return 500;
        }

        // Default proportional fallback based on font style
        return font switch
        {
            TinyPdfCreate.PdfFont.Courier or TinyPdfCreate.PdfFont.CourierBold or
            TinyPdfCreate.PdfFont.CourierOblique or TinyPdfCreate.PdfFont.CourierBoldOblique => 600,
            TinyPdfCreate.PdfFont.HelveticaBold or TinyPdfCreate.PdfFont.TimesBold => 650,
            _ => 500
        };
    }

    private static int[] GetFontTable(TinyPdfCreate.PdfFont font) => font switch
    {
        TinyPdfCreate.PdfFont.Helvetica => s_helveticaWinAnsi,
        TinyPdfCreate.PdfFont.HelveticaBold => s_helveticaBoldWinAnsi,
        TinyPdfCreate.PdfFont.HelveticaOblique => s_helveticaObliqueWinAnsi,
        TinyPdfCreate.PdfFont.HelveticaBoldOblique => s_helveticaBoldObliqueWinAnsi,
        TinyPdfCreate.PdfFont.Times or TinyPdfCreate.PdfFont.TimesItalic => s_timesWinAnsi,
        TinyPdfCreate.PdfFont.TimesBold or TinyPdfCreate.PdfFont.TimesBoldItalic => s_timesBoldWinAnsi,
        _ => s_courierWinAnsi
    };

    public static bool IsFullWidth(char c)
    {
        return (c >= 0x1100 && c <= 0x11FF) || // Hangul Jamo
               (c >= 0x2E80 && c <= 0xA4CF) || // CJK Radicals, Kangxi, Ideographs, Yi
               (c >= 0xAC00 && c <= 0xD7AF) || // Hangul Syllables
               (c >= 0xF900 && c <= 0xFAFF) || // CJK Compatibility Ideographs
               (c >= 0xFE30 && c <= 0xFE4F) || // CJK Compatibility Forms
               (c >= 0xFF01 && c <= 0xFF60) || // Fullwidth ASCII variants
               (c >= 0xFFE0 && c <= 0xFFE6);   // Fullwidth symbol variants
    }

    public static int UnicodeToWinAnsi(char c)
    {
        if (c <= 0xFF) return c; // Standard Latin-1 0..255

        // Windows-1252 extensions for typographic Unicode characters
        return c switch
        {
            '\u20AC' => 128, // Euro €
            '\u201A' => 130, // Single low-9 quotation mark ‚
            '\u0192' => 131, // Latin small letter f with hook ƒ
            '\u201E' => 132, // Double low-9 quotation mark „
            '\u2026' => 133, // Horizontal ellipsis …
            '\u2020' => 134, // Dagger †
            '\u2021' => 135, // Double dagger ‡
            '\u02C6' => 136, // Modifier letter circumflex accent ˆ
            '\u2030' => 137, // Per mille sign ‰
            '\u0160' => 138, // Latin capital letter S with caron Š
            '\u2039' => 139, // Single left-pointing angle quotation mark ‹
            '\u0152' => 140, // Latin capital ligature OE Œ
            '\u017D' => 142, // Latin capital letter Z with caron Ž
            '\u2018' => 145, // Left single quotation mark ‘
            '\u2019' => 146, // Right single quotation mark ’
            '\u201C' => 147, // Left double quotation mark “
            '\u201D' => 148, // Right double quotation mark ”
            '\u2022' => 149, // Bullet •
            '\u2013' => 150, // En dash –
            '\u2014' => 151, // Em dash —
            '\u02DC' => 152, // Small tilde ˜
            '\u2122' => 153, // Trade mark sign ™
            '\u0161' => 154, // Latin small letter s with caron š
            '\u203A' => 155, // Single right-pointing angle quotation mark ›
            '\u0153' => 156, // Latin small ligature oe œ
            '\u017E' => 158, // Latin small letter z with caron ž
            '\u0178' => 159, // Latin capital letter Y with diaeresis Ÿ
            _ => -1
        };
    }

    private static int[] BuildCourierWinAnsi()
    {
        var t = new int[256];
        Array.Fill(t, 600);
        for (int i = 0; i < 32; i++) t[i] = 0;
        t[127] = 0;
        return t;
    }

    private static int[] BuildHelveticaWinAnsi()
    {
        var t = new int[256];
        // ASCII 32..126
        int[] ascii = [
            278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
            556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
            1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
            667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
            333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
            556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584
        ];
        Array.Copy(ascii, 0, t, 32, ascii.Length);

        // WinAnsi 128..255
        t[128] = 556; t[130] = 222; t[131] = 556; t[132] = 333; t[133] = 1000;
        t[134] = 556; t[135] = 556; t[136] = 333; t[137] = 1000; t[138] = 667;
        t[139] = 333; t[140] = 1000; t[142] = 667; t[145] = 222; t[146] = 222;
        t[147] = 333; t[148] = 333; t[149] = 350; t[150] = 556; t[151] = 1000;
        t[152] = 333; t[153] = 1000; t[154] = 500; t[155] = 333; t[156] = 889;
        t[158] = 500; t[159] = 667;

        for (int i = 160; i <= 255; i++)
        {
            if (i == 160) t[i] = 278;
            else if (i == 161) t[i] = 333;
            else if (i >= 162 && i <= 165) t[i] = 556;
            else if (i == 166) t[i] = 260;
            else if (i == 167) t[i] = 556;
            else if (i == 169 || i == 174) t[i] = 737;
            else if (i >= 171 && i <= 173) t[i] = 556;
            else if (i == 177) t[i] = 584;
            else if (i >= 187 && i <= 190) t[i] = 556;
            else if (i == 191) t[i] = 611;
            else if (i >= 192 && i <= 223) t[i] = (i == 198) ? 1000 : 667; // AE is 1000
            else if (i >= 224 && i <= 255) t[i] = (i == 223) ? 611 : (i == 230) ? 889 : 556;
            else t[i] = 500;
        }

        return t;
    }

    private static int[] BuildHelveticaBoldWinAnsi()
    {
        var t = new int[256];
        int[] ascii = [
            278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
            556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
            975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
            667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
            333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
            611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584
        ];
        Array.Copy(ascii, 0, t, 32, ascii.Length);

        for (int i = 128; i < 256; i++)
        {
            t[i] = (int)(s_helveticaWinAnsi[i] * 1.05);
        }
        return t;
    }

    private static int[] BuildTimesWinAnsi()
    {
        var t = new int[256];
        int[] ascii = [
            250, 333, 408, 500, 500, 833, 778, 180, 333, 333, 500, 564, 250, 333, 250, 278,
            500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 278, 278, 564, 564, 564, 444,
            921, 722, 667, 667, 722, 611, 556, 722, 722, 333, 389, 722, 611, 889, 722, 722,
            556, 722, 667, 556, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 469, 500,
            333, 444, 500, 444, 500, 444, 333, 500, 500, 278, 278, 500, 278, 778, 500, 500,
            500, 500, 333, 389, 278, 500, 500, 722, 500, 500, 444, 480, 200, 480, 541
        ];
        Array.Copy(ascii, 0, t, 32, ascii.Length);

        for (int i = 128; i < 256; i++)
        {
            t[i] = s_helveticaWinAnsi[i];
        }
        return t;
    }

    private static int[] BuildTimesBoldWinAnsi()
    {
        var t = new int[256];
        int[] ascii = [
            250, 333, 555, 500, 500, 1000, 833, 278, 333, 333, 500, 570, 250, 333, 250, 278,
            500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 333, 333, 570, 570, 570, 500,
            930, 722, 667, 722, 722, 667, 611, 778, 778, 389, 500, 778, 667, 944, 722, 778,
            611, 778, 722, 556, 667, 722, 722, 1000, 722, 722, 667, 333, 278, 333, 581, 500,
            333, 500, 556, 444, 556, 444, 333, 500, 556, 278, 333, 556, 278, 833, 556, 500,
            556, 556, 444, 389, 333, 556, 500, 722, 500, 500, 444, 394, 220, 394, 520
        ];
        Array.Copy(ascii, 0, t, 32, ascii.Length);

        for (int i = 128; i < 256; i++)
        {
            t[i] = (int)(s_timesWinAnsi[i] * 1.05);
        }
        return t;
    }
}
