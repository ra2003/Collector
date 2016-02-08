﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Collector.Utility.DOM;

namespace Collector.Services
{
    public class Articles : Service
    {

        #region "Article Structure"
        public struct AnalyzedArticle
        {
            public int id;
            public string rawHtml;
            public string url;
            public string domain;
            public string pageTitle;
            public string title;
            public string summary;
            public int relevance;
            public int importance;
            public bool fiction;
            public List<DomElement> elements;
            public AnalyzedTags tags;
            public List<AnalyzedTag> tagNames;
            public List<AnalyzedParentIndex> parentIndexes;
            public List<AnalyzedWord> words;
            public List<AnalyzedPhrase> phrases;
            public List<ArticleSubject> subjects;
            public List<int> body;
            public List<DomElement> bodyElements;
            public List<string> sentences;
            public AnalyzedAuthor author;
            public DateTime publishDate;
            public List<AnalyzedPerson> people;
        }

        public struct AnalyzedTags
        {
            public List<AnalyzedText> text;
            public List<int> anchorLinks;
            public List<int> headers;
        }

        public struct AnalyzedTag
        {
            public string name;
            public int count;
            public int[] index;
        }

        public struct AnalyzedText
        {
            public int index;
            public List<AnalyzedWordInText> words;
            public enumTextType type;
            public List<PossibleTextType> possibleTypes;
        }

        public struct AnalyzedWord
        {
            public int id;
            public string word;
            public int count;
            public int importance;
            public enumWordType type;
            public bool apostrophe;
        }

        public struct AnalyzedPhrase
        {
            public string phrase;
            public int[] words;
            public int count;
        }

        public struct AnalyzedWordInText
        {
            public string word;
            public AnalyzedWord[] relations;
            public int index;
        }

        public struct AnalyzedImage
        {
            public string url;
            public int relavance;
        }

        public struct AnalyzedAuthor
        {
            public string name;
            public enumAuthorSex sex;
        }

        public struct AnalyzedFile
        {
            public string filename;
            public string fileType;
        }

        public struct AnalyzedParentIndex
        {
            public List<int> elements;
            public int index;
            public int textLength;
        }

        public struct AnalyzedElementCount
        {
            public int index;
            public int count;
        }

        public struct PossibleTextType
        {
            public enumTextType type;
            public int count;
        }

        public struct ArticleSubject
        {
            public int id;
            public int parentId;
            public string title;
            public enumWordType type;
            public string[] breadcrumb;
            public int[] hierarchy;
            public List<int> parentIndexes;
            public int count;
            public int score;
        }

        public struct AnalyzedPerson
        {
            public string fullName;
            public string firstName;
            public string middleName;
            public string lastName;
            public string surName;
            public int[] references; //word indexes within article words (he, she, his, hers, him, her, he'd, she'd, he's, she's, etc...)
        }

        public enum enumTextType
        {
            mainArticle = 0,
            authorName = 1,
            publishDate = 2,
            comment = 3,
            advertisement = 4,
            linkTitle = 5,
            menuTitle = 6,
            header = 7,
            copyright = 8,
            script = 9,
            useless = 10,
            style = 11,
            anchorLink = 12,
            menuItem = 13
        }

        public enum enumWordType
        {
            none = 0,
            verb = 1,
            adverb = 2,
            noun = 3,
            pronoun = 4,
            adjective = 5,
            article = 6,
            preposition = 7,
            conjunction = 8,
            interjection = 9,
            punctuation = 10
        }

        public enum enumAuthorSex
        {
            female = 0,
            male = 1
        }

        private int textTypesCount = 13;

        #endregion

        #region "Dictionaries"

        private string[] wordSeparators = new string[] { "(", ")", ".", ",", "?", "/", "\\", "|", "!", "\"", "'", ";", ":", "[", "]", "{", "}", "”", "“", "—", "_", "~", "…" };
        private string[] sentenceSeparators = new string[] { "(", ")", ".", ",", "?", "/", "\\", "|", "!", "\"", ";", ":", "[", "]", "{", "}", "”", "“", "—", "_", "~", "…" };

        private string[] scriptSeparators = new string[] { "{", "}", ";", "$", "=", "(", ")" };

        private string[] dateTriggers = new string[] {
            "published","written","posted",
            "january","february","march","april","may","june",
            "july","august","september","october","november","december" };

        private string[] nonSentenceTags = new string[] { "h1", "h2", "h3", "h4", "h5", "h6", "title" };

        private string[] badTags = new string[]  {
            "applet", "area", "audio", "canvas", "dialog", "small",
            "embed", "footer", "iframe", "input", "label", "nav",
            "object", "option", "s", "script", "style", "textarea",
            "video" };

        private string[] badArticleTags = new string[]  {
            "applet", "area", "audio", "canvas", "dialog", "small", "nav",
            "embed", "footer", "iframe", "input", "label", "nav", "header", "head",
            "object", "option", "s", "script", "style", "title", "textarea",
            "video", "form", "figure", "figcaption","noscript" };

        private string[] badAttributes = new string[] { "id" };

        private string[] badClasses = new string[] {
            "head", "social", "side", "advert", "menu", "comment", "tag", "keyword",
            "nav", "logo", "list", "link", "search", "form", "topic", "feature",
            "filter", "categor", "bread", "credit", "foot", "disqus", "callout",
            "graphic", "image", "photo", "addthis", "tool", "separat",
            "related", "ad-", "item", "return", "mobile", "home", "about", "hidden",
            "semantic"}; 

        private string[] badPhotoCredits = new string[] { "photo", "courtesy", "by", "copyright" };

        private string[] badMenu = new string[] { "previous", "next", "post", "posts", "entry", "entries", "article", "articles", "more", "back", "go", "view", "about", "home", "blog" };

        private string[] badTrailing = new string[] { "additional", "resources", "notes", "comment" };

        private string[] badChars = new string[] { "|", ":", "{", "}", "[", "]" };

        private string[] domainSuffixes = new string[] { "com", "net", "org", "biz" };

        #endregion

        public Articles(Core CollectorCore, string[] paths):base(CollectorCore, paths)
        {
        }

        #region "Analyze"
        public AnalyzedArticle Analyze(string url, string content = "")
        {
            AnalyzedArticle analyzed = new AnalyzedArticle();
            analyzed.tags = new AnalyzedTags();
            analyzed.tags.text = new List<AnalyzedText>();
            analyzed.author = new AnalyzedAuthor();
            analyzed.body = new List<int>();
            analyzed.phrases = new List<AnalyzedPhrase>();
            analyzed.subjects = new List<ArticleSubject>();
            analyzed.url = url;
            analyzed.domain = S.Util.Str.GetDomainName(url);
            analyzed.title = analyzed.pageTitle = analyzed.summary = "";
            analyzed.relevance = analyzed.importance = 0;
            analyzed.fiction = true;
            
            analyzed.publishDate = DateTime.Now;




            //STEP #1 : Get Web Page Contents via disk cache or url download //////////////////////////////////////////////////////////////////////
            string htm = "";
            bool isCached = false;
            SqlReader reader;

            if(ArticleExist(url) == true)
            {
                //check if article is already cached
                reader = new SqlReader();
                reader.ReadFromSqlClient(S.Sql.ExecuteReader("EXEC GetArticleByUrl @url='" + url + "'"));
                if(reader.Read() == true)
                {
                    var letter = reader.Get("domain").Substring(0, 2);
                    var path = S.Server.MapPath("content/articles/" + letter + "/" + reader.Get("domain") + "/");
                    analyzed.id = reader.GetInt("articleId");
                    if (File.Exists(path + analyzed.id + ".html"))
                    {
                        htm = File.ReadAllText(path + analyzed.id + ".html");
                    }

                    //check for cached object in json file format
                    if (File.Exists(path + analyzed.id + ".json"))
                    {
                        var cached = (AnalyzedArticle)S.Util.Serializer.OpenFromFile(typeof(AnalyzedArticle), path + analyzed.id + ".json");
                        cached = new AnalyzedArticle();
                        isCached = true;
                    }
                    if(htm == "")
                    {
                        //file was empty. Final resort, download from url
                        htm = S.Util.Web.Download(url, true);
                        File.WriteAllText(S.Server.MapPath("/wwwroot/tests/new.html"), htm);
                    }
                }
            }
            else if(content == "")
            {
                //download html from url
                if (url.IndexOf("local") == 0) //!!! offline debug only !!!
                {
                    htm = File.ReadAllText(S.Server.MapPath("wwwroot/tests/" + url.Replace("local ", "") + ".html"));
                    analyzed.url = "/tests/" + url.Replace("local ", "") + ".html";
                }
                else
                {
                    htm = S.Util.Web.Download(url, true);
                    File.WriteAllText(S.Server.MapPath("/wwwroot/tests/new.html"), htm);
                }
            }else if(content.Length > 0)
            {
                //use attached content string
                htm = content.ToString();
            }




            //STEP #2 : Analyze Raw HTML //////////////////////////////////////////////////////////////////////

            //save raw html
            analyzed.rawHtml = htm;

            //make sure the html isn't just a 404 error or empty page or some crap
            var tmp = htm.ToLower();
            if (tmp.Split(new string[] { "error" }, StringSplitOptions.None).Length >= 10)
            {
            }
            if (tmp.Length <= 200)
            {
                if(tmp.IndexOf("error") >= 0)
                {
                    return analyzed;
                }
                if (tmp.IndexOf("fail") >= 0)
                {
                    return analyzed;
                }
                if (tmp.IndexOf("exit") >= 0)
                {
                    return analyzed;
                }
                if (tmp.IndexOf("dump") >= 0)
                {
                    return analyzed;
                }
            }
            //remove spaces, line breaks, & tabs
            htm = htm.Replace("\n", " ").Replace("\r"," ").Replace("  ", " ").Replace("  ", " ").Replace("	","").Replace(" "," ");




            //STEP #3 : Create DOM Hierarchy from HTML Tags //////////////////////////////////////////////////////////////////////

            var parsed = new Parser(S, htm);
            var tagNames = new List<AnalyzedTag>();

            //sort elements into different lists
            var textElements = new List<DomElement>();
            var anchorElements = new List<DomElement>();
            var headerElements = new List<DomElement>();
            var imgElements = new List<DomElement>();
            var parentIndexes = new List<AnalyzedParentIndex>();
            DomElement traverseElement;

            foreach (DomElement element in parsed.Elements)
            {
                switch(element.tagName.ToLower())
                {
                    case "#text":
                        //sort text elements
                        textElements.Add(element);
                        traverseElement = element;
                        //add element's parent indexes to list
                        do
                        {
                            var parentIndex = parentIndexes.FindIndex(x => x.index == traverseElement.parent);
                            if (parentIndex >= 0)
                            {
                                //update existing parent index
                                var parent = parentIndexes[parentIndex];
                                parent.elements.Add(element.index);
                                parent.textLength += element.text.Length;
                                parentIndexes[parentIndex] = parent;
                            }
                            else
                            {
                                //create new parent index
                                var parent = new AnalyzedParentIndex();
                                parent.index = traverseElement.parent;
                                parent.elements = new List<int>();
                                parent.elements.Add(element.index);
                                parent.textLength = element.text.Length;
                                parentIndexes.Add(parent);
                            }
                            if(traverseElement.parent > -1)
                            {
                                //get next parent element
                                traverseElement = parsed.Elements[traverseElement.parent];
                            }
                            else { break; }
                        } while (traverseElement.parent > -1);
                        break;
                    case "a":
                        //sort anchor links
                        anchorElements.Add(element);
                        break;
                    case "h1": case "h2": case "h3": case "h4": case "h5": case "h6":
                        headerElements.Add(element);
                        break;
                    case "title":
                        if(analyzed.title == "")
                        {
                            analyzed.pageTitle = analyzed.title = parsed.Elements[element.index + 1].text.Trim();

                            //check for 404 error
                            if(analyzed.title.IndexOf("404") >= 0)
                            {
                                return analyzed;
                            }
                        }
                        break;
                    case "img":
                        imgElements.Add(element);
                        break;
                }

                //add new tag to list or incriment count of existing tag in list
                var tagIndex = tagNames.FindIndex(x => x.name == element.tagName);
                if(tagIndex >= 0)
                {
                    //incriment tag name count
                    var t = tagNames[tagIndex];
                    t.count+=1;
                    tagNames[tagIndex] = t;
                }
                else
                {
                    //add new tag to list
                    var t = new AnalyzedTag();
                    t.name = element.tagName;
                    t.count = 1;
                    tagNames.Add(t);
                }
            }




            //STEP #4 : Sort Element Lists & Add to Analyzed Object //////////////////////////////////////////////////////////////////////

            //sort lists
            var textOrdered = textElements.OrderBy(p => p.text.Length * -1).ToList();
            var headersOrdered = headerElements.OrderBy(p => p.tagName).ToList();
            var parentIndexesOrdered = parentIndexes.OrderBy(p => (p.elements.Count * p.textLength) * -1).ToList();
            var tagNamesOrdered = tagNames.OrderBy(t => t.count * -1).ToList();

            //set up elements
            analyzed.elements = parsed.Elements;
            analyzed.tagNames = tagNamesOrdered;

            //setup analyzed headers
            var headers = new List<int>();
            foreach (DomElement header in headersOrdered)
            {
                headers.Add(header.index);
            }
            analyzed.tags.headers = headers;

            //setup analyzed links
            var anchors = new List<int>();
            foreach (DomElement anchor in anchorElements)
            {
                anchors.Add(anchor.index);
            }
            analyzed.tags.anchorLinks = anchors;




            //STEP #5 : Analyze Sorted Text ///////////////////////////////////////////////////////////////////////////////////////////////////

            string[] texts;
            AnalyzedWord word;
            AnalyzedWordInText wordIn;
            var index = 0;
            var i = -1;
            var allWords = new List<AnalyzedWord>();
            var words = new List<AnalyzedWord>();
            foreach (DomElement element in textOrdered)
            {
                
                var wordsInText = new List<AnalyzedWordInText>();
                var newText = new AnalyzedText();
                newText.index = element.index;
                //separate all words & symbols with a space
                texts = GetWords(element.text);
                for (var x = 0; x < texts.Length; x++)
                {
                    if(texts[x].Trim() == "") { continue; }
                    wordIn = new AnalyzedWordInText();
                    wordIn.word = texts[x];
                    wordIn.index = x;

                    //add word to all words list
                    index = allWords.FindIndex(w => w.word == texts[x]);
                    if (index >= 0)
                    {
                        //incriment word count
                        var w = allWords[index];
                        w.count+=1;
                        allWords[index] = w;
                    }
                    else
                    {
                        //add new word to list
                        word = new AnalyzedWord();
                        word.word = texts[x];
                        word.count = 1;
                        allWords.Add(word);
                    }

                    //set reference to analyzed word
                    wordsInText.Add(wordIn);
                }
                newText.words = wordsInText;

                //check all words for patterns to determine
                //what type of text is in this element
                i = -1;
                var sortedAllWords = allWords.OrderBy(a => a.count * -1).ToList();
                var len = wordsInText.Count;
                var possibleTypes = new int[textTypesCount+1];

                foreach (AnalyzedWordInText aword in wordsInText)
                {
                    i++;
                    var w = aword.word.ToLower();
                    if (CheckWordForPossibleTypes(parsed, element, w, possibleTypes, len) == false)
                    {
                        break;
                    }
                }
                if(element.text.Length >= 4)
                {
                    if (element.text.Trim().IndexOf("<!--") == 0)
                    {
                        possibleTypes[(int)enumTextType.useless] = 10000;
                    }
                }
                if (element.hierarchyTags.Contains("ul") && element.hierarchyTags.Contains("li"))
                {
                    //menu item
                    possibleTypes[(int)enumTextType.menuItem] += 100;
                }
                else if (element.hierarchyTags.Contains("a"))
                {
                    //anchor link
                    possibleTypes[(int)enumTextType.anchorLink] += 100;
                }

                //sort possible types by count
                var possTypes = new List<PossibleTextType>();
                var e = 0;
                for (e = 0; e <= textTypesCount; e++)
                {
                    var newPoss = new PossibleTextType();
                    newPoss.type = (enumTextType)e;
                    newPoss.count = possibleTypes[e];
                    possTypes.Add(newPoss);
                }
                var sortedPossTypes = possTypes.OrderBy(p => p.count * -1).ToList();
                newText.possibleTypes = sortedPossTypes;

                //figure out dominant type from possible types
                for (e = 0; e < sortedPossTypes.Count; e++)
                {
                    var t = sortedPossTypes[e];
                    var found = false;
                    if (t.count > 1)
                    {
                        if(t.type == enumTextType.script)
                        { 
                            if(t.count >= 5){ found = true; }
                        }
                        else if (t.type == enumTextType.style)
                        {
                            if (t.count >= 5) { found = true; }
                        }
                        else if (t.type == enumTextType.useless)
                        {
                            if (t.count >= 5) { found = true; }
                        }
                        else if (t.type == enumTextType.copyright)
                        {
                            if (t.count >= 2) { found = true; }
                        }
                        else if (t.type == enumTextType.publishDate)
                        {
                            if (t.count >= 5) {
                                found = true;
                            }
                        }
                        else if(t.type == enumTextType.anchorLink)
                        {
                            found = true;
                        }
                        else if (t.type == enumTextType.menuItem)
                        {
                            found = true;
                        }
                        if(t.type != enumTextType.script)
                        {
                            //script type takes presidence over other types
                            if(e < sortedPossTypes.Count - 1)
                            {
                                if(sortedPossTypes[e+1].type == enumTextType.script)
                                {
                                    if(sortedPossTypes[e + 1].count >= 5)
                                    {
                                        found = false;
                                    }
                                }
                            }
                        }

                    }
                    if(found == true)
                    {
                        newText.type = t.type;
                        break;
                    }
                }
                
                //add text to analyzed results
                analyzed.tags.text.Add(newText);
            }




            //STEP #6 : Get Article Body from Sorted Text //////////////////////////////////////////////////////////////////////////////////////////

            var pIndexes = new List<AnalyzedElementCount>();
            i = 0;
            foreach (var a in analyzed.tags.text)
            {
                
                if(a.type == enumTextType.mainArticle)
                {
                    //find most relevant parent indexes shared by all article text elements
                    i++;

                    //limit to large article text
                    if (a.words.Count < 20 && i > 2) { i--;  break; }

                    //limit to top 5 article text
                    if (i > 10) { i--;  break; } 

                    foreach (int indx in parsed.Elements[a.index].hierarchyIndexes)
                    {
                        var parindex = pIndexes.FindIndex(p => p.index == indx);
                        if (parindex >= 0)
                        {
                            //update count for a parent index
                            var p = pIndexes[parindex];
                            p.count+=1;
                            pIndexes[parindex] = p;
                        }
                        else
                        {
                            //add new parent index
                            var p = new AnalyzedElementCount();
                            p.index = indx;
                            p.count = 1;
                            pIndexes.Add(p);
                        }
                    }
                }
            }
            var sortedArticleParents = pIndexes.OrderBy(p => p.index).OrderBy(p => p.count).Reverse().ToList();

            //determine best parent element that contains the entire article
            var bodyText = new List<int>();
            var uselessText = new[] {
                enumTextType.advertisement,
                enumTextType.comment,
                enumTextType.copyright,
                enumTextType.script,
                enumTextType.style,
                enumTextType.useless
            };

            int parentId;
            var isFound = false;
            var isBad = false;
            var isEnd = false;
            var badIndexes = new List<int>();
            string[] articleText;
            string articletxt = "";
            DomElement hElem;
            DomElement elem;

            for (var x = sortedArticleParents.Count - 1; x >= 0; x--)
            {
                //all elements are a part of this parent element
                //get a list of text elements that are a part of the 
                //parent element
                parentId = sortedArticleParents[x].index;
                isFound = false;
                isBad = false;
                isEnd = false;
                for (var y = parentId + 1; y < parsed.Elements.Count; y++)
                {
                    elem = parsed.Elements[y];
                    if (S.Util.IsEmpty(elem)) { continue; }
                    if (elem.hierarchyIndexes.Contains(parentId))
                    {
                        if(elem.hierarchyIndexes.Where(ind => badIndexes.Contains(ind)).Count() > 0) { continue; }
                        if (elem.tagName == "#text")
                        {
                            //element is text & is part of parent index
                            //check if text type is article
                            var textTag = analyzed.tags.text.Find(p => p.index == y);
                            if(!uselessText.Contains(textTag.type))
                            {
                                articletxt = elem.text.ToLower();
                                articleText = GetWords(articletxt);
                                if(elem.className != null)
                                {
                                    elem.className = elem.className.Select(c => c.ToLower()).ToList();
                                }
                                else { elem.className = new List<string>(); }
                                    
                                //check for any text from the article that does not belong,
                                //such as advertisements, side-bars, photo credits, widgets
                                isBad = false;

                                for(var z = elem.hierarchyIndexes.Length - 1; z >= 0; z--)
                                {
                                    //search down the hierarchy DOM tree
                                    hElem = analyzed.elements[elem.hierarchyIndexes[z]];
                                    if(hElem.index == parentId) { break; }
                                        
                                    //check for bad tag names
                                    if(badTags.Contains(hElem.tagName)) { isBad = true; break; }

                                    //check classes for bad element indicators within class names
                                    if (hElem.className.Count > 0)
                                    {
                                        if (hElem.className.Where(c => badClasses.Contains(c)).Count() > 0)
                                        {
                                            isBad = true; break;
                                        }
                                    }
                                }

                                if (articletxt.IndexOf("disqus") >= 0)
                                {
                                    //article comments
                                    isBad = true;
                                    isEnd = true;
                                }

                                if (articleText.Length <= 20)
                                {
                                    if (articleText.Where(t => badPhotoCredits.Contains(t)).Count() >= 2)
                                    {
                                        //photo credits
                                        isBad = true;
                                    }
                                }
                                if(articleText.Length <= 7 && isBad != true)
                                {
                                    if(articleText.Where(t => badMenu.Contains(t)).Count() >= 1)
                                    {
                                        //menu
                                        isBad = true;
                                    }
                                    if(articletxt.IndexOf("additional resources")>=0){
                                        //end of article
                                        isBad = true;
                                        isEnd = true;
                                    }
                                }

                                if(articleText.Length <= 3 && isBad != true)
                                {
                                    if(articleText.Where(t => badChars.Contains(t)).Count() >= 1)
                                    {
                                        //bad characters
                                        isBad = true;
                                    }
                                    if (articleText.Where(t => badTrailing.Contains(t)).Count() >= 1)
                                    {
                                        //bad characters
                                        isBad = true;
                                        isEnd = true;
                                    }
                                }

                                if(isBad == false)
                                {
                                    //finally, add text to article
                                    if (!bodyText.Contains(elem.index))
                                    {
                                        bodyText.Add(elem.index);
                                    }
                                    //clean up text in element
                                    elem.text = S.Util.Str.HtmlDecode(elem.text);
                                }
                            }
                        }
                        else
                        {
                            //element is not text, 
                            //determine if element contains bad content
                            if(elem.className != null)
                            {
                                if (elem.className.Where(c => badClasses.Where(bc => c.IndexOf(bc) >= 0).Count() > 0).Count() > 0)
                                {
                                    badIndexes.Add(elem.index);
                                }
                            }
                            if(elem.attribute != null) { 
                                if (elem.attribute.ContainsKey("id"))
                                {
                                    if (badClasses.Where(bc => elem.attribute["id"].ToLower().IndexOf(bc) >= 0).Count() > 0)
                                    {
                                        badIndexes.Add(elem.index);
                                    }
                                    //if(elem.attribute["id"] != ""){
                                    //    badIndexes.Add(elem.index);
                                    //}
                                    }
                                }

                            if (badArticleTags.Contains(elem.tagName))
                            {
                                badIndexes.Add(elem.index);
                            }


                        }
                    }
                    else
                    {
                        //no longer part of parent id
                        isFound = true;
                        break;
                    }
                    if (isFound == true || isEnd == true) { break; }
                }
                //if(isFound == true || isEnd == true) { break; }
            }

            bodyText.Sort();
            analyzed.body = bodyText;




            //STEP #7 : Analyze all Words & Phrases from Article Body & Separate into Sentences ////////////////////////////////////////////////////////////////////////////////////

            var text = ""; var txt = "";
            var charA = S.Util.Str.Asc("A");
            var charZ = S.Util.Str.Asc("Z");
            var charSymbol1 = 0;
            var charSymbol2 = 64;
            var charSymbol3 = 91;
            var charSymbol4 = 96;
            var charSymbol5 = 123;
            int character;
            var txt1 = "";
            var sentences = new List<string>();
            bool isName = true;
            DomElement domText;
            var domainName = analyzed.domain.Split('.')[0].ToLower();
            var commonWords = GetCommonWords();
            var normalWords = GetNormalWords();
            allWords = new List<AnalyzedWord>();

            //build article text
            for (var x = 0; x < analyzed.body.Count; x++)
            {
                domText = analyzed.elements[analyzed.body[x]];
                if(domText.hierarchyTags.Where(h => nonSentenceTags.Contains(h)).Count() > 0)
                {
                    continue;
                }
                txt = domText.text.Trim();
                text += txt + " ";
            }

            //separate text into sentences
            analyzed.sentences = GetSentences(text);

            // analyze all words in article & get phrases, too
            var phrases = new List<AnalyzedPhrase>();
            var phrasewords = new List<string>();
            List<string[]> phrasesFound;
            List<string> phraseCreated;
            var dbphrases = GetPhrases();
            int bufferedUnknownPhrase = 0;
            int bufferedPhrase = 0;
            bool isUnknownPhraseBuffered = false;
            bool isPhraseBuffered = false;
            bool isNormal = false;
            text = "";
            for (var x = 0; x < analyzed.body.Count; x++)
            {
                domText = analyzed.elements[analyzed.body[x]];
                txt = domText.text.Trim();
                text += txt + " ";
            }
            texts = GetWords(S.Util.Str.HtmlDecode(text));
            for (var x = 0; x < texts.Length; x++)
            {
                txt = texts[x].ToLower().Trim();
                if (txt == "") { continue; }
                if(txt.Length == 1)
                {
                    if (S.Util.Str.CheckChar(txt, true, true, wordSeparators) == false)
                    {
                        continue;
                    }
                }
                isNormal = false;

                texts[x] = texts[x].Trim();

                //add word to all words list
                index = allWords.FindIndex(w => w.word == txt);
                if (index >= 0)
                {
                    //incriment word count
                    word = allWords[index];
                    word.count+=1;
                    allWords[index] = word;
                }
                else
                {
                    //add new word to list
                    word = new AnalyzedWord();
                    word.word = txt;
                    word.count = 1;
                    word.importance = 5;
                    if(txt.Length > 2)
                    {
                        if (txt.Contains("'s") == true || txt.Contains("’s") == true || txt.IndexOf("'") == txt.Length - 1 || txt.IndexOf("’") == txt.Length - 1)
                        {
                            word.word = S.Util.Str.RemoveApostrophe(word.word);
                            word.apostrophe = true;
                        }
                    }


                    //figure out importance (score) of word
                    //capitalized words = 10
                    //people names = 10
                    //is in page title = 10
                    //potentially important = 5
                    //number = 3
                    //common word = 2
                    //symbol = 0
                    if (normalWords.Contains(word.word.ToLower())) {
                        word.importance = 5;
                        isNormal = true;
                    }
                    if (commonWords.Contains(txt) && isNormal == false)
                    {
                        word.importance = 2;
                    }
                    else
                    {
                        //potentially important word
                        character = S.Util.Str.Asc(texts[x].Substring(0, 1));
                        if (character >= charA && character <= charZ) 
                        {
                            //word is a Name
                            isName = true;
                            if(x > 0)
                            {
                                if (texts[x - 1] == ".")
                                {
                                    //word comes after a period, 
                                    //check for a second capitalized word
                                    isName = false;
                                    if(x < texts.Length - 1)
                                    {
                                        character = S.Util.Str.Asc(texts[x+1].Substring(0, 1));
                                        if (character >= charA && character <= charZ)
                                        {
                                            //word is important
                                            isName = true;
                                        }
                                    }
                                    
                                }
                            }
                            if(isName == true)
                            {
                                word.importance = 10;
                                if (txt.IndexOf("'") > 0 || txt.IndexOf("’") > 0)
                                {
                                    //word contains an apostrophe
                                    txt = S.Util.Str.RemoveApostrophe(txt);
                                }
                                index = allWords.FindIndex(w => w.word == txt);
                                if (index < 0)
                                {
                                    word.word = txt;
                                }
                                else
                                {
                                    //skip name (it already exists)
                                    var w = allWords[index];
                                    w.count+=1;
                                    allWords[index] = w;
                                    continue;
                                }
                            } 
                        }
                        else
                        {
                            if (character >= 48 && character <= 57)
                            {
                                //word starts with a number
                                if (S.Util.Str.IsNumeric(texts[x]))
                                {
                                    //word is a number
                                    word.importance = 3;
                                }
                                else
                                {
                                    //word is a symbol
                                    if(isNormal == false) { word.importance = 0; }
                                }
                            }
                            else if (character >= charSymbol1 && character <= charSymbol2)
                            {
                                //word is a character symbol, currency, ID, or unique (unreadable) set of characters
                                word.importance = 0;
                            }
                            else if (character >= charSymbol3 && character <= charSymbol4)
                            {
                                //word is a character symbol, currency, ID, or unique (unreadable) set of characters
                                word.importance = 0;
                            }
                            else if (character >= charSymbol5)
                            {
                                //word is a character symbol, currency, ID, or unique (unreadable) set of characters
                                word.importance = 0;
                            }
                        }
                        
                    }
                    if(word.word == domainName) { word.importance = 2; }
                    

                    allWords.Add(word);
                }
                //find phrases /////////////////////////////////////////////////////////////////////////////////////////////////////
                //guess unknown phrases based on capitalized words
                if(isUnknownPhraseBuffered == false)
                {
                    if (word.importance == 10 && word.apostrophe == false)
                    {
                        phrasewords.Add(word.word);
                        bufferedUnknownPhrase += 1;
                    }
                    else
                    {
                        if(word.apostrophe == true)
                        {
                            phrasewords.Add(word.word);
                            bufferedUnknownPhrase += 1;
                        }
                        if (phrasewords.Count > 1)
                        {
                            var newphrase = new AnalyzedPhrase();
                            newphrase.phrase = S.Util.Str.RemoveApostrophe(string.Join(" ", phrasewords));
                            newphrase.count = 1;
                            index = phrases.FindIndex(p => p.phrase == newphrase.phrase);
                            if (index >= 0)
                            {
                                //phrase already exists
                                newphrase = phrases[index];
                                newphrase.count++;
                                phrases[index] = newphrase;
                            }
                            else
                            {
                                //add new phrase
                                phrases.Add(newphrase);
                            }
                            isUnknownPhraseBuffered = true;
                        }
                        else
                        {
                            bufferedUnknownPhrase = 0;
                            isUnknownPhraseBuffered = false;
                        }
                        phrasewords = new List<string>();
                    }
                }
                

                //find phrases from phrases.json
                if(isPhraseBuffered == false)
                {
                    phrasesFound = dbphrases.Where(p => p[0] == word.word).ToList();
                    if (phrasesFound.Count > 0)
                    {
                        foreach (var phraseFound in phrasesFound)
                        {
                            var e = 0;
                            isBad = false;
                            phraseCreated = new List<string>();
                            foreach (var p in phraseFound)
                            {
                                txt1 = texts[x + e].Trim().ToLower();
                                if (p.IndexOf(txt1) != 0 && txt1.IndexOf(p) != 0)
                                {
                                    isBad = true;
                                    break;
                                }
                                phraseCreated.Add(p); // p or txt1
                                bufferedPhrase += 1;
                                e++;
                            }
                            if (isBad == false)
                            {
                                var newphrase = new AnalyzedPhrase();
                                newphrase.phrase = string.Join(" ", phraseCreated);
                                newphrase.count = 1;
                                index = phrases.FindIndex(p => p.phrase == newphrase.phrase);
                                if (index >= 0)
                                {
                                    //phrase already exists
                                    newphrase = phrases[index];
                                    newphrase.count++;
                                    phrases[index] = newphrase;
                                }
                                else
                                {
                                    //add new phrase
                                    phrases.Add(newphrase);
                                }
                                isPhraseBuffered = true;
                            }
                        }
                    }
                }

                if (isPhraseBuffered == true)
                {
                    if (bufferedPhrase > 0) { bufferedPhrase--; }
                    if (bufferedPhrase == 0) { isPhraseBuffered = false; }
                }

                if (isUnknownPhraseBuffered == true)
                {
                    if (bufferedUnknownPhrase > 0) { bufferedUnknownPhrase--; }
                    if (bufferedUnknownPhrase == 0) { isUnknownPhraseBuffered = false; }
                }


            }// END: analyze all words

            analyzed.phrases = phrases;




            //STEP #8 : Use Potentially Important Words & Phrases to Find Matches in the Database //////////////////////////////////////////////////////////////////////

            var wordlist = new List<string>();
            for(int x = 0;x < allWords.Count; x++)
            {
                word = allWords[x];
                if(word.importance > 3) //2 = common words, 3 = numbers
                {
                    if(word.word.Length >= 2)
                    {
                        wordlist.Add(word.word);
                    }
                }
            }

            AnalyzedPhrase phrase;
            for (int x = 0; x < analyzed.phrases.Count; x++)
            {
                phrase = analyzed.phrases[x];
                    if (phrase.phrase.Length >= 3)
                    {
                        wordlist.Add(phrase.phrase);
                    }
                else { break; }
            }



            //STEP #9 : Find Potential Subjects the Article may belong to based on the Database Word List //////////////////////////////////////////////////////////////////////

            var subjects = new List<ArticleSubject>();
            string subj;
            string[] subjs;
            int subjId = 0;
            reader = new SqlReader();
            reader.ReadFromSqlClient(S.Sql.ExecuteReader("GetWords @words='" + string.Join(",", wordlist.ToArray()) + "'"));
            if (reader.Rows.Count > 0)
            {
                while (reader.Read())
                {
                    index = allWords.FindIndex(w => w.word == reader.Get("word"));
                    if (index >= 0)
                    {
                        word = allWords[index];
                        word.id = reader.GetInt("wordId");
                        allWords[index] = word;

                        //get list of subject IDs that belong to word
                        subj = reader.Get("subjects");
                        if (subj != "")
                        {
                            subjs = subj.Split(',');
                            foreach (string sub in subjs)
                            {
                                if (S.Util.Str.IsNumeric(sub))
                                {
                                    subjId = subjects.FindIndex(s => s.id == int.Parse(sub));
                                    if (subjId >= 0)
                                    {
                                        //subject already exists
                                        var newsub = subjects[subjId];
                                        newsub.count += word.count;
                                        subjects[subjId] = newsub;
                                    }
                                    else
                                    {
                                        var newsub = new ArticleSubject();
                                        newsub.count = word.count;
                                        newsub.id = int.Parse(sub);
                                        subjects.Add(newsub);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //get a list of subjects that this article can be placed under
            var allSubjects = subjects.OrderByDescending(s => s.count).ToList();
            var subjectlist = new List<string>();
            for(int x = 0; x < allSubjects.Count; x++)
            {
                subjectlist.Add(allSubjects[x].id.ToString());
            }

            //get subject details from database
            int subjIndex;
            reader = new SqlReader();
            reader.ReadFromSqlClient(S.Sql.ExecuteReader("EXEC GetSubjects @subjectIds='" + string.Join(",", subjectlist) + "'"));
            if(reader.Rows.Count > 0)
            {
                while (reader.Read())
                {
                    subjIndex = allSubjects.FindIndex(s => s.id == reader.GetInt("subjectId"));
                    if(subjIndex >= 0)
                    {
                        var newsub = allSubjects[subjIndex];
                        if(reader.Get("hierarchy") != "")
                        {
                            newsub.hierarchy = reader.Get("hierarchy").Split('>').Select(s => int.Parse(s)).ToArray();
                        }
                        if (reader.Get("breadcrumb") != "")
                        {
                            newsub.breadcrumb = reader.Get("breadcrumb").Split('>');
                        }
                        newsub.parentId = reader.GetInt("parentid");
                        newsub.title = reader.Get("title");
                        allSubjects[subjIndex] = newsub;
                    }
                }
            }

            //calculate subject score based on (total hierarchial words * hierarchy-depth)
            var subjWordCount = 0;
            var subjBreadcrumb = "";
            var parentBreadcrumb = "";
            for (int x = 0; x < allSubjects.Count; x++)
            {
                var newsub = allSubjects[x];
                newsub.parentIndexes = new List<int>();
                subjWordCount = allSubjects[x].count;
                subjBreadcrumb = string.Join(" > ", allSubjects[x].breadcrumb);
                
                for (int y = 0; y < allSubjects.Count; y++)
                {
                    if(x == y) { continue; }
                    parentBreadcrumb = string.Join(" > ", allSubjects[y].breadcrumb);
                    if (parentBreadcrumb != "")
                    {
                        parentBreadcrumb += " > " + allSubjects[y].title;
                    }
                    else
                    {
                        parentBreadcrumb = allSubjects[y].title;
                    }
                    if (subjBreadcrumb.IndexOf(parentBreadcrumb) >= 0)
                    {
                        subjWordCount += allSubjects[y].count;
                        newsub.parentIndexes.Add(allSubjects[y].id);
                    }
                }
                
                var hl = newsub.breadcrumb.Length + 1;
                newsub.score = subjWordCount * hl;
                allSubjects[x] = newsub;
            }

            //add all subjects to the results, order by popularity & hierarchy
            analyzed.subjects = allSubjects.OrderBy(s => (999999 - s.score) + ">" + (s.breadcrumb != null ? string.Join(">", s.breadcrumb) : "")).ToList();




            //STEP #10 : Add all Article Words to the Analyzed Object, Sorted by ID & Importance //////////////////////////////////////////////////////////////////////

            analyzed.words = allWords.OrderByDescending(w => w.count).OrderByDescending(w => w.id > 0).OrderByDescending(w => w.importance).ToList();




            //STEP #11 : Beautify the Raw HTML (if neccessary) //////////////////////////////////////////////////////////////////////

            if (htm.IndexOf("/r") < 0)
            {
                //beautify the html since it is all on one line
                htm = "";
                var htms = new List<string>();
                var htmelem = "";
                var tabs = "";
                foreach(var el in analyzed.elements)
                {
                    if(el.isClosing == true && el.isSelfClosing == false)
                    {
                        //closing tag
                        htmelem = "</" + el.tagName + ">";
                    }
                    else{
                        if(el.tagName == "#text")
                        {
                            htmelem = el.text;
                        }
                        else
                        {
                            htmelem = "<" + el.tagName;
                            if (el.attribute.Count > 0)
                            {
                                foreach (var attr in el.attribute)
                                {
                                    htmelem += " " + attr.Key + "=\"" + attr.Value + "\"";
                                }
                            }
                            if (el.isSelfClosing == true)
                            {
                                htmelem += "/>";
                            }
                            else
                            {
                                htmelem += ">" + el.text;
                            }
                        }
                        
                        
                    }
                    tabs = "";
                    for(var x = 1; x <= el.hierarchyIndexes.Length; x++)
                    {
                        tabs += "    ";
                    }
                    htms.Add(tabs + htmelem);
                }
                htm = string.Join("\n", htms);
                analyzed.rawHtml = htm;
            }

            return analyzed;
        }

        public List<AnalyzedArticle> AnalyzeArticles(List<Scavenger.ScavangedContent> content)
        {
            var articles = new List<AnalyzedArticle>();
            foreach(Scavenger.ScavangedContent doc in content)
            {
                articles.Add(Analyze(doc.url, doc.html));
            }
            return articles;
        }

        private bool CheckWordForPossibleTypes(Parser parsed, DomElement element, string w, int[] possibleTypes, int totalWords)
        {
            if (scriptSeparators.Contains(w))
            {
                if(element.index > 0)
                {
                    if (parsed.Elements[element.index - 1].tagName == "script")
                    {
                        possibleTypes[(int)enumTextType.script] += 5;
                        return false;
                    }
                    if (parsed.Elements[element.index - 1].tagName == "style")
                    {
                        possibleTypes[(int)enumTextType.style] += 5;
                        return false;
                    }
                }
                

                
            }
            else if (w == "copyright")
            {
                possibleTypes[(int)enumTextType.copyright] += 1;
            }
            else if (w.IndexOf("&copy") >= 0 || w == "©")
            {
                possibleTypes[(int)enumTextType.copyright] += 2;
            }
            else if (w == "rights" || w == "reserved")
            {
                possibleTypes[(int)enumTextType.copyright] += 1;
            }
            else if (dateTriggers.Contains(w))
            {
                if (totalWords < 20)
                {
                    //small group of text has better chance 
                    //of being a publish date
                    possibleTypes[(int)enumTextType.publishDate] += 5;
                }
                else
                {
                    possibleTypes[(int)enumTextType.publishDate] += 1;
                }
            }
            else if (w.IndexOf("advertis") >= 0 || w.IndexOf("sponsor") >= 0)
            {
                if (totalWords < 10)
                {
                    //small group of text has better chance 
                    //of being an advertisement
                    possibleTypes[(int)enumTextType.advertisement] += 5;
                }
                else
                {
                    possibleTypes[(int)enumTextType.advertisement] += 1;
                }
            }
            return true;
        }

        public List<AnalyzedWord> CombineWordLists(List<AnalyzedWord> list1, List<AnalyzedWord> list2)
        {
            List<AnalyzedWord> list;
            List<AnalyzedWord> added;
            if(list1.Count > list2.Count)
            {
                list = list1;
                added = list2;
            }
            else
            {
                list = list2;
                added = list1;
            }

            foreach(var a in added)
            {
                var i = list.FindIndex(c => c.word == a.word);
                if(i >= 0)
                {
                    var word = list[i];
                    word.count += a.count;
                    list[i] = word;
                }
                else
                {
                    //add new word
                    list.Add(a);
                }
            }

            return list;

        }

        public List<string> GetSentences(string text)
        {

            var txt = "";
            var charA = S.Util.Str.Asc("A");
            var charZ = S.Util.Str.Asc("Z");
            var txt1 = "";
            var txt2 = "";
            var txt3 = "";
            int sentenceStart = 0;
            bool foundSentence = false;
            var sentence = "";
            var sentences = new List<string>();
            var modtext = S.Util.Str.replaceAll(
                S.Util.Str.HtmlDecode(
                    text.Replace("&nbsp;", ""))
                    .Replace("... ", "{{p3}}").Replace(".. ", ". ").Replace(".. ", ". ").Replace("{{p3}}", "... ")
                , "=+={1}=+=", sentenceSeparators);
            var texts = modtext.Replace("=+==+=", "=+=").Replace("=+==+=", "=+=")
                .Split(new string[] { "=+=" }, StringSplitOptions.RemoveEmptyEntries).Where(p => p.Trim() != "").ToArray();
            for (var x = 0; x < texts.Length; x++)
            {
                foundSentence = false;
                sentence = "";
                txt = texts[x].ToLower().Trim();
                if (txt == "") { continue; }

                //find end of sentence
                if (txt == ".")
                {
                    foundSentence = true;
                    //is period actually used for the end of a sentence 
                    //or is it used for an acronym or abbreviation instead?
                    if (texts.Length > x + 3)
                    {
                        //look ahead for a period
                        txt1 = texts[x + 1].Trim();
                        if (txt1 == "") { txt1 = " "; }
                        if (texts[x + 2].Trim() == ".")
                        {
                            if (txt1.IndexOf(" ") < 0)
                            {
                                //part of an abbreviation or acronym
                                foundSentence = false;
                            }
                            txt2 = texts[x + 3];
                            if (txt2.Length > 3)
                            {
                                //look ahead for website URLs
                                if (domainSuffixes.Contains(txt2.Substring(0, 3)) == true)
                                {
                                    txt3 = txt2.Substring(3, 1);
                                    if (S.Util.Str.CheckChar(txt3, true) == false)
                                    {
                                        //found a website URL, it is not an acronym like I had thought
                                        foundSentence = true;
                                    }
                                }
                            }

                        }
                        else if (S.Util.Str.CheckChar(txt1.Substring(0, 1), true, false, null, true) == true)
                        {
                            //look ahead for a capital letter
                        }
                        //text is a potential sentence part
                        if (x >= 2)
                        {
                            if (texts[x - 1].Trim() == ".")
                            {
                                foundSentence = false;
                            }
                        }
                        if (x < texts.Length - 2)
                        {
                            txt2 = texts[x + 1].Trim();
                            if (txt2.IndexOf("com") == 0)
                            {
                                if (1 == 1)
                                {

                                }
                            }
                            if (txt2.Length > 3)
                            {
                                //look for website URLs
                                if (domainSuffixes.Contains(txt2.Substring(0, 3)) == true)
                                {
                                    txt3 = txt2.Substring(3, 1);
                                    if (S.Util.Str.CheckChar(txt3, true) == false)
                                    {
                                        //found a website URL
                                        foundSentence = false;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (txt == "!" || txt == "?" || txt == ":")
                {
                    foundSentence = true;
                }


                if (foundSentence == true)
                {
                    //look for quotes
                    if (texts.Length - 1 > x)
                    {
                        txt1 = texts[x + 1].Trim();
                        if (txt1 != "")
                        {
                            txt2 = txt1.Substring(0, 1);
                            if (txt2 == "\"" || txt2 == ",")
                            {
                                foundSentence = false;
                            }
                        }

                    }
                }


                if (foundSentence == true || x == texts.Length - 1)
                {
                    for (int z = sentenceStart; z <= x; z++)
                    {
                        sentence += texts[z];
                    }
                    //check sentence length
                    if (sentence.Length > 1)
                    {
                        txt = sentence.Trim();
                        if(txt.Length > 0)
                        {
                            txt1 = txt.Substring(0, 1);
                            txt2 = txt.Substring(txt.Length - 1, 1);
                            if (badChars.Contains(txt1) == true)
                            {
                                if (txt.Length > 1)
                                {
                                    txt = txt.Substring(1);
                                }
                                else
                                {
                                    txt = "";
                                }
                            }
                            if (badChars.Contains(txt2) == true)
                            {
                                if (txt2 != ":")
                                {
                                    txt = txt.Substring(0, txt.Length - 1);
                                }
                            }
                            if(txt != "")
                            {
                                sentences.Add(txt);
                                sentenceStart = x + 1;
                            }
                        }
                    }
                }
            }
            return sentences;
        }

        public string[] GetWords(string text, string[] exceptions = null)
        {
            if (exceptions != null)
            {

                var ws = wordSeparators.Where(w => !exceptions.Contains(w)).ToArray();
                return S.Util.Str.replaceAll(text, " {1} ", ws).Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Split(' ').Where(w => w != "").ToArray();
            }
            return  S.Util.Str.replaceAll(text, " {1} ", wordSeparators).Replace("  ", " ").Replace("  ", " ").Replace("  ", " ").Split(' ').Where(w => w != "").ToArray();
        }

        public bool isSentenceSeparator(string character, string[] exceptions = null)
        {
            if(exceptions != null)
            {
                if(exceptions.Contains(character) == true) { return false; }
            }
            return sentenceSeparators.Contains(character);
        }

        public string GetWordTypeClassNames(AnalyzedArticle article, AnalyzedWord word, List<string> commonWords)
        {
            var wordType = "";
            var i = 0;
            if (S.Util.Str.IsNumeric(word.word) == true)
            {
                var number = int.Parse(word.word);
                var numtype = " number";
                if (word.word.Length == 4)
                {

                    //potential year
                    if (number <= DateTime.Now.Year + 100 && number >= 1)
                    {
                        if (number < 1600)
                        {
                            if(S.Util.IsEmpty(article) == false)
                            {
                                if (article.words[i + 1].word.ToLower() == "ad" || article.words[i + 1].word.ToLower() == "bc")
                                {
                                    numtype = " year";
                                }
                            }
                            
                        }
                        else
                        {
                            numtype = " year";
                        }
                    }

                }
                wordType += numtype;
                i++;
            }

            if (word.importance == 10) { wordType += " important"; }
            if (commonWords.Contains(word.word.ToLower().Trim())) { wordType += " common"; }
            else
            {
                if (word.importance == 0) { wordType += " symbols"; }
            }
            if (word.id > 0) { wordType += " database"; }
            return wordType;
        }
        #endregion

        #region "Articles"

        public bool ArticleExist(string url)
        {
            var result = S.Sql.ExecuteScalar("EXEC ArticleExists @url='" + url + "'");
            if(result is DBNull) { result = 0; }
            return ((int)result == 0) ? false : true;
        }

        public void CleanArticle(int articleId)
        {
            S.Sql.ExecuteNonQuery("EXEC CleanArticle @articleId=" + articleId);
        }

        public int AddArticle(int feedId, string url, string domain, string title, string summary = "", int images = 0, DateTime datePublished = new DateTime(), int subjects = 0, int relavance = 1, int importance = 1, int fiction = 1)
        {
            return (int)S.Sql.ExecuteScalar("EXEC AddArticle @feedId=" + feedId + ", @url='" + url + "', @subjects=" + subjects +
                ", @domain='" + S.Sql.Encode(domain) + "', @title='" + S.Sql.Encode(title) + "', @summary='" + S.Sql.Encode(summary) + "', @images=" + images + 
                ", @datePublished='" + datePublished.ToString() + "', @relavance=" + relavance + ", @importance=" + importance + ", @fiction=" + fiction);
        }

        public void AddArticleWord(int articleId, int wordId, int count)
        {
            S.Sql.ExecuteNonQuery("EXEC AddArticleWord @articleid=" + articleId + ", @wordid=" + wordId + ", @count=" + count);
        }

        public void AddArticleSubjects(int articleId, int[] subjectId, int importance = 1, DateTime datePublished = new DateTime())
        {
            if(subjectId.Length == 0) { return; }
            foreach(int id in subjectId)
            {
                S.Sql.ExecuteNonQuery("AddArticleSubject @articleId=" + articleId + ", @subjectId=" + id + ", @datepublished='" + datePublished.ToString() + "', @importance=" + importance);
            }
        }

        public void UpdateArticle(int articleId, string title, string summary = "", int images = 0, DateTime datePublished = new DateTime(), int subjects = 0, int relavance = 1, int importance = 1, int fiction = 1)
        {
            S.Sql.ExecuteNonQuery("EXEC UpdateArticle @articleId=" + articleId + ", @subjects=" + subjects +
                ", @title='" + S.Sql.Encode(title) + "', @summary='" + S.Sql.Encode(summary) + "', @images=" + images +
                ", @datePublished='" + datePublished.ToString() + "', @relavance=" + relavance + ", @importance=" + importance + ", @fiction=" + fiction);
        }

        public SqlReader GetArticles(int start = 1, int length = 50, int[] subjectIds = null, string search = "", int sort = 0, int isActive = 2, bool isDeleted = false, int minImages = 0, DateTime dateStart = new DateTime(), DateTime dateEnd = new DateTime())
        {
            var d1 = dateStart;
            var d2 = dateEnd;
            if(d1.Year == 1)
            {
                d1 = DateTime.Today.AddYears(-100);
            }
            if (d2.Year == 1)
            {
                d2 = DateTime.Today.AddYears(100);
            }
            
            var reader = new SqlReader();

            reader.ReadFromSqlClient(S.Sql.ExecuteReader("EXEC GetArticles " + 
                "@subjectIds='" + (subjectIds == null ? "" : string.Join(",", subjectIds)) + "', @search='" + search + "', " + 
                "@isActive=" + isActive + ", @isDeleted=" + (isDeleted == true ? 1 : 0) + ", " + 
                "@minImages=" + minImages +", @dateStart=" + reader.ConvertDateTime(d1) + ", @dateEnd=" + reader.ConvertDateTime(d2) + ", " + 
                "@start=" + start + ", @length=" + length + ", @orderby=" + sort));
            return reader;
        }

        public void SaveArticle(AnalyzedArticle article)
        {
            if (ArticleExist(article.url) == false)
            {
                article.id = AddArticle(0, article.url, article.domain, article.pageTitle, article.summary, 0, article.publishDate, article.subjects.Count, 1, 0);
            }
            else
            {
                //remove existing subjects (to re-add)
                CleanArticle(article.id);

                //update article title, summary, & publish date
                UpdateArticle(article.id, article.pageTitle, article.summary, 0, article.publishDate, article.subjects.Count, 1, 0);
            }

            //add words to article
            foreach(AnalyzedWord word in article.words)
            {
                if(word.id > 0)
                {
                    AddArticleWord(article.id, word.id, word.count);
                }
            }

            //add subjects to article
            if (article.subjects.Count > 0)
            {
                var subjects = new List<int>();
                foreach (var subject in article.subjects)
                {
                    subjects.Add(subject.id);
                }
                AddArticleSubjects(article.id, subjects.ToArray(), 1, article.publishDate);
            }

            //finally, save article html & object to file
            SaveArticleToFile(article);
        }

        public void SaveArticleToFile(AnalyzedArticle article)
        {
            if(article.domain.Length > 0)
            {
                var letter = article.domain.Substring(0, 2);
                var path = S.Server.MapPath("/content/articles/" + letter + "/" + article.domain + "/");
                //create folder for domain
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                //save raw html to file
                if (File.Exists(path + article.id + ".html") == false)
                {
                    File.WriteAllText(path + article.id + ".html", article.rawHtml);
                }


                //save (stripped) article object to file
                var newArticle = new AnalyzedArticle();
                var bodyElements = new List<DomElement>();
                foreach (int x in article.body)
                {
                    bodyElements.Add(article.elements[x]);
                }
                newArticle.id = article.id;
                newArticle.pageTitle = article.pageTitle;
                newArticle.title = article.title;
                newArticle.summary = article.summary;
                newArticle.url = article.url;
                newArticle.domain = article.domain;
                newArticle.author = article.author;
                newArticle.publishDate = article.publishDate;
                newArticle.phrases = article.phrases;
                newArticle.words = article.words;
                newArticle.people = article.people;
                newArticle.bodyElements = bodyElements;
                //S.Util.Serializer.SaveToFile(newArticle, path + article.id + ".json");
            }
            
        }

        public AnalyzedArticle OpenArticleFromFile(string file)
        {
            if (File.Exists(S.Server.MapPath(file)))
            {
                return (AnalyzedArticle)S.Util.Serializer.OpenFromFile(typeof(AnalyzedArticle), S.Server.MapPath(file));
            }
            return new AnalyzedArticle();
        }
        #endregion

        #region "Article Words"
        public void AddWords(string wordList, int grammartype, string hierarchy, int score)
        {
            if(hierarchy == "") { return; }
            var hier = hierarchy.ToLower().Replace(" > ", ">").Replace("> ", ">").Replace(" >", ">").Split('>');
            int parentId = 0;
            var parentTitle = "";
            var parentBreadcrumb = "";
            if (hier.Length > 0)
            {
                var parentHier = hier.ToList();
                parentTitle = hier[hier.Length - 1];
                parentHier.RemoveAt(parentHier.Count - 1);
                parentBreadcrumb = string.Join(">", parentHier);
            }
            var reader = new SqlReader();
            reader.ReadFromSqlClient(S.Sql.ExecuteReader("EXEC GetSubject @title='" + parentTitle + "', @breadcrumb='" + parentBreadcrumb + "'"));
            if (reader.Rows.Count > 0)
            {
                reader.Read();
                parentId = reader.GetInt("subjectid");
                parentBreadcrumb = reader.Get("breadcrumb");
            }

            var words = wordList.Replace("'","\'").Replace(" , ", ",").Replace(", ", ",").Replace(" ,", ",").Split(',');
            foreach(string word in words)
            {
                S.Sql.ExecuteNonQuery("EXEC AddWord @word='" + word + "', @subjectId=" + parentId + ", @grammartype=" + grammartype + ", @score=" + score);
            }
            
        }

        public void AddCommonWord(string wordlist)
        {
            var file = S.Server.MapPath("/content/commonwords.json");
            var commonWords = GetCommonWords();
            if(wordlist == ",") { if (!commonWords.Contains(",")) { commonWords.Add(","); } else { commonWords.Remove(","); } }
            var words = wordlist.Split(',');
            foreach (string word in words)
            {
                var w = word.Trim().ToLower();
                if (w == "") { continue; }
                if (!commonWords.Contains(w))
                {
                    commonWords.Add(w);
                }
                else
                {
                    //remove common word instead
                    commonWords.Remove(w);
                }
            }
            commonWords = commonWords.OrderBy(w => w).ToList();
            S.Server.Cache["commonwords"] = commonWords;
            S.Util.Serializer.SaveToFile(commonWords, S.Server.MapPath("/content/commonwords.json"));
        }

        public List<string> GetCommonWords()
        {
            var file = S.Server.MapPath("/content/commonwords.json");
            var commonWords = new List<string>();
            if (S.Server.Cache.ContainsKey("commonwords"))
            {
                commonWords = (List<string>)S.Server.Cache["commonwords"];
            }
            else if (File.Exists(file))
            {
                commonWords = (List<string>)S.Util.Serializer.OpenFromFile(typeof(List<string>), file);
            }
            return commonWords;
        }

        public void AddNormalWord(string wordlist)
        {
            var file = S.Server.MapPath("/content/normalwords.json");
            var normalWords = GetNormalWords();
            if (wordlist == ",") { if (!normalWords.Contains(",")) { normalWords.Add(","); } else { normalWords.Remove(","); } }
            var words = wordlist.Split(',');
            foreach (string word in words)
            {
                var w = word.Trim().ToLower();
                if (w == "") { continue; }
                if (!normalWords.Contains(w))
                {
                    normalWords.Add(w);
                }
                else
                {
                    //remove common word instead
                    normalWords.Remove(w);
                }
            }
            normalWords = normalWords.OrderBy(w => w).ToList();
            S.Server.Cache["normalwords"] = normalWords;
            S.Util.Serializer.SaveToFile(normalWords, file);
        }

        public List<string> GetNormalWords()
        {
            var file = S.Server.MapPath("/content/normalwords.json");
            var normalWords = new List<string>();
            if (S.Server.Cache.ContainsKey("normalwords"))
            {
                normalWords = (List<string>)S.Server.Cache["normalwords"];
            }
            else if (File.Exists(file))
            {
                normalWords = (List<string>)S.Util.Serializer.OpenFromFile(typeof(List<string>), file);
            }
            return normalWords;
        }

        #endregion

        #region "Article Sentences"
        public void AddArticleSentence(int articleId, int index, string sentence)
        {
            S.Sql.ExecuteNonQuery("EXEC AddArticleSentence @articleId=" + articleId + ", @index=" + index + ", @sentence='" + S.Sql.Encode(sentence) + "'");
        }
        #endregion

        #region "Phrases"
        public void AddPhrase(string wordlist)
        {
            if(wordlist == "") { return; }
            if (wordlist.IndexOf(",") <= 0) { return; }
            var phrases = GetPhrases();
            var words = wordlist.ToLower().Split(',').Select(p => p.Trim()).ToArray();
            if(FindPhrase(words).Length == 0)
            {
                //add new phrase to database
                phrases.Add(words);
            }
            S.Server.Cache["phrases"] = phrases;
            S.Util.Serializer.SaveToFile(phrases, S.Server.MapPath("/content/phrases.json"));
        }

        public string[] FindPhrase(string[] words)
        {
            var phrases = GetPhrases();
            var foundPhrases = phrases.Where(p => p[0] == words[0]).ToList();
            if (foundPhrases.Count > 0)
            {
                foreach (var phrase in foundPhrases)
                {
                    var isBad = false;
                    for (var x = 0; x < phrase.Length; x++)
                    {
                        if (x >= words.Length) { isBad = true; break; }
                        if (phrase[x].IndexOf(words[x]) != 0)
                        {
                            isBad = true;
                        }
                    }
                    if (isBad == false)
                    {
                        return phrase;
                    }
                }
            }
            return new string[0];
        }

        public List<string[]> FindPhrases(string word)
        {
            return GetPhrases().Where(p => p[0] == word).ToList();
        }

        public List<string[]> GetPhrases()
        {
            var phrases = new List<string[]>();
            if (S.Server.Cache.ContainsKey("phrases"))
            {
                phrases = (List<string[]>)S.Server.Cache["phrases"];
            }
            else if (File.Exists(S.Server.MapPath("/content/phrases.json")))
            {
                phrases = (List<string[]>)S.Util.Serializer.OpenFromFile(typeof(List<string[]>), S.Server.MapPath("/content/phrases.json"));
            }
            return phrases;
        }
        #endregion
    }
}