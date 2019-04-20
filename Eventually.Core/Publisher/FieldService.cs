using Eventually.Core.Publisher.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eventually.Core.Publisher
{
    public class FieldService : IFieldService, IDisposable
    {
        private readonly ConcurrentBag<FirstClassField> _Fields;

        private readonly long _MaxFieldCount;

        private readonly IDisposable _FieldGenerationSubscription;

        //Simple ~50% error rate simulation
        private long _InterlockRef;

        public IReadOnlyCollection<FirstClassField> Fields => 
            new ReadOnlyCollection<FirstClassField>(_Fields.ToList());

        //Responsible for generating and holding field data
        public FieldService(TimeSpan generationInterval, long maxFieldCount)
        {
            _Fields = new ConcurrentBag<FirstClassField>
            {
                new FirstClassField(-1L)
            };

            _MaxFieldCount = maxFieldCount;

            _FieldGenerationSubscription = GenerateFields(generationInterval);
        }

        //Annotate a FirstClassField with a simulated rate of failure
        public async Task<bool> AnnotateField(long fieldId, Annotation annotation)
        {
            //This is just a cheap way to simulate a rate of failure
            var willSucceed = (Interlocked.Increment(ref _InterlockRef) % 4L) != 0;

            if (willSucceed)
            {
                var field = _Fields.FirstOrDefault(x => x.Id == fieldId);

                if (field != null)
                {
                    field.Annotate(annotation);
                }
            }
            else
            {

            }

            return willSucceed;
        }

        //Retrieve all FirstClassFields that are not currently annotated
        public async Task<IReadOnlyCollection<FirstClassField>> GetUnannotatedFields()
        {
            var fields = _Fields.Where(field => field.ActiveAnnotation is null).ToList();

            return new ReadOnlyCollection<FirstClassField>(fields);
        }

        //Continually generate new fields to simulate data entry
        private IDisposable GenerateFields(TimeSpan interval)
        {
            var subscription = Observable
                .Interval(interval)
                .Subscribe(id =>
                {
                    if(_Fields.Count < _MaxFieldCount)
                    {
                        _Fields.Add(new FirstClassField(id));
                    }
                });

            return subscription;
        }

        public void Dispose()
        {
            _FieldGenerationSubscription.Dispose();
        }
    }
}
