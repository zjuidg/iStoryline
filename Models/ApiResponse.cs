namespace StorylineBackend.models
{
    public class ApiResponse<T>
    {
        public const int OK = 0;
        public const int ERR = -1;
        
        public int error = OK;
        public T data;
        
        public ApiResponse (int error = ERR)
        {
            this.error = error;
        }

        public ApiResponse(int error, T data)
        {
            this.error = error;
            this.data = data;
        }
    }

    public class StringResponse : ApiResponse<string>
    {
        public StringResponse(int err, string data)
        {
            this.error = err;
            this.data = data;
        }
    }
}