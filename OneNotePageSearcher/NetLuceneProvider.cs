﻿using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;


namespace OneNotePageSearcher
{
    internal class NetLuceneProvider
    {
        private readonly string _indexPath = "LuceneIndex";

        private readonly string _indexByDocumentPath = "LuceneIndex";

        private int _maxDoc;
        private readonly Analyzer _analyzer;

        private IndexWriter writer;

        private int _searchLimit = 1000;

        private Directory indexDirectory= null;

        public bool debug;

        public NetLuceneProvider(bool overwrite)
        {
            _analyzer = new StandardAnalyzer(Version.LUCENE_30);
            SetWorkingDirectory();
        }

        public void SetWorkingDirectory()
        {
            indexDirectory = FSDirectory.Open(_indexPath);
        }

        public void SetUpReader()
        {
            var indexReader = IndexReader.Open(indexDirectory, true);
            _maxDoc = indexReader.MaxDoc;
            if (debug) Console.WriteLine(_maxDoc);
            indexReader.Dispose();
            indexDirectory.Dispose();
        }

        public void SetUpWriter(bool overwrite = true)
        {
            writer = new IndexWriter(indexDirectory, _analyzer, overwrite, IndexWriter.MaxFieldLength.UNLIMITED);
        }

        public void CloseWriter()
        {
            writer.Optimize();
            //Close the writer
            writer.Flush(true, true, true);
            writer.Dispose();
            indexDirectory.Dispose();
        }

        public void AddDocumentList(List<Tuple<string, string>> documentList)
        {
            foreach (var docPair in documentList)
            {
                AddTextToIndex(docPair.Item1, docPair.Item2);
            }
        }

        public void AddDocument(Tuple<string,string> doc)
        {
            AddTextToIndex(doc.Item1, doc.Item2);
        }

        public void DeleteDocumentByID(string id)
        {
            writer.DeleteDocuments(new Term("id", id));
        }

        public HashSet<String> GetAllValuesByField(string field_name)
        {
            IndexReader reader = IndexReader.Open(indexDirectory,true);
            TermEnum terms = reader.Terms();
            HashSet<String> uniqueTerms = new HashSet<String>();
            while (terms.Next())
            {
                var term = terms.Term;
                if (term.Field.Equals(field_name))
                {
                    uniqueTerms.Add(term.Text);
                }
            }
            return uniqueTerms;
        }

        private void Main(string[] args)
        {
            var lucene = new NetLuceneProvider(true);
            lucene.SetWorkingDirectory();
            if (debug) Console.WriteLine(String.Join(",", lucene.GetAllValuesByField("id")));
            if (debug) Console.Read();
        }

        public List<Tuple<string, string, float>> Search(string q)
        {
            //Setup searcher
            //Directory directory = FSDirectory.Open(_indexPath);

            SetWorkingDirectory();

            var searcher = new IndexSearcher(indexDirectory);
            var parser = new QueryParser(Version.LUCENE_30, "postBody", _analyzer);

            var query = parser.Parse(q);
            var hits = searcher.Search(query, _searchLimit);

            if (debug) Console.WriteLine(hits.TotalHits);

            var results = hits.TotalHits;

            var resultList = new List<Tuple<string, string, float>>();

            if (debug) Console.WriteLine("Found {0} results", results);
            for (var i = 0; i < results && i< _searchLimit; i++)
            {
                var doc = searcher.Doc(hits.ScoreDocs[i].Doc);
                var score = hits.ScoreDocs[i].Score;

                resultList.Add(new Tuple<string, string, float>(
                    doc.Get("id"), doc.Get("postBody"), score
                    ));
            }


            //Clean up everything
            searcher.Dispose();
            indexDirectory.Dispose();
            return resultList;
        }

        private void AddTextToIndex(string id, string text)
        {
            var doc = new Document();
            doc.Add(new Field("id", id, Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("postBody", text, Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);
        }
    }
} 

 
