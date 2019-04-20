using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eventually.Core.Consumer.Models;
using Eventually.Core.Publisher.Models;

namespace Eventually.Core.Consumer
{
    public class AnnotationService : IAnnotationService, IDisposable
    {
        private long _InterlockRef;
        private long _IdRef;

        private ConcurrentDictionary<long, ConcurrentBag<AckableAnnotation>> _Annotations { get; }

        private IDisposable _RepublishSubscription { get; }

        private Func<long, Annotation, Task> _PublishAnnotationFunc { get; }

        //TODO: Find a way to make the ConcurrentBag here readonly
        public IReadOnlyDictionary<long, ConcurrentBag<AckableAnnotation>> Annotations =>
            new ReadOnlyDictionary<long, ConcurrentBag<AckableAnnotation>>(_Annotations);

        public AnnotationService(
            Func<long, Annotation, Task> annotateFunc,
            TimeSpan republishInterval)
        {
            _PublishAnnotationFunc = annotateFunc ?? throw new ArgumentNullException(nameof(annotateFunc));

            _Annotations = new ConcurrentDictionary<long, ConcurrentBag<AckableAnnotation>>();

            _RepublishSubscription = RepublishAnnotations(republishInterval);
        }

        //Store an acknowledgement for an annotation if we can find it
        public async Task Acknowledge(long fieldId, Annotation annotation)
        {
            if (_Annotations.TryGetValue(fieldId, out var ackables))
            {
                var annotationToAck = ackables.FirstOrDefault(ackable =>
                    ackable.Annotation.Id == annotation.Id);

                if (annotationToAck != null)
                {
                    annotationToAck.Acked = true;
                }
            }
        }

        //Generate and store an annotation for a field
        public async Task Annotate(FirstClassField field)
        {
            //This is just a cheap way to simulate a rate of failure
            var willSucceed = (Interlocked.Increment(ref _InterlockRef) % 2L) == 0;

            if (willSucceed)
            {
                ConcurrentBag<AckableAnnotation> ackables;

                if (_Annotations.TryGetValue(field.Id, out var fieldAckables))
                {
                    ackables = fieldAckables;
                }
                else
                {
                    ackables = new ConcurrentBag<AckableAnnotation>();
                    _Annotations.TryAdd(field.Id, ackables);
                }

                var annotation = new Annotation(Interlocked.Increment(ref _IdRef), "ANNOTATION!");

                ackables.Add(new AckableAnnotation(annotation));

                //Fire and forget publish
                _PublishAnnotationFunc(field.Id, annotation);
            }
        }

        //Continually republish any annotation that is not yet acked
        private IDisposable RepublishAnnotations(TimeSpan interval)
        {
            var subscription = Observable
                .Interval(interval)
                .Subscribe(async _ =>
                {
                    foreach(var field in _Annotations)
                    {
                        var unAcked = field.Value.Where(ackable => !ackable.Acked);

                        foreach(var ackable in unAcked)
                        {
                            //Fire and forget publish
                            _PublishAnnotationFunc(field.Key, ackable.Annotation);
                        }
                    }
                });

            return subscription;
        }

        public void Dispose()
        {
            _RepublishSubscription.Dispose();
        }
    }
}
