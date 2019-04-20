using Eventually.Core.Publisher.Models;
using System.Threading.Tasks;

namespace Eventually.Core.Publisher
{
    interface IPublisherService
    {
        Task AnnotateField(long fieldId, Annotation annotation);
    }
}
