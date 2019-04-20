using Eventually.Core.Publisher.Models;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Eventually.Core.Publisher
{
    public class PublisherService : IPublisherService, IDisposable
    {
        private IDisposable _RepublishSubscription;

        private IFieldService _FieldService { get; }
        private Func<FirstClassField, Task> _PublishFunc;
        private Func<long, Annotation, Task> _AckFunc;

        public PublisherService(IFieldService fieldService)
        {
            _FieldService = fieldService ?? throw new ArgumentNullException(nameof(fieldService));
        }

        public void StartPublishing(
            Func<FirstClassField, Task> publishFunc,
            Func<long, Annotation, Task> ackFunc,
            TimeSpan republishInterval)
        {
            _PublishFunc = publishFunc ?? throw new ArgumentNullException(nameof(publishFunc));
            _AckFunc     = ackFunc     ?? throw new ArgumentNullException(nameof(ackFunc));

            _RepublishSubscription = RepublishFields(republishInterval);
        }

        public async Task AnnotateField(long fieldId, Annotation annotation)
        {
            if (await _FieldService.AnnotateField(fieldId, annotation))
            {
                //Fire and forget ack
                _AckFunc(fieldId, annotation);
            }
        }

        private IDisposable RepublishFields(TimeSpan interval)
        {
            var subscription = Observable
                .Interval(interval)
                .Subscribe(async _ =>
                {
                    var unAnnotatedFields = await _FieldService.GetUnannotatedFields();

                    foreach(var field in unAnnotatedFields)
                    {
                        //Fire and forget async publish
                        _PublishFunc(field);
                    }
                });

            return subscription;
        }

        public void Dispose()
        {
            _RepublishSubscription?.Dispose();
        }
    }
}
