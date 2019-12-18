using System;

namespace StorylineBackend.models
{
    public class Request<T>
    {
        public string action;
        public T payload;
    }
}