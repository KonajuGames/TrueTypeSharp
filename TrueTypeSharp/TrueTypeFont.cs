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

using System;
using System.Diagnostics;
using System.IO;

namespace TrueTypeSharp
{
    public partial class TrueTypeFont
    {
        stb_truetype.stbtt_fontinfo _info;

        public TrueTypeFont(byte[] data, int offset)
        {
            CheckFontData(data, offset);

            if (0 == stb_truetype.stbtt_InitFont(ref _info,
                new FakePtr<byte>() { Array = data }, offset))
            {
                throw new BadImageFormatException("Couldn't load TrueType file.");
            }
        }

        public TrueTypeFont(string filename)
            : this(File.ReadAllBytes(filename), 0)
        {

        }

        static void CheckFontData(byte[] data, int offset)
        {
            if (data == null) { throw new ArgumentNullException("data"); }
            if (offset < 0 || offset > data.Length) { throw new ArgumentOutOfRangeException("offset"); }
        }

        public static bool TryGetFontOffsetForIndex(byte[] data, int index, out int offset)
        {
            offset = stb_truetype.stbtt_GetFontOffsetForIndex(new FakePtr<byte>() { Array = data }, index);
            if (offset < 0) { offset = 0; return false; } else { return true; }
        }

        public int BakeFontBitmap(float pixelHeight, char firstCodepoint,
            BakedChar[] characters, FontBitmap bitmap)
        {
            float scale = GetScaleForPixelHeight(pixelHeight);
            return BakeFontBitmap(scale, scale, firstCodepoint, characters, bitmap);
        }

        public int BakeFontBitmap(float xScale, float yScale, char firstCodepoint,
            BakedChar[] characters, FontBitmap bitmap)
        {
            if (!bitmap.IsValid) { throw new ArgumentException("bitmap"); }
            if (characters == null) { throw new ArgumentNullException("characters"); }

            return stb_truetype.stbtt_BakeFontBitmap(ref _info, xScale, yScale,
                bitmap.StartPointer, bitmap.Width, bitmap.Height, bitmap.Stride,
                (int)firstCodepoint, characters.Length,
                new FakePtr<BakedChar>() { Array = characters });
        }

        // generates square textures ... minimalHeight can be used to crop if desired
        public FontBitmap BakeFontBitmap(float pixelHeight, char firstCodepoint,
            BakedChar[] characters, out int minimalHeight)
        {
            int size = 16;
            if (characters.Length == 0) { minimalHeight = 0; return new FontBitmap(0, 0); }

            while (true)
            {
                var bitmap = new FontBitmap(size, size);
                int result = BakeFontBitmap(pixelHeight, firstCodepoint, characters, bitmap);
                if (result > 0) { minimalHeight = result; return bitmap; }

                size *= 2;
            }
        }

        public FontBitmap BakeFontBitmap(float pixelHeight, out BakedCharCollection characters)
        {
            return BakeFontBitmap(pixelHeight, out characters, false);
        }

        public FontBitmap BakeFontBitmap(float pixelHeight, out BakedCharCollection characters,
            bool shrinkToMinimalHeight)
        {
            return BakeFontBitmap(pixelHeight, char.MinValue, char.MaxValue, out characters,
                shrinkToMinimalHeight);
        }

        public FontBitmap BakeFontBitmap(float pixelHeight, char firstCodepoint,
            char lastCodepoint, out BakedCharCollection characters)
        {
            if (lastCodepoint < firstCodepoint)
            {
                var tmp = lastCodepoint;
                lastCodepoint = firstCodepoint;
                firstCodepoint = tmp;
            }

            return BakeFontBitmap(pixelHeight, firstCodepoint,
                lastCodepoint - firstCodepoint + 1, out characters);
        }

        public FontBitmap BakeFontBitmap(float pixelHeight, char firstCodepoint,
            int characterCount, out BakedCharCollection characters)
        {
            return BakeFontBitmap(pixelHeight, firstCodepoint, characterCount,
                out characters, false);
        }

        public FontBitmap BakeFontBitmap(float pixelHeight, char firstCodepoint,
            int characterCount, out BakedCharCollection characters, bool shrinkToMinimalHeight)
        {
            if (characterCount < 0) { throw new ArgumentOutOfRangeException("characterCount"); }

            var charArray = new BakedChar[characterCount]; int minimalHeight;
            var bitmap = BakeFontBitmap(pixelHeight, firstCodepoint, charArray, out minimalHeight);
            if (shrinkToMinimalHeight) { bitmap.Height = minimalHeight; bitmap = bitmap.GetTrimmedBitmap(); }

            characters = new BakedCharCollection(firstCodepoint, charArray, bitmap.Width, bitmap.Height);
            return bitmap;
        }

        public int FindGlyphIndex(char codepoint)
        {
            return stb_truetype.stbtt_FindGlyphIndex(ref _info, (int)codepoint);
        }

        public void FlattenCurves(GlyphVertex[] vertices, float flatness,
            out ContourPoint[] points, out int[] contourLengths)
        {
            if (vertices == null) { throw new ArgumentNullException("vertices"); }

            FakePtr<int> contourLengthsFP; int contourCount;
            var contours = stb_truetype.stbtt_FlattenCurves
                (new FakePtr<GlyphVertex>() { Array = vertices }, vertices.Length,
                flatness, out contourLengthsFP, out contourCount);
            contourLengths = contourLengthsFP.GetData(contourCount);

            int pointCount = 0;
            foreach (var length in contourLengths) { pointCount += length; }
            points = contours.GetData(pointCount);
        }

        public byte[] GetGlyphBitmapSubpixel(int glyphIndex, float xScale, float yScale, float xShift, float yShift,
            out int width, out int height, out int xOffset, out int yOffset)
        {
            var data = stb_truetype.stbtt_GetGlyphBitmapSubpixel(ref _info, xScale, yScale, xShift, yShift, glyphIndex,
                out width, out height, out xOffset, out yOffset);
            if (data.IsNull) { width = 0; height = 0; xOffset = 0; yOffset = 0; return data.GetData(0); }
            return data.GetData(width * height);
        }

        public byte[] GetGlyphBitmap(int glyphIndex, float xScale, float yScale,
            out int width, out int height, out int xOffset, out int yOffset)
        {
            return GetGlyphBitmapSubpixel(glyphIndex, xScale, yScale, 0, 0,
                out width, out height, out xOffset, out yOffset);
        }

        public void MakeGlyphBitmapSubpixel(int glyphIndex,
            float xScale, float yScale, float xShift, float yShift,
            FontBitmap bitmap)
        {
            if (bitmap.Buffer == null) { throw new ArgumentNullException("bitmap.Buffer"); }
            stb_truetype.stbtt_MakeGlyphBitmapSubpixel(ref _info,
                bitmap.StartPointer, bitmap.Width, bitmap.Height, bitmap.Stride,
                xScale, yScale, xShift, yShift, glyphIndex);
        }

        public void MakeGlyphBitmap(int glyphIndex, float xScale, float yScale, FontBitmap bitmap)
        {
            MakeGlyphBitmapSubpixel(glyphIndex, xScale, yScale, 0, 0, bitmap);
        }

        public void GetGlyphBitmapBoxSubpixel(int glyphIndex,
            float xScale, float yScale, float xShift, float yShift,
            out int x0, out int y0, out int x1, out int y1)
        {
            stb_truetype.stbtt_GetGlyphBitmapBoxSubpixel(ref _info, glyphIndex,
                xScale, yScale, xShift, yShift, out x0, out y0, out x1, out y1);
        }

        public void GetGlyphBitmapBox(int glyphIndex, float xScale, float yScale,
            out int x0, out int y0, out int x1, out int y1)
        {
            GetGlyphBitmapBoxSubpixel(glyphIndex, xScale, yScale, 0, 0, out x0, out y0, out x1, out y1);
        }

        public void GetGlyphBox(int glyphIndex,
            out int x0, out int y0, out int x1, out int y1)
        {
            stb_truetype.stbtt_GetGlyphBox(ref _info, glyphIndex,
                out x0, out y0, out x1, out y1);
        }

        public void GetGlyphHMetrics(int glyphIndex, out int advanceWidth, out int leftSideBearing)
        {
            stb_truetype.stbtt_GetGlyphHMetrics(ref _info, glyphIndex,
                out advanceWidth, out leftSideBearing);
        }

        public GlyphVertex[] GetGlyphShape(int glyphIndex)
        {
            FakePtr<GlyphVertex> vertices;
            int n = stb_truetype.stbtt_GetGlyphShape(ref _info, glyphIndex, out vertices);
            return vertices.GetData(n);
        }

        public int GetGlyphKernAdvance(int glyph1Index, int glyph2Index)
        {
            return stb_truetype.stbtt_GetGlyphKernAdvance(ref _info, glyph1Index, glyph2Index);
        }

        public void GetFontVMetrics(out int lineAscender, out int lineDescender, out int lineGap)
        {
            stb_truetype.stbtt_GetFontVMetrics(ref _info, out lineAscender, out lineDescender, out lineGap);
        }

        public void GetFontVMetricsAtScale(float pixelHeight, out float lineAscender, out float lineDescender, out float lineGap)
        {
            int lineAscenderI, lineDescenderI, lineGapI;
            GetFontVMetrics(out lineAscenderI, out lineDescenderI, out lineGapI);

            float scale = GetScaleForPixelHeight(pixelHeight);
            lineAscender = (float)lineAscenderI * scale;
            lineDescender = (float)lineDescenderI * scale;
            lineGap = (float)lineGapI * scale;
        }

        public float GetScaleForPixelHeight(float height)
        {
            return stb_truetype.stbtt_ScaleForPixelHeight(ref _info, height);
        }

        public float GetScaleForMappingEmToPixels(float pixels)
        {
            return stb_truetype.stbtt_ScaleForMappingEmToPixels(ref _info, pixels);
        }
    }
}
