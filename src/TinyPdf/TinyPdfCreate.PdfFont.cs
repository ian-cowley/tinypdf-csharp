namespace TinyPdf;

public partial class TinyPdfCreate
{
    /// <summary>
    /// Represents the Standard 14 PDF Type 1 fonts supported by TinyPdf.
    /// </summary>
    public enum PdfFont
    {
        Helvetica,
        HelveticaBold,
        HelveticaOblique,
        HelveticaBoldOblique,
        Times,
        TimesBold,
        TimesItalic,
        TimesBoldItalic,
        Courier,
        CourierBold,
        CourierOblique,
        CourierBoldOblique,
        Symbol,
        ZapfDingbats
    }

    /// <summary>
    /// Returns the internal PDF resource font tag (e.g. /F1, /F4) for the specified font.
    /// </summary>
    public static string GetFontTag(PdfFont font) => font switch
    {
        PdfFont.Helvetica => "/F1",
        PdfFont.Times => "/F2",
        PdfFont.Courier => "/F3",
        PdfFont.HelveticaBold => "/F4",
        PdfFont.HelveticaOblique => "/F5",
        PdfFont.HelveticaBoldOblique => "/F6",
        PdfFont.TimesBold => "/F7",
        PdfFont.TimesItalic => "/F8",
        PdfFont.TimesBoldItalic => "/F9",
        PdfFont.CourierBold => "/F10",
        PdfFont.CourierOblique => "/F11",
        PdfFont.CourierBoldOblique => "/F12",
        PdfFont.Symbol => "/F13",
        PdfFont.ZapfDingbats => "/F14",
        _ => "/F1"
    };
}
