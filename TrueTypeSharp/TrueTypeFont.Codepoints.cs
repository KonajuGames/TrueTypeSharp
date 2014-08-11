#region License
/* TrueTypeSharp
   Copyright (c) 2010, 2012 Illusory Studios LLC

   TrueTypeSharp is available at zer7.com. It is a C# port of Sean Barrett's
   C library stb_truetype, which was placed in the public domain and is
   available at nothings.org.

   Permission to use, copy, modify, and/or distribute this software for any
   purpose with or without fee is hereby granted, provided that the above
   copyright notice and this permission notice appear in all copies.

   THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
   WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
   MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
   ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
   WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
   ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
   OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/
#endregion

namespace TrueTypeSharp
{
    public partial class TrueTypeFont
    {
        public byte[] GetCodepointBitmapSubpixel(char codepoint,
            float xScale, float yScale, float xShift, float yShift,
            out int width, out int height, out int xOffset, out int yOffset)
        {
            return GetGlyphBitmapSubpixel(FindGlyphIndex(codepoint),
                xScale, yScale, xShift, yShift,
                out width, out height, out xOffset, out yOffset);
        }

        public byte[] GetCodepointBitmap(char codepoint, float xScale, float yScale,
            out int width, out int height, out int xOffset, out int yOffset)
        {
            return GetCodepointBitmapSubpixel(codepoint, xScale, yScale, 0, 0,
                out width, out height, out xOffset, out yOffset);
        }

        public void GetCodepointBitmapBoxSubpixel(char codepoint,
            float xScale, float yScale, float xShift, float yShift,
            out int x0, out int y0, out int x1, out int y1)
        {
            GetGlyphBitmapBoxSubpixel(FindGlyphIndex(codepoint),
                xScale, yScale, xShift, yShift,
                out x0, out y0, out x1, out y1);
        }

        public void GetCodepointBitmapBox(char codepoint, float xScale, float yScale,
            out int x0, out int y0, out int x1, out int y1)
        {
            GetCodepointBitmapBoxSubpixel(codepoint, xScale, yScale, 0, 0, out x0, out y0, out x1, out y1);
        }

        public void GetCodepointBox(char codepoint,
            out int x0, out int y0, out int x1, out int y1)
        {
            GetGlyphBox(FindGlyphIndex(codepoint), out x0, out y0, out x1, out y1);
        }

        public int GetCodepointKernAdvance(char codepoint1, char codepoint2)
        {
            return GetGlyphKernAdvance(FindGlyphIndex(codepoint1), FindGlyphIndex(codepoint2));
        }

        public void GetCodepointHMetrics(char codepoint, out int advanceWidth, out int leftSideBearing)
        {
            GetGlyphHMetrics(FindGlyphIndex(codepoint), out advanceWidth, out leftSideBearing);
        }

        public GlyphVertex[] GetCodepointShape(char codepoint)
        {
            return GetGlyphShape(FindGlyphIndex(codepoint));
        }

        public void MakeCodepointBitmap(char codepoint, float xScale, float yScale,
            FontBitmap bitmap)
        {
            MakeGlyphBitmap(FindGlyphIndex(codepoint), xScale, yScale, bitmap);
        }
    }
}
