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
        private readonly long _FailOnEvery;

        private readonly IDisposable _FieldGenerationSubscription;

        private long _InterlockRef;

        public IReadOnlyCollection<FirstClassField> Fields => 
            new ReadOnlyCollection<FirstClassField>(_Fields.ToList());

        //Responsible for generating and holding field data
        public FieldService(
            TimeSpan generationInterval,
            long maxFieldCount,
            long failOnEvery)
        {
            _Fields = new ConcurrentBag<FirstClassField>
            {
                new FirstClassField(-1L)
            };

            _MaxFieldCount = maxFieldCount;
            _FailOnEvery = failOnEvery;

            _FieldGenerationSubscription = GenerateFields(generationInterval);
        }

        //Annotate a FirstClassField with a simulated rate of failure
        public async Task<bool> AnnotateField(long fieldId, Annotation annotation)
        {
            //This is just a cheap way to simulate a rate of failure.
            //Interlocked.Increment is a thread-safe, atomic increment
            var willSucceed = (Interlocked.Increment(ref _InterlockRef) % _FailOnEvery) != 0;

            if (willSucceed)
            {
                var field = _Fields.FirstOrDefault(x => x.Id == fieldId);

                if (field != null)
                {
                    field.Annotate(annotation);
                }
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
            //Continuosly execute the specified anonymous method on the specified interval, without blocking.
            //Stop executing once the number of iterations has reached the specified maximum count
            var subscription = Observable
                .Interval(interval)
                .TakeUntil(_  => _Fields.Count == _MaxFieldCount)
                .Subscribe(id => _Fields.Add(new FirstClassField(id)));

            return subscription;
        }

        public void Dispose()
        {
            _FieldGenerationSubscription.Dispose();
        }
    }
}
