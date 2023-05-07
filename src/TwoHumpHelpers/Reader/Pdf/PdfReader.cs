using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.ReadingOrderDetector;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Outline;
using UglyToad.PdfPig.Tokens;

namespace TwoHumpHelpers.Reader.Pdf
{
    public class PdfReader : IDisposable
    {
        private readonly PdfDocument _document;
        private readonly Regex _simpleNumberRegex = new(@"^\d+[,]?$", RegexOptions.Compiled);
        private readonly Regex _numberRangeRegex = new(@"^(\d+)(\-|–|—)(\d+)[,]?$", RegexOptions.Compiled);
        private readonly Regex _romanNumberRegex = new Regex(@"^(M{0,3})?(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})[,]?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly List<string> _indexPageKeywords = new() { "Index", "Stichwortverzeichnis" };

        private PdfReader(string filePath)
        {
            _document = PdfDocument.Open(filePath);
        }

        public static PdfReader Open(string filePath)
        {
            var reader = new PdfReader(filePath);
            return reader;
        }

        public IEnumerable<PdfPage> GetContent()
        {
            var indexEntriesByPage = Enumerable.Empty<IndexEntry>().SelectMany(e => e.Pages.Select(p => (p, e))).ToLookup(x => x.p, x => x.e);
            var bookmarkNodes = new List<Chapter>();

            if (_document.TryGetBookmarks(out var bookmarks))
            {
                bookmarkNodes = bookmarks
                    .GetNodes()
                    .Where(x => x is DocumentBookmarkNode)
                    .Cast<DocumentBookmarkNode>()
                    .Select(x => new Chapter(x.Title, x.PageNumber))
                    .OrderBy(x => x.Page)
                    .ToList();

                if (TryParseIndex(bookmarks, out var index))
                {
                    indexEntriesByPage = index.Entries.SelectMany(e => e.Pages.Select(p => (p, e))).ToLookup(x => x.p, x => x.e);
                }
            }

            var pageOffset = 0;

            if (_document.Structure.Catalog.CatalogDictionary.ContainsKey(NameToken.PageLabels))
            {
                // It is possible that the PDF starts with front matter pages like roman letters.
                // The index pages may not fit to the real page number.

                if (TryGetPageOffset(out var offset))
                {
                    pageOffset = offset;
                }
            }

            foreach (var page in _document.GetPages())
            {
                var logicalPageNumber = page.Number - pageOffset;
                var currentChapter = bookmarkNodes.TakeWhile(c => c.Page <= page.Number).LastOrDefault();
                var currentIndexEntries = indexEntriesByPage[logicalPageNumber].ToArray();
                var words = GetWordsInReadingOrder(page).Select(x => x.Text).ToArray();

                yield return new PdfPage(page.Number, currentChapter, currentIndexEntries, words);
            }
        }

        private bool TryGetPageOffset(out int pageOffset)
        {
            pageOffset = 0;

            if (_document.NumberOfPages == 1)
            {
                return true;
            }

            var assumptions = new List<int>();

            foreach (var page in _document.GetPages().Take(60))
            {
                var lastWord = page.GetWords().LastOrDefault();
                if (lastWord == null)
                {
                    continue;
                }

                if (!int.TryParse(lastWord.Text, out var pageNumber))
                {
                    continue;
                }

                assumptions.Add(page.Number - pageNumber);
            }

            if (!assumptions.Any())
            {
                return false;
            }

            pageOffset = assumptions
                .GroupBy(x => x)
                .Select(g => (Offset: g.Key, Count: g.Count()))
                .MaxBy(x => x.Count)
                .Offset;

            return true;
        }

        private bool TryParseIndex(Bookmarks bookmarks, [NotNullWhen(true)] out Index? index)
        {
            

            index = null;

            var indexFound = false;
            var indexLevel = 0;
            var indexStartPage = -1;
            var indexEndPage = -1;

            foreach (var bookmarkNode in bookmarks.GetNodes())
            {
                if (_indexPageKeywords.Any(keyword => string.Equals(bookmarkNode.Title, keyword)) && bookmarkNode is DocumentBookmarkNode indexBookmark)
                {
                    indexFound = true;
                    indexLevel = indexBookmark.Level;
                    indexStartPage = indexBookmark.PageNumber;
                    continue;
                }

                if (!indexFound)
                {
                    continue;
                }

                if (bookmarkNode.Level != indexLevel || bookmarkNode is not DocumentBookmarkNode nextChapter)
                {
                    continue;
                }

                indexEndPage = nextChapter.PageNumber;
                break;
            }

            if (!indexFound)
            {
                // not found by bookmarks, but maybe it exists
                var pagesWithIndexKeyword = _document.GetPages().TakeLast(15).Where(p =>
                    _indexPageKeywords.Any(keyword => p.GetWords().Any(w => string.Equals(w.Text, keyword))));

                foreach (var page in pagesWithIndexKeyword)
                {
                    var count = page.GetWords().Count(w => Regex.IsMatch(w.Text, @"\d+"));
                    if (count >= 20)
                    {
                        var wordsStartingWithA = page.GetWords().Count(w => w.Text.StartsWith("A", StringComparison.InvariantCulture));
                        if (wordsStartingWithA >= 10)
                        {
                            // ok, let's assume this is an index page
                            indexFound = true;
                            indexStartPage = page.Number;
                        }
                    }
                }
            }

            if (!indexFound || indexStartPage < 0)
            {
                return false;
            }

            var indexPageCount = indexEndPage == -1
                ? _document.NumberOfPages - indexStartPage +1
                : indexEndPage - indexStartPage;

            var allEntries = new List<IndexEntry>();

            var indexPages = Enumerable.Range(indexStartPage, indexPageCount).ToArray();

            foreach (var pageNo in indexPages)
            {
                var page = _document.GetPage(pageNo);
                var words = GetWordsInReadingOrder(page);
                var entries = ParseIndexEntries(words);
                allEntries.AddRange(entries);
            }

            index = new Index(indexPages, allEntries.ToArray());
            return true;
        }

        private IEnumerable<Word> GetWordsInReadingOrder(Page page)
        {
            var words = page.GetWords(NearestNeighbourWordExtractor.Instance).ToList();

            var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);

            var unsupervisedReadingOrderDetector = new UnsupervisedReadingOrderDetector(10);
            var orderedBlocks = unsupervisedReadingOrderDetector.Get(blocks);

            foreach (var block in orderedBlocks)
            {
                var wordsList = words.Where(x => x.BoundingBox.IntersectsWith(block.BoundingBox)).ToList();
                foreach (var word in wordsList.Where(word => !string.IsNullOrWhiteSpace(word.Text)))
                {
                    yield return word;
                }
            }
        }

        IEnumerable<IndexEntry> ParseIndexEntries(IEnumerable<Word> words)
        {
            var indexEntries = new List<IndexEntry>();
            var currentChunk = new List<string>();

            foreach (var word in words)
            {
                var trimmedText = word.Text.Trim('.').TrimEnd(',', ' ', '\t');

                if (IsPageNumber(trimmedText))
                {
                    if (!currentChunk.Any() && int.TryParse(trimmedText, out _))
                    {
                        // skip the page number of the index page
                        continue;
                    }

                    currentChunk.Add(trimmedText);
                }
                else if (IsFrontMatter(trimmedText) && !(currentChunk.Any() && int.TryParse(currentChunk[^1], out _)))
                {
                    currentChunk.Add(trimmedText);
                }
                else
                {
                    if (currentChunk.Count > 0)
                    {
                        var entry = ProcessChunk(currentChunk);
                        if (!entry.IsEmpty)
                        {
                            indexEntries.Add(entry);
                            currentChunk.Clear();
                        }
                    }

                    if (!currentChunk.Any() && _indexPageKeywords.Contains(word.Text))
                    {
                        // skip the index page header
                        continue;
                    }

                    if (!currentChunk.Any() && word.Text.Length == 1 && char.IsUpper(word.Text[0]))
                    {
                        // An index is often grouped by capital letters.
                        continue;
                    }

                    currentChunk.Add(word.Text);
                }
            }

            if (currentChunk.Count > 0)
            {
                var lastEntry = ProcessChunk(currentChunk);
                if (!lastEntry.IsEmpty)
                {
                    indexEntries.Add(lastEntry);
                }
            }

            return indexEntries;
        }

        private bool IsPageNumber(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (_simpleNumberRegex.IsMatch(text))
            {
                return true;
            }
            
            if (_numberRangeRegex.IsMatch(text))
            {
                return true;
            }
            
            return false;
        }

        private bool IsFrontMatter(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            if (_romanNumberRegex.IsMatch(text))
            {
                return true;
            }

            return false;
        }

        IndexEntry ProcessChunk(List<string> chunk)
        {
            var titleWords = new List<string>();
            var pages = new List<int>();
            var frontMatterPages = new List<string>();

            foreach (var text in chunk)
            {
                if (TryParseNumber(text, out var pageNumber))
                {
                    pages.Add(pageNumber);
                }
                else if (TryParseNumberRange(text, out var startRange, out var endRange))
                {
                    pages.AddRange(Enumerable.Range(startRange, endRange - startRange + 1));
                }
                else if (TryParseRomanNumber(text, out var frontMatterPage))
                {
                    frontMatterPages.Add(frontMatterPage);
                }
                else
                {
                    titleWords.Add(text);
                }
            }

            var title = string.Join(" ", titleWords).TrimEnd('.', ' ', '\t');
            return new IndexEntry(title, pages.ToArray(), frontMatterPages.ToArray());
        }

        bool TryParseNumber(string text, out int result)
        {
            result = 0;

            if (_simpleNumberRegex.IsMatch(text))
            {
                // Remove the optional comma from the end of the input string
                text = text.TrimEnd(',');

                if (int.TryParse(text, out int number))
                {
                    result = number;
                    return true;
                }
            }

            return false;
        }

        bool TryParseNumberRange(string text, out int start, out int end)
        {
            start = 0;
            end = 0;

            var match = _numberRangeRegex.Match(text);
            if (!match.Success)
            {
                return false;
            }

            // Extract the start and end numbers from the captured groups
            start = int.Parse(match.Groups[1].Value);
            end = int.Parse(match.Groups[3].Value);
            return true;
        }

        bool TryParseRomanNumber(string text, out string result)
        {
            result = "";

            if (!_romanNumberRegex.IsMatch(text))
            {
                return false;
            }

            // Remove the optional comma from the end of the input string
            text = text.TrimEnd(',');

            result = text.ToUpper();
            return true;
        }

        private record Index(int[] IndexPages, IndexEntry[] Entries);

        public void Dispose()
        {
            _document?.Dispose();
        }
    }
}
