using Eventually.Core.Consumer;
using Eventually.Core.Publisher;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Eventually.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var maxFields = 1000L;
            var fieldGenerationInterval = TimeSpan.FromMilliseconds(1);
            var annotationRepublishInterval = TimeSpan.FromSeconds(2);
            var fieldRepublishInterval = TimeSpan.FromSeconds(2);

            var fieldService = new FieldService(fieldGenerationInterval, maxFields);
            using (var publisher = new PublisherService(fieldService))
            using (var consumer = new AnnotationService(publisher.AnnotateField, annotationRepublishInterval))
            {
                publisher.StartPublishing(consumer.Annotate, consumer.Acknowledge, fieldRepublishInterval);

                Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(observationCount =>
                {
                    var fields = fieldService.Fields;
                    var annotatedFields = fields.Where(field => field.ActiveAnnotation != null);
                    var annotatedFieldCount = annotatedFields.Count();
                    var annotations = consumer.Annotations;

                    System.Console.WriteLine($"Observation: {observationCount}");
                    System.Console.WriteLine($"Field Count: {fields.Count}");
                    System.Console.WriteLine($"Annotated Field Count: {annotatedFieldCount}");
                    System.Console.WriteLine($"Unannotated Field Count: {fields.Count - annotatedFieldCount}");
                    System.Console.WriteLine($"Annotations Count: {annotations.Count}");
                    System.Console.WriteLine();
                });

                System.Console.ReadKey();
            }
        }
    }
}
