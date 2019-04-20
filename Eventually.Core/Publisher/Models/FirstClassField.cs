using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Eventually.Core.Publisher.Models
{
    public class FirstClassField
    {
        private IList<Annotation> _Annotations;

        public long Id { get; }

        public Annotation ActiveAnnotation { get; private set; }

        public IReadOnlyCollection<Annotation> Annotations =>
            new ReadOnlyCollection<Annotation>(_Annotations);

        public FirstClassField(long id)
        {
            Id = id;

            _Annotations = new List<Annotation>();
        }

        internal void Annotate(Annotation annotation)
        {
            ActiveAnnotation = annotation ?? throw new ArgumentNullException(nameof(annotation));

            _Annotations.Add(annotation);
        }

        internal void RemoveAnnotation()
        {
            ActiveAnnotation = null;
        }
    }
}
