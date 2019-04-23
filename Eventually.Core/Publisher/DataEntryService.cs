using Eventually.Core.Publisher.Models;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Eventually.Core.Publisher
{
    public class DataEntryService : IDataEntryService, IDisposable
    {
        private IDisposable _RepublishSubscription;

        private readonly IFieldService _FieldService;

        private Func<FirstClassField, Task> _PublishFunc;
        private Func<long, Annotation, Task> _AckFunc;

        public DataEntryService(IFieldService fieldService)
        {
            _FieldService = fieldService ?? throw new ArgumentNullException(nameof(fieldService));
        }

        //Initiate publication of unannotated fields to what we hope is an annotation service
        public void StartPublishing(
            Func<FirstClassField, Task> publishFunc,
            Func<long, Annotation, Task> ackFunc,
            TimeSpan republishInterval)
        {
            _PublishFunc = publishFunc ?? throw new ArgumentNullException(nameof(publishFunc));
            _AckFunc     = ackFunc     ?? throw new ArgumentNullException(nameof(ackFunc));

            _RepublishSubscription = RepublishFields(republishInterval);
        }

        //Acknowledge successful annotations
        public async Task AnnotateField(long fieldId, Annotation annotation)
        {
            if (await _FieldService.AnnotateField(fieldId, annotation))
            {
                //Fire and forget ack
                _AckFunc?.Invoke(fieldId, annotation);
            }
        }

        private IDisposable RepublishFields(TimeSpan interval)
        {
            //Continuosly execute the specified anonymous method on the specified interval, without blocking.
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
