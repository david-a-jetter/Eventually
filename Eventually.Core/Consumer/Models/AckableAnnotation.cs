using Eventually.Core.Publisher.Models;
using System;

namespace Eventually.Core.Consumer.Models
{
    public class AckableAnnotation
    {
        public bool Acked { get; set; }

        public Annotation Annotation { get; }

        public AckableAnnotation(Annotation annotation)
        {
            Annotation = annotation ?? throw new ArgumentNullException(nameof(annotation));
        }
    }
}
