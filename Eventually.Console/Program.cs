using Eventually.Core.Consumer;
using Eventually.Core.Publisher;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Eventually.ConsoleRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var maxFields = 10000L;
            var fieldGenerationInterval = TimeSpan.Zero;
            var annotationRepublishInterval = TimeSpan.FromSeconds(1);
            var fieldRepublishInterval = TimeSpan.FromSeconds(1);

            var failOnAnnotateSave = 2L;
            var failOnAnnotateAck = 2L;
            var failOnAnnotate = 2L;

            var fieldService = new FieldService(fieldGenerationInterval, maxFields, failOnAnnotateSave);
            using (var publisher = new PublisherService(fieldService))
            using (var consumer = new AnnotationService(
                publisher.AnnotateField,
                annotationRepublishInterval,
                failOnAnnotateAck,
                failOnAnnotate))
            {
                publisher.StartPublishing(consumer.Annotate, consumer.Acknowledge, fieldRepublishInterval);

                Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(observationCount =>
                {
                    var fields = fieldService.Fields;
                    var annotatedFields = fields.Where(field => field.ActiveAnnotation != null);
                    var annotatedFieldCount = annotatedFields.Count();
                    var annotations = consumer.Annotations;

                    Console.WriteLine($"Observation:             {observationCount}");
                    Console.WriteLine($"Field Count:             {fields.Count}");
                    Console.WriteLine($"Annotated Field Count:   {annotatedFieldCount}");
                    Console.WriteLine($"Annotations Count:       {annotations.Count}");
                    Console.WriteLine($"Unannotated Field Count: {fields.Count - annotatedFieldCount}");
                    Console.WriteLine();
                });

                Console.ReadKey();
            }
        }
    }
}
