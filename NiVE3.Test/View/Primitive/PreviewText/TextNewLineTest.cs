using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiVE3.View.Primitive.PreviewText;

namespace NiVE3.Test.View.Primitive.PreviewText
{
    /// <summary>
    /// 改行コード ("\r\n" / "\n" / "\r") を正規化せずに扱う TextNewLine・TextDocument・TextNavigation のテスト
    /// </summary>
    public class TextNewLineTest
    {
        [Test]
        public void TestGetLengthAt()
        {
            Assert.Multiple(() =>
            {
                Assert.That(TextNewLine.GetLengthAt("a\r\nb", 1), Is.EqualTo(2));
                Assert.That(TextNewLine.GetLengthAt("a\r\nb", 2), Is.EqualTo(1));
                Assert.That(TextNewLine.GetLengthAt("a\nb", 1), Is.EqualTo(1));
                Assert.That(TextNewLine.GetLengthAt("a\rb", 1), Is.EqualTo(1));
                Assert.That(TextNewLine.GetLengthAt("a\r\nb", 0), Is.EqualTo(0));
                Assert.That(TextNewLine.GetLengthAt("a\r", 1), Is.EqualTo(1));
                Assert.That(TextNewLine.GetLengthAt("a", 1), Is.EqualTo(0));
            });
        }

        [Test]
        public void TestGetLengthBefore()
        {
            Assert.Multiple(() =>
            {
                Assert.That(TextNewLine.GetLengthBefore("a\r\nb", 3), Is.EqualTo(2));
                Assert.That(TextNewLine.GetLengthBefore("a\r\nb", 2), Is.EqualTo(1));
                Assert.That(TextNewLine.GetLengthBefore("a\nb", 2), Is.EqualTo(1));
                Assert.That(TextNewLine.GetLengthBefore("a\rb", 2), Is.EqualTo(1));
                Assert.That(TextNewLine.GetLengthBefore("a\r\nb", 1), Is.EqualTo(0));
                Assert.That(TextNewLine.GetLengthBefore("a", 0), Is.EqualTo(0));
            });
        }

        [Test]
        public void TestDetectAndConvert()
        {
            Assert.Multiple(() =>
            {
                Assert.That(TextNewLine.Detect("a\r\nb\nc"), Is.EqualTo("\r\n"));
                Assert.That(TextNewLine.Detect("a\nb"), Is.EqualTo("\n"));
                Assert.That(TextNewLine.Detect("a\rb"), Is.EqualTo("\r"));
                Assert.That(TextNewLine.Detect("ab"), Is.EqualTo(TextNewLine.Default));
                Assert.That(TextNewLine.ConvertTo("a\r\nb\nc\rd", "\r\n"), Is.EqualTo("a\r\nb\r\nc\r\nd"));
                Assert.That(TextNewLine.ConvertTo("a\r\nb\nc\rd", "\n"), Is.EqualTo("a\nb\nc\nd"));
                Assert.That(TextNewLine.ConvertTo("abc", "\r\n"), Is.EqualTo("abc"));
            });
        }

        [Test]
        public void TestDocumentKeepsCrLfAndLineIndex()
        {
            var document = new TextDocument();
            document.SetText("ab\r\ncd\r\n\r\ne");

            Assert.Multiple(() =>
            {
                Assert.That(document.Text, Is.EqualTo("ab\r\ncd\r\n\r\ne"));
                Assert.That(document.NewLine, Is.EqualTo("\r\n"));
                Assert.That(document.LineCount, Is.EqualTo(4));
                Assert.That(document.GetLineStart(1), Is.EqualTo(4));
                Assert.That(document.GetLineStart(2), Is.EqualTo(8));
                Assert.That(document.GetLineStart(3), Is.EqualTo(10));
                Assert.That(document.GetLineLength(0), Is.EqualTo(2));
                Assert.That(document.GetLineLength(1), Is.EqualTo(2));
                Assert.That(document.GetLineLength(2), Is.EqualTo(0));
                Assert.That(document.GetLineLength(3), Is.EqualTo(1));
                Assert.That(document.GetLineNewLineLength(0), Is.EqualTo(2));
                Assert.That(document.GetLineNewLineLength(3), Is.EqualTo(0));
                // 行末 ('\r' の直前) はその行に属する
                Assert.That(document.GetLineIndexFromOffset(2), Is.EqualTo(0));
                Assert.That(document.GetLineIndexFromOffset(4), Is.EqualTo(1));
            });
        }

        [Test]
        public void TestDocumentLineIndexWithLfOnly()
        {
            var document = new TextDocument();
            document.SetText("ab\ncd");

            Assert.Multiple(() =>
            {
                Assert.That(document.NewLine, Is.EqualTo("\n"));
                Assert.That(document.LineCount, Is.EqualTo(2));
                Assert.That(document.GetLineStart(1), Is.EqualTo(3));
                Assert.That(document.GetLineLength(0), Is.EqualTo(2));
                Assert.That(document.GetLineNewLineLength(0), Is.EqualTo(1));
            });
        }

        [Test]
        public void TestNavigationTreatsCrLfAsOneElement()
        {
            const string text = "ab\r\ncd";

            Assert.Multiple(() =>
            {
                // Delete: 'b' の直後から改行 2 文字分を跨ぐ
                Assert.That(TextNavigation.NextElement(text, 2), Is.EqualTo(4));
                // Backspace: 'c' の直前から改行 2 文字分を戻る
                Assert.That(TextNavigation.PrevElement(text, 4), Is.EqualTo(2));
                Assert.That(TextNavigation.PrevElement(text, 5), Is.EqualTo(4));
                Assert.That(TextNavigation.NextElement(text, 1), Is.EqualTo(2));
                // 単語移動も改行を空白と同様に跨ぐ
                Assert.That(TextNavigation.NextWord(text, 0), Is.EqualTo(4));
                Assert.That(TextNavigation.PrevWord(text, 4), Is.EqualTo(0));
                Assert.That(TextNavigation.WordRange(text, 4), Is.EqualTo((4, 6)));
                Assert.That(TextNavigation.WordRange(text, 2), Is.EqualTo((0, 2)));
            });
        }

        [Test]
        public void TestNavigationWithLoneCr()
        {
            const string text = "ab\rcd";

            Assert.Multiple(() =>
            {
                Assert.That(TextNavigation.NextElement(text, 2), Is.EqualTo(3));
                Assert.That(TextNavigation.PrevElement(text, 3), Is.EqualTo(2));
                Assert.That(TextNavigation.NextWord(text, 0), Is.EqualTo(3));
            });
        }
    }
}