#region License
/* TrueTypeSharp
   Copyright (c) 2010 Illusory Studios LLC

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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TrueTypeSharp
{
    [Serializable]
    public class BakedCharCollection : ISerializable
    {
        Dictionary<char, BakedChar> _characters;
        int _bakeWidth, _bakeHeight;

        public BakedCharCollection(char firstCodepoint, BakedChar[] characters,
            int bakeWidth, int bakeHeight)
        {
            if (characters == null) { throw new ArgumentNullException("characters"); }

            Dictionary<char, BakedChar> dictionary = new Dictionary<char, BakedChar>();
            for (int i = 0; i < characters.Length; i++)
            {
                char codepoint = (char)(firstCodepoint + i);
                if (char.IsSurrogate(codepoint)) { continue; }

                BakedChar character = characters[i];
                if (character.IsEmpty) { continue; }

                dictionary[codepoint] = character;
            }
            Create(dictionary, bakeWidth, bakeHeight);
        }

        protected BakedCharCollection(SerializationInfo info, StreamingContext context)
        {
            Create((Dictionary<char, BakedChar>)
                info.GetValue("Characters", typeof(Dictionary<char, BakedChar>)),
                info.GetInt32("BakeWidth"), info.GetInt32("BakeHeight"));
        }

        void Create(Dictionary<char, BakedChar> characters, int bakeWidth, int bakeHeight)
        {
            if (characters == null) { throw new ArgumentNullException("characters"); }
            if (bakeWidth < 0 || bakeHeight < 0) { throw new ArgumentOutOfRangeException(); }

            BakeWidth = bakeWidth; BakeHeight = bakeHeight;
            _characters = characters;
            
        }

        public IEnumerable<BakedQuad> GetTextQuads(string str,
            float lineAscender, float lineDescender, float lineGap,
            int hAlign, int vAlign)
        {
            return GetTextQuads(new string[] { str },
                lineAscender, lineDescender, lineGap, hAlign, vAlign);
        }

        public IEnumerable<BakedQuad> GetTextQuads(IEnumerable<string> strs,
            float lineAscender, float lineDescender, float lineGap,
            int hAlign, int vAlign)
        {
            if (strs == null) { throw new ArgumentNullException("strs"); }

            float y0 = 0, y1, yPosition = 0;
            y1 = BakedCharCollection.GetVerticalTextHeight(strs, lineAscender, lineDescender, lineGap);
            BakedCharCollection.OffsetTextPositionForAlignment(y0, y1, ref yPosition, vAlign);

            foreach (var str in strs)
            {
                yPosition += lineAscender;

                float sx0, sy0, sx1, sy1, sxPosition = 0, syPosition = yPosition;
                if (GetTextBounds(str, out sx0, out sy0, out sx1, out sy1))
                {
                    BakedCharCollection.OffsetTextPositionForAlignment
                        (sx0, sx1, ref sxPosition, hAlign);

                    foreach (var ch in str)
                    {
                        var quad = GetBakedQuad(ch, ref sxPosition, ref syPosition);
                        if (quad.IsEmpty) { continue; }

                        yield return quad;
                    }
                }

                yPosition += lineGap - lineDescender;
            }
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Characters", Characters);
            info.AddValue("BakeWidth", BakeWidth);
            info.AddValue("BakeHeight", BakeHeight);
        }

        public BakedQuad GetBakedQuad(char character,
            ref float xPosition, ref float yPosition)
        {
            return GetBakedQuad(character, ref xPosition, ref yPosition, false);
        }
        
        public BakedQuad GetBakedQuad(char character,
            ref float xPosition, ref float yPosition, bool putTexCoordsAtTexelCenters)
        {
            var bakedChar = this[character]; if (bakedChar.IsEmpty) { return BakedQuad.Empty; }
            return bakedChar.GetBakedQuad(BakeWidth, BakeHeight,
                ref xPosition, ref yPosition, putTexCoordsAtTexelCenters);
        }

        static int ElementCount(IEnumerable<string> strs)
        {
            var list = strs as ICollection<string>; if (list != null) { return list.Count; }
            var e = strs.GetEnumerator(); int count = 0; while (e.MoveNext()) { count++; }
            return count;
        }

        public static float GetVerticalTextHeight(IEnumerable<string> strs,
            float lineAscender, float lineDescender, float lineGap)
        {
            if (strs == null) { throw new ArgumentNullException("strs"); }
            return GetVerticalTextHeight(ElementCount(strs),
                lineAscender, lineDescender, lineGap);
        }

        public static float GetVerticalTextHeight(int lineCount,
            float lineAscender, float lineDescender, float lineGap)
        {
            return Math.Max(0, lineGap * (lineCount - 1)
                + (lineAscender - lineDescender) * lineCount);
        }

        public bool GetTextBounds(string str, out float x0, out float y0, out float x1, out float y1)
        {
            if (str == null) { throw new ArgumentNullException("str"); }
            x0 = y0 = x1 = y1 = 0; bool isSet = false; float xPosition = 0, yPosition = 0;

            foreach (var ch in str)
            {
                var quad = GetBakedQuad(ch, ref xPosition, ref yPosition);
                if (quad.IsEmpty) { continue; }

                if (isSet)
                {
                    if (quad.X0 < x0) { x0 = quad.X0; } if (quad.Y0 < y0) { y0 = quad.Y0; }
                    if (quad.X1 > x1) { x1 = quad.X1; } if (quad.Y1 > y1) { y1 = quad.Y1; }
                }
                else
                {
                    x0 = quad.X0; y0 = quad.Y0; x1 = quad.X1; y1 = quad.Y1;
                    isSet = true;
                }
            }

            return isSet;
        }

        public void OffsetTextPositionForAlignment(string str,
            ref float xPosition, ref float yPosition, int hAlign, int vAlign)
        {
            float x0, y0, x1, y1;
            GetTextBounds(str, out x0, out y0, out x1, out y1);
            OffsetTextPositionForAlignment(x0, x1, ref xPosition, hAlign);
            OffsetTextPositionForAlignment(y0, y1, ref yPosition, vAlign);
        }

        public static void OffsetTextPositionForAlignment(float xOrY0, float xOrY1,
            ref float position, int align)
        {
            if (align < 0) { position -= xOrY0; }
            else if (align == 0) { position -= (xOrY0 + xOrY1) * 0.5f; }
            else { position -= xOrY1; }
        }

        public int BakeWidth
        {
            get { return _bakeWidth; }
            set { if (value < 0) { throw new ArgumentOutOfRangeException(); } _bakeWidth = value; }
        }

        public int BakeHeight
        {
            get { return _bakeHeight; }
            set { if (value < 0) { throw new ArgumentOutOfRangeException(); } _bakeHeight = value; }
        }

        public IDictionary<char, BakedChar> Characters
        {
            get { return _characters; }
        }

        public BakedChar this[char character]
        {
            get
            {
                BakedChar bakedChar;
                _characters.TryGetValue(character, out bakedChar);
                return bakedChar;
            }
        }
    }
}
