using System.Collections.Generic;
using System.Text;

namespace CardMesh
{
    /// <summary>
    /// Inserts soft hyphens (U+00AD) into long Russian words so TextMeshPro can wrap
    /// "Электричество" as "Электри-/чество" instead of shrinking it to an unreadable size
    /// or breaking it at a random letter. TMP shows the hyphen only where it breaks a line.
    /// Rules: P. Khristov's algorithm as modified by Dymchenko and Varsanofiev
    /// (x = й/ь/ъ, g = vowel, s = consonant).
    /// </summary>
    public static class RussianHyphenation
    {
        private const char SoftHyphen = '\u00AD';
        private const int MinWordLength = 7;
        private const float MinVisualLength = 9f;
        private const string WideLetters = "шщмжыюфд";
        private const int MinPart = 2;

        private static readonly (string pattern, int cut)[] Rules =
        {
            ("xgg", 1), ("xgs", 1), ("xsg", 1), ("xss", 1),
            ("gssssg", 3), ("gsssg", 3), ("sgsg", 2), ("gssg", 2),
            ("sggg", 2), ("sggs", 2),
        };

        private static readonly Dictionary<string, string> Cache = new();

        public static string Hyphenate(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf(SoftHyphen) >= 0)
            {
                return text;
            }

            if (Cache.TryGetValue(text, out string cached))
            {
                return cached;
            }

            StringBuilder result = new(text.Length + 8);
            int start = 0;
            for (int i = 0; i <= text.Length; i++)
            {
                if (i == text.Length || !char.IsLetter(text[i]))
                {
                    if (i > start)
                    {
                        result.Append(HyphenateWord(text.Substring(start, i - start)));
                    }

                    if (i < text.Length)
                    {
                        result.Append(text[i]);
                    }

                    start = i + 1;
                }
            }

            string value = result.ToString();
            Cache[text] = value;
            return value;
        }

        // Wide Cyrillic letters take ~1.3 average widths, so "Мормышка" needs breaking
        // while the same-length "Проходка" still fits on one line.
        private static float VisualLength(string word)
        {
            float length = 0f;
            foreach (char c in word)
            {
                length += WideLetters.IndexOf(char.ToLowerInvariant(c)) >= 0 ? 1.3f : 1f;
            }

            return length;
        }

        private static string HyphenateWord(string word)
        {
            if (word.Length < MinWordLength || VisualLength(word) < MinVisualLength)
            {
                return word;
            }

            char[] classes = new char[word.Length];
            for (int i = 0; i < word.Length; i++)
            {
                char c = char.ToLowerInvariant(word[i]);
                classes[i] = "йьъ".IndexOf(c) >= 0 ? 'x'
                    : "аеёиоуыэюя".IndexOf(c) >= 0 ? 'g'
                    : "бвгджзклмнпрстфхцчшщ".IndexOf(c) >= 0 ? 's'
                    : '?';
            }

            string pattern = new(classes);
            SortedSet<int> cuts = new();
            foreach ((string rule, int cut) in Rules)
            {
                int index = pattern.IndexOf(rule, System.StringComparison.Ordinal);
                while (index >= 0)
                {
                    int position = index + cut;
                    if (position >= MinPart && position <= word.Length - MinPart)
                    {
                        cuts.Add(position);
                    }

                    index = pattern.IndexOf(rule, index + 1, System.StringComparison.Ordinal);
                }
            }

            if (cuts.Count == 0)
            {
                return word;
            }

            StringBuilder builder = new(word.Length + cuts.Count);
            for (int i = 0; i < word.Length; i++)
            {
                if (cuts.Contains(i))
                {
                    builder.Append(SoftHyphen);
                }

                builder.Append(word[i]);
            }

            return builder.ToString();
        }
    }
}
