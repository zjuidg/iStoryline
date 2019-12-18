using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Nancy;
using Nancy.ModelBinding;

namespace StorylineBackend.upload
{
    public class UploadRequest
    {
        public HttpFile File { get; set; }
    }

    public class UploadRequestBinder : IModelBinder
    {
        public object Bind(NancyContext context, Type modelType, object instance, BindingConfig configuration,
            params string[] blackList)
        {
            var fileUploadRequest = instance as UploadRequest ?? new UploadRequest();

            fileUploadRequest.File = GetFileByKey(context, "file");
            return fileUploadRequest;
        }
        
        private static HttpFile GetFileByKey(NancyContext context, string key)
        {
            IEnumerable<HttpFile> files = context.Request.Files;
            return files?.FirstOrDefault(x => x.Key == key);
        }

        public bool CanBind(Type modelType)
        {
            return modelType == typeof(UploadRequest);
        }
    }
    
    public class Pair<T, U> {
        public Pair() {
        }

        public Pair(T item1, U item2) {
            this.Item1 = item1;
            this.Item2 = item2;
        }

        public T Item1 { get; set; }
        public U Item2 { get; set; }

        public Tuple<T, U> toTuple()
        {
            return new Tuple<T, U>(Item1, Item2);
        }
    }

    public class Triple<T, U, R>
    {
        public Triple()
        {
        }

        public Triple(T item1, U item2, R item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }

        public T Item1;
        public U Item2;
        public R Item3;
    }

    public class CharacterYConstraint
    {
        public int characterId;
        public int frame;
        public int upperY;
        public int lowerY;

        public CharacterYConstraint()
        {
        }

        public CharacterYConstraint(int characterId, int frame, int upperY, int lowerY)
        {
            this.characterId = characterId;
            this.frame = frame;
            this.upperY = upperY;
            this.lowerY = lowerY;
        }
    }

    public class SessionBreak
    {
        public int session1;
        public int session2;
        public int frame;
    }

    public class UpdateRequest
    {
        public string id;
        // [{s, gap}]
        public List<Pair<int, double>> sessionInnerGaps;
        // [{{s1, s2}, {min, max}}]
        public List<Pair<Pair<int, int>, Pair<int, int>>> sessionOuterGaps;
        public List<CharacterYConstraint> characterYConstraints;
        public List<List<int>> orders;
        public List<Tuple<int, List<int>>> orderTable;
        public List<int> groupIds;
        public List<int> selectedSessions;
        public List<Pair<int, List<int>>> majorCharacters;
        public List<SessionBreak> sessionBreaks;
        public double sessionInnerGap;
        public double sessionOuterGap;
    }
}