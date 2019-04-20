using Eventually.Core.Publisher.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eventually.Core.Publisher
{
    public class FieldService : IFieldService, IDisposable
    {
        private readonly ConcurrentBag<FirstClassField> _Fields;
        private readonly long _MaxFieldCount;

        private IDisposable _FieldGenerationSubscription;

        //Simple ~50% error rate simulation
        private volatile bool _AnnotateSucceed;

        public IReadOnlyCollection<FirstClassField> Fields => new ReadOnlyCollection<FirstClassField>(_Fields.ToList());

        public FieldService(TimeSpan generationInterval, long maxFieldCount)
        {
            _Fields = new ConcurrentBag<FirstClassField>
            {
                new FirstClassField(-1L)
            };

            _MaxFieldCount = maxFieldCount;

            _FieldGenerationSubscription = GenerateFields(generationInterval);
        }

        public async Task<bool> AnnotateField(long fieldId, Annotation annotation)
        {
            var willSucceed = _AnnotateSucceed;

            if (willSucceed)
            {
                _AnnotateSucceed = false;

                var field = _Fields.FirstOrDefault(x => x.Id == fieldId);

                if (field != null)
                {
                    field.Annotate(annotation);
                }
            }
            else
            {
                _AnnotateSucceed = true;
            }

            return willSucceed;
        }

        public async Task<IReadOnlyCollection<FirstClassField>> GetUnannotatedFields()
        {
            var fields = _Fields.Where(field => field.ActiveAnnotation is null).ToList();

            return new ReadOnlyCollection<FirstClassField>(fields);
        }

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
