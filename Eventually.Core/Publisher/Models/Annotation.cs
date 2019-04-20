using System;

namespace Eventually.Core.Publisher.Models
{
    public class Annotation
    {
        public long Id     { get; }
        public string Data { get; }

        public Annotation(long id, string data)
        {
            Id   = id;
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }
    }
}
